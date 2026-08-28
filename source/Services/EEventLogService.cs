using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>
/// 事件日志查询（方案 A：只读）。
/// 数据范围：具备列表权限的登录用户<strong>全员可查</strong>（无 UserID 行级过滤）；请依赖筛选/搜索控制数据量。
/// BusinessID：整型业务主键，本模块不解析业务类型与跳转 URL（由事件引擎与扩展配置负责）。
/// </summary>
public class EEventLogService
{
    private readonly FrameworkDbContext _db;
    private readonly DictService _dict;

    /// <summary>日志 ActionType：字典 EVENT_ACTION + 流转动作码回退。</summary>
    public static IReadOnlyDictionary<string, string> EventActionFallback { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["RAISE"] = "发布事件",
            ["RESOLVE"] = "解析接收",
            ["DELIVER"] = "投递",
            ["HANDLE"] = "处理",
            ["CREATE_TODO"] = "生成待办",
            ["SEND_NOTICE"] = "发送通知",
            ["TRIGGER_EVENT"] = "触发下一事件",
            ["CALL_API"] = "调用接口"
        };

    public EEventLogService(FrameworkDbContext db, DictService dict)
    {
        _db = db;
        _dict = dict;
    }

    public Task<Dictionary<string, string>> GetEventActionMapAsync(CancellationToken ct) =>
        _dict.GetItemMapMergedAsync(DictCodes.EventAction, EventActionFallback, ct: ct);

    public static string ToActionTypeText(string? code, IReadOnlyDictionary<string, string>? map = null)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();
        if (c.Length == 0) return "-";
        if (map != null && map.TryGetValue(c, out var name))
            return name;
        return EventActionFallback.TryGetValue(c, out var fb) ? fb : c;
    }

    public static IReadOnlyDictionary<string, string> HandleResultDisplay { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["SUCCESS"] = "成功",
        ["FAIL"] = "失败",
        ["PARTIAL"] = "部分成功",
        ["SKIP"] = "跳过",
        ["PENDING"] = "待定",
        ["TIMEOUT"] = "超时"
    };

    public static string ToHandleResultText(string? code)
    {
        var c = (code ?? "").Trim();
        return HandleResultDisplay.TryGetValue(c, out var t) ? t : (c.Length == 0 ? "-" : c);
    }

    public static List<(string Value, string Text)> BuildEventCodeFilterOptions(IReadOnlyList<(string Code, string Display)> events)
    {
        var list = new List<(string, string)> { ("", "全部") };
        list.AddRange(events.Select(x => (x.Code, x.Display)));
        return list;
    }

    public static List<(string Value, string Text)> BuildHandleResultFilterOptions() =>
    [
        ("", "全部"),
        ("SUCCESS", "成功"),
        ("FAIL", "失败"),
        ("PARTIAL", "部分成功"),
        ("SKIP", "跳过"),
        ("PENDING", "待定"),
        ("TIMEOUT", "超时")
    ];

    public static List<(string Value, string Text)> BuildSearchFieldOptions() =>
    [
        ("EventCode", "事件编码"),
        ("BusinessID", "业务主键(精确)"),
        ("User", "操作人账号/姓名"),
        ("Remark", "备注"),
        ("HandleResult", "处理结果")
    ];

    public async Task<List<(string Code, string Display)>> LoadActiveEventOptionsAsync(CancellationToken ct)
    {
        var rows = await _db.EEventConfigs.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.AppCode).ThenBy(x => x.EventCode)
            .ToListAsync(ct);
        return rows
            .Select(x => (x.EventCode, $"{x.AppCode}.{x.EventCode} - {(x.EventName ?? "")}".Trim()))
            .ToList();
    }

    public async Task<(IReadOnlyList<EEventLogListRowVm> pageRows, int totalRecords, int totalPages, int page)> GetIndexPageAsync(
        string? eventCode1,
        string? handleResult1,
        string? createTimeFrom,
        string? createTimeTo,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EEventLogs.AsNoTracking().ToListAsync(ct);

        var fromDt = ParseDateStart(createTimeFrom);
        var toDt = ParseDateEndExclusive(createTimeTo);
        if (fromDt.HasValue)
            rows = rows.Where(x => x.CreateTime >= fromDt.Value).ToList();
        if (toDt.HasValue)
            rows = rows.Where(x => x.CreateTime < toDt.Value).ToList();

        var ec = (eventCode1 ?? "").Trim();
        if (ec.Length > 0)
            rows = rows.Where(x => string.Equals(x.EventCode, ec, StringComparison.OrdinalIgnoreCase)).ToList();

        var hr = (handleResult1 ?? "").Trim();
        if (hr.Length > 0)
            rows = rows.Where(x => string.Equals((x.HandleResult ?? "").Trim(), hr, StringComparison.OrdinalIgnoreCase)).ToList();

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            var userIdsFromSearch = await ResolveUserIdsByKeywordAsync(kw, ct);
            rows = (searchField ?? "EventCode") switch
            {
                "EventCode" => rows.Where(x => (x.EventCode ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "ObjectKey" or "BusinessID" => rows.Where(x =>
                    (x.ObjectKey ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "ObjectType" => rows.Where(x =>
                    (x.ObjectType ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "User" => rows.Where(x => x.UserId.HasValue && userIdsFromSearch.Contains(x.UserId.Value)).ToList(),
                "Remark" => rows.Where(x => (x.Remark ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "HandleResult" => rows.Where(x => (x.HandleResult ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow);

        var userIds = rows.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct().ToList();
        var users = await _db.EUsers.AsNoTracking().Where(x => userIds.Contains(x.DataId)).ToListAsync(ct);
        var userMap = users.ToDictionary(x => x.DataId, x => $"{x.LoginId} - {x.RealName}".Trim());

        var eventCodes = rows.Select(x => x.EventCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var evs = await _db.EEventConfigs.AsNoTracking().ToListAsync(ct);
        var eventNameMap = evs.GroupBy(x => (x.EventCode ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().EventName ?? "", StringComparer.OrdinalIgnoreCase);
        var actionMap = await GetEventActionMapAsync(ct);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x =>
        {
            var en = eventNameMap.TryGetValue((x.EventCode ?? "").Trim(), out var n) ? (n ?? "") : "";
            var evDisp = (string.IsNullOrWhiteSpace(en) ? x.EventCode : $"{x.EventCode} - {en}".Trim()) ?? "";
            var remark = x.Remark ?? "";
            const int rmax = 80;
            var remarkShort = remark.Length <= rmax ? remark : remark[..rmax] + "…";
            return new EEventLogListRowVm
            {
                DataId = x.DataId,
                CreateTime = x.CreateTime,
                EventDisplay = evDisp,
                ActionTypeText = ToActionTypeText(x.ActionType, actionMap),
                HandleResultText = ToHandleResultText(x.HandleResult),
                UserDisplay = x.UserId.HasValue && userMap.TryGetValue(x.UserId.Value, out var ud)
                    ? (ud ?? $"#{x.UserId}")
                    : (x.UserId.HasValue ? $"#{x.UserId}" : "-"),
                ObjectType = x.ObjectType ?? "",
                ObjectKey = x.ObjectKey ?? "",
                ObjectRefDisplay = EventObjectRefHelper.FormatDisplay(x.ObjectType, x.ObjectKey),
                RemarkShort = string.IsNullOrWhiteSpace(remarkShort) ? "-" : remarkShort
            };
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<EEventLogDetailVm?> GetDetailAsync(long id, CancellationToken ct)
    {
        var x = await _db.EEventLogs.AsNoTracking().FirstOrDefaultAsync(e => e.DataId == id, ct);
        if (x == null) return null;
        var actionMap = await GetEventActionMapAsync(ct);
        EUser? u = null;
        if (x.UserId.HasValue)
            u = await _db.EUsers.AsNoTracking().FirstOrDefaultAsync(e => e.DataId == x.UserId.Value, ct);
        var ev = await _db.EEventConfigs.AsNoTracking()
            .FirstOrDefaultAsync(e => e.AppCode == x.AppCode && e.EventCode == x.EventCode, ct);
        return new EEventLogDetailVm
        {
            DataId = x.DataId,
            CreateTime = x.CreateTime,
            EventCode = x.EventCode,
            EventName = ev?.EventName ?? "",
            AppCode = x.AppCode ?? "",
            ObjectType = x.ObjectType ?? "",
            ObjectKey = x.ObjectKey ?? "",
            ObjectRefDisplay = EventObjectRefHelper.FormatDisplay(x.ObjectType, x.ObjectKey),
            UserId = x.UserId ?? 0,
            UserDisplay = u == null ? (x.UserId.HasValue ? $"#{x.UserId}" : "-") : $"{u.LoginId} - {u.RealName}".Trim(),
            ActionType = x.ActionType,
            ActionTypeText = ToActionTypeText(x.ActionType, actionMap),
            HandleResult = x.HandleResult,
            HandleResultText = ToHandleResultText(x.HandleResult),
            Remark = x.Remark
        };
    }

    private async Task<HashSet<int>> ResolveUserIdsByKeywordAsync(string kw, CancellationToken ct)
    {
        var users = await _db.EUsers.AsNoTracking()
            .Where(x => x.LoginId.Contains(kw) || x.RealName.Contains(kw))
            .Select(x => x.DataId)
            .ToListAsync(ct);
        return users.ToHashSet();
    }

    private static DateTime? ParseDateStart(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s.Trim(), out var d) ? d.Date : null;
    }

    private static DateTime? ParseDateEndExclusive(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (!DateTime.TryParse(s.Trim(), out var d)) return null;
        return d.Date.AddDays(1);
    }

    private static List<EEventLog> SortRows(List<EEventLog> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        IEnumerable<EEventLog> ordered = field switch
        {
            "CreateTime" => desc ? rows.OrderByDescending(x => x.CreateTime) : rows.OrderBy(x => x.CreateTime),
            "EventCode" => desc ? rows.OrderByDescending(x => x.EventCode) : rows.OrderBy(x => x.EventCode),
            "HandleResult" => desc ? rows.OrderByDescending(x => x.HandleResult) : rows.OrderBy(x => x.HandleResult),
            "DataID" => desc ? rows.OrderByDescending(x => x.DataId) : rows.OrderBy(x => x.DataId),
            _ => rows.OrderByDescending(x => x.CreateTime).ThenByDescending(x => x.DataId)
        };
        return ordered.ToList();
    }
}
