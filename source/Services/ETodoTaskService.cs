using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>
/// 待办任务：列表 + 处理。
/// 数据范围：<strong>仅本人</strong>——仅展示 UserID 等于当前登录账号在 Tbl_E_Users 中对应 DataID 的记录（按 LoginId = MemberID 解析）。
/// 状态：0 待处理、1 已处理、2 已关闭。合法流转：0→1、0→2、1→2；2 为终态；重复提交同状态幂等（不改 HandleTime）。
/// 处理时：进入 1 或 2 且原 HandleTime 为空则写入当前时间；HandlerDeptID/HandlerPosID 可选更新（仅处理请求时传入）。
/// </summary>
public class ETodoTaskService
{
    private readonly FrameworkDbContext _db;
    private readonly DictService _dict;

    public static IReadOnlyDictionary<string, string> StatusFallback { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["0"] = "待处理",
            ["1"] = "已处理",
            ["2"] = "已关闭",
            ["3"] = "已转交",
            ["4"] = "已撤回"
        };

    public static IReadOnlyDictionary<string, string> PriorityFallback { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["LOW"] = "低",
            ["NORMAL"] = "普通",
            ["HIGH"] = "高",
            ["URGENT"] = "紧急"
        };

    public ETodoTaskService(FrameworkDbContext db, DictService dict)
    {
        _db = db;
        _dict = dict;
    }

    public static string StatusText(byte s) =>
        StatusFallback.TryGetValue(s.ToString(), out var t) ? t : $"未知({s})";

    public static string PriorityText(string? code, IReadOnlyDictionary<string, string>? map = null)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();
        if (c.Length == 0) c = "NORMAL";
        if (map != null && map.TryGetValue(c, out var name))
            return name;
        return PriorityFallback.TryGetValue(c, out var fb) ? fb : c;
    }

    public Task<List<(string Value, string Text)>> GetStatusFilterOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.TodoStatus, forFilter: true, formEmptyLabel: null,
            StatusFallback.Select(x => (x.Key, x.Value)).ToList(), ct: ct);

    public async Task<List<(string Value, string Text)>> GetEventCodeFilterOptionsForUserAsync(int currentUserId, CancellationToken ct)
    {
        var codes = await _db.ETodoTasks.AsNoTracking()
            .Where(x => x.UserId == currentUserId && x.EventCode != null && x.EventCode != "")
            .Select(x => x.EventCode!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);
        var list = new List<(string, string)> { ("", "全部事件") };
        list.AddRange(codes.Select(c => (c, c)));
        return list;
    }

    public Task<Dictionary<string, string>> GetStatusMapAsync(CancellationToken ct) =>
        _dict.GetItemMapMergedAsync(DictCodes.TodoStatus, StatusFallback, ct: ct);

    public Task<Dictionary<string, string>> GetPriorityMapAsync(CancellationToken ct) =>
        _dict.GetItemMapMergedAsync(DictCodes.TodoPriority, PriorityFallback, ct: ct);

    private static string StatusText(byte s, IReadOnlyDictionary<string, string> map)
    {
        var key = s.ToString();
        return map.TryGetValue(key, out var t) ? t : StatusText(s);
    }

    public async Task<List<(int Id, string Display)>> LoadActiveDepartmentOptionsAsync(CancellationToken ct)
    {
        return await _db.EDepartments.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.BStatus == "1" || x.BStatus == "启用"))
            .OrderBy(x => x.DeptCode)
            .Select(x => new ValueTuple<int, string>(x.DataId, $"{x.DeptCode} - {x.DeptCName}".Trim()))
            .ToListAsync(ct);
    }

    public async Task<List<(int Id, string Display)>> LoadActivePositionOptionsAsync(CancellationToken ct)
    {
        return await _db.EPositions.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.BStatus == "1" || x.BStatus == "启用"))
            .OrderBy(x => x.PostCode)
            .Select(x => new ValueTuple<int, string>(x.DataId, $"{x.PostCode} - {x.PostCName}".Trim()))
            .ToListAsync(ct);
    }

    public async Task<int?> ResolveEUserDataIdByMemberIdAsync(string? memberId, CancellationToken ct)
    {
        var m = (memberId ?? "").Trim();
        if (m.Length == 0) return null;
        return await _db.EUsers.AsNoTracking()
            .Where(x => x.LoginId == m)
            .Select(x => (int?)x.DataId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<(IReadOnlyList<ETodoTaskListRowVm> pageRows, int totalRecords, int totalPages, int page)> GetIndexPageAsync(
        int currentUserId,
        string? status1,
        string? eventCode1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.ETodoTasks.AsNoTracking()
            .Where(x => x.UserId == currentUserId)
            .ToListAsync(ct);

        var statusMap = await GetStatusMapAsync(ct);
        var priorityMap = await GetPriorityMapAsync(ct);

        var st = (status1 ?? "").Trim();
        if (st.Length > 0 && byte.TryParse(st, out var sb))
            rows = rows.Where(x => x.Status == sb).ToList();

        var ev = (eventCode1 ?? "").Trim();
        if (ev.Length > 0)
            rows = rows.Where(x => string.Equals(x.EventCode, ev, StringComparison.OrdinalIgnoreCase)).ToList();

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            var userSelf = await _db.EUsers.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == currentUserId, ct);
            rows = (searchField ?? "TodoTitle") switch
            {
                "TodoTitle" => rows.Where(x => (x.TodoTitle ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "User" => userSelf != null && (
                        userSelf.LoginId.Contains(kw, StringComparison.OrdinalIgnoreCase)
                        || userSelf.RealName.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    ? rows
                    : new List<ETodoTask>(),
                "EventCode" => rows.Where(x =>
                    (x.EventCode ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "ObjectKey" or "BusinessID" => rows.Where(x =>
                    (x.ObjectKey ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "ObjectType" => rows.Where(x =>
                    (x.ObjectType ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow);

        var u = await _db.EUsers.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == currentUserId, ct);
        var userDisp = u == null ? $"#{currentUserId}" : $"{u.LoginId} - {u.RealName}".Trim();

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x =>
        {
            var bizUrl = EventObjectRefHelper.ResolveBusinessUrl(x.ObjectType, x.ObjectKey, x.ObjectUrl);
            return new ETodoTaskListRowVm
            {
                DataId = x.DataId,
                Status = x.Status,
                TitleDisplay = string.IsNullOrWhiteSpace(x.TodoTitle) ? "(无标题)" : x.TodoTitle!,
                UserDisplay = userDisp,
                StatusText = StatusText(x.Status, statusMap),
                PriorityText = PriorityText(x.Priority, priorityMap),
                CreateTime = x.CreateTime,
                HandleTimeDisplay = x.HandleTime.HasValue ? x.HandleTime.Value.ToString("yyyy-MM-dd HH:mm") : "-",
                ObjectType = x.ObjectType ?? "",
                ObjectKey = x.ObjectKey ?? "",
                ObjectRefDisplay = EventObjectRefHelper.FormatDisplay(x.ObjectType, x.ObjectKey),
                EventCode = x.EventCode,
                BusinessUrl = bizUrl,
                BusinessLinkLabel = EventObjectRefHelper.BusinessLinkLabel(x.ObjectType),
                EventLogId = x.EventLogId
            };
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<ETodoTaskDetailVm?> GetDetailForUserAsync(int id, int currentUserId, CancellationToken ct)
    {
        var x = await _db.ETodoTasks.AsNoTracking().FirstOrDefaultAsync(t => t.DataId == id && t.UserId == currentUserId, ct);
        if (x == null) return null;

        var statusMap = await GetStatusMapAsync(ct);
        var priorityMap = await GetPriorityMapAsync(ct);

        var u = await _db.EUsers.AsNoTracking().FirstOrDefaultAsync(e => e.DataId == x.UserId, ct);
        string? dd = null, pd = null;
        if (x.HandlerDeptId.HasValue)
        {
            var d = await _db.EDepartments.AsNoTracking().FirstOrDefaultAsync(e => e.DataId == x.HandlerDeptId.Value, ct);
            dd = d == null ? null : $"{d.DeptCode} - {d.DeptCName}".Trim();
        }
        if (x.HandlerPosId.HasValue)
        {
            var p = await _db.EPositions.AsNoTracking().FirstOrDefaultAsync(e => e.DataId == x.HandlerPosId.Value, ct);
            pd = p == null ? null : $"{p.PostCode} - {p.PostCName}".Trim();
        }
        var bizUrl = EventObjectRefHelper.ResolveBusinessUrl(x.ObjectType, x.ObjectKey, x.ObjectUrl);
        return new ETodoTaskDetailVm
        {
            DataId = x.DataId,
            EventLogId = x.EventLogId,
            UserId = x.UserId,
            UserDisplay = u == null ? $"#{x.UserId}" : $"{u.LoginId} - {u.RealName}".Trim(),
            ObjectType = x.ObjectType ?? "",
            ObjectKey = x.ObjectKey ?? "",
            ObjectRefDisplay = EventObjectRefHelper.FormatDisplay(x.ObjectType, x.ObjectKey),
            ObjectTitle = x.ObjectTitle,
            EventCode = x.EventCode,
            BusinessUrl = bizUrl,
            BusinessLinkLabel = EventObjectRefHelper.BusinessLinkLabel(x.ObjectType),
            Status = x.Status,
            StatusText = StatusText(x.Status, statusMap),
            Priority = x.Priority,
            PriorityText = PriorityText(x.Priority, priorityMap),
            DueTime = x.DueTime,
            TodoTitle = x.TodoTitle,
            HandlerDeptId = x.HandlerDeptId,
            HandlerDeptDisplay = dd,
            HandlerPosId = x.HandlerPosId,
            HandlerPosDisplay = pd,
            CreateTime = x.CreateTime,
            HandleTime = x.HandleTime
        };
    }

    public async Task<(bool ok, string message)> TryHandleAsync(int id, int currentUserId, byte newStatus, int? handlerDeptId, int? handlerPosId, CancellationToken ct)
    {
        if (newStatus is not 1 and not 2)
            return (false, "无效的目标状态。");

        var row = await _db.ETodoTasks.FirstOrDefaultAsync(t => t.DataId == id && t.UserId == currentUserId, ct);
        if (row == null)
            return (false, "待办不存在或无权操作。");

        if (handlerDeptId.HasValue
            && !await _db.EDepartments.AsNoTracking().AnyAsync(x => x.DataId == handlerDeptId.Value, ct))
            return (false, "处理部门不存在。");
        if (handlerPosId.HasValue
            && !await _db.EPositions.AsNoTracking().AnyAsync(x => x.DataId == handlerPosId.Value, ct))
            return (false, "处理岗位不存在。");

        var cur = row.Status;
        if (cur == 2)
            return (false, "已关闭的待办不可再变更。");
        if (cur == newStatus)
            return (true, "状态未变化（幂等）。");

        if (cur == 0 && (newStatus == 1 || newStatus == 2)) { }
        else if (cur == 1 && newStatus == 2) { }
        else
            return (false, "不允许的状态流转。");

        row.Status = newStatus;
        if (!row.HandleTime.HasValue)
            row.HandleTime = DateTime.Now;
        if (handlerDeptId.HasValue) row.HandlerDeptId = handlerDeptId;
        if (handlerPosId.HasValue) row.HandlerPosId = handlerPosId;

        await _db.SaveChangesAsync(ct);
        return (true, "已保存。");
    }

    private static List<ETodoTask> SortRows(List<ETodoTask> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        IEnumerable<ETodoTask> ordered = field switch
        {
            "CreateTime" => desc ? rows.OrderByDescending(x => x.CreateTime) : rows.OrderBy(x => x.CreateTime),
            "Status" => desc ? rows.OrderByDescending(x => x.Status) : rows.OrderBy(x => x.Status),
            "DataID" => desc ? rows.OrderByDescending(x => x.DataId) : rows.OrderBy(x => x.DataId),
            _ => rows.OrderByDescending(x => x.CreateTime).ThenByDescending(x => x.DataId)
        };
        return ordered.ToList();
    }
}
