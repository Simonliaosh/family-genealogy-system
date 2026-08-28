using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>
/// 用户交接记录（方案 A：登记 + 查询）。
/// 定稿：1) 禁止交出方与接收方为同一用户。2) 首版<strong>仅落库</strong>，不调用权限/待办/客户等转派服务（后续在需求中写「例外」再接 X 服务）。
/// 3) 防重：同一 (SourceUserID, TargetUserID, HandoverType) 在近 10 分钟内不允许重复登记。4) OperatorUserID、HandoverTime 仅服务端赋值。
/// HandoverType 码表见 HandoverTypeMap 静态字典。
/// </summary>
public class EUserHandoverService
{
    public const int DuplicateWindowMinutes = 10;

    private readonly FrameworkDbContext _db;
    private readonly DictService _dict;

    /// <summary>v1 页面内置码（字典 HANDOVER_TYPE 未覆盖时回退）。</summary>
    public static IReadOnlyDictionary<string, string> HandoverTypeFallback { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["LEAVE"] = "请假",
            ["TRANSFER"] = "调岗",
            ["RESIGN"] = "离职",
            ["TEMP"] = "临时",
            ["PERMISSION"] = "权限交接",
            ["TODO"] = "待办交接",
            ["CUSTOMER"] = "客户交接",
            ["CONTRACT"] = "合同交接",
            ["ASSET"] = "资产交接",
            ["OTHER"] = "其他"
        };

    public EUserHandoverService(FrameworkDbContext db, DictService dict)
    {
        _db = db;
        _dict = dict;
    }

    public Task<Dictionary<string, string>> GetHandoverTypeMapAsync(CancellationToken ct) =>
        _dict.GetItemMapMergedAsync(DictCodes.HandoverType, HandoverTypeFallback, ct: ct);

    private static List<(string Value, string Text)> FallbackHandoverTypeFormOptions() =>
        HandoverTypeFallback.Select(x => (x.Key, $"{x.Key} - {x.Value}")).ToList();

    public async Task<List<(string Value, string Text)>> GetHandoverTypeFilterOptionsAsync(CancellationToken ct)
    {
        var opts = await _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.HandoverType, forFilter: true, formEmptyLabel: null,
            FallbackHandoverTypeFormOptions(), ct: ct);
        return opts.Select(o => o.Value.Length == 0 ? o : (o.Value, $"{o.Value} - {o.Text}")).ToList();
    }

    public async Task<List<(string Value, string Text)>> GetHandoverTypeFormOptionsAsync(CancellationToken ct)
    {
        var opts = await _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.HandoverType, forFilter: false, formEmptyLabel: null,
            FallbackHandoverTypeFormOptions(), ct: ct);
        return opts.Where(o => o.Value.Length > 0)
            .Select(o => (o.Value, $"{o.Value} - {o.Text}"))
            .ToList();
    }

    public static string ToHandoverTypeText(string? code, IReadOnlyDictionary<string, string>? map = null)
    {
        var c = (code ?? "").Trim();
        if (c.Length == 0) return "-";
        if (map != null && map.TryGetValue(c, out var t))
            return t;
        return HandoverTypeFallback.TryGetValue(c, out var fb) ? fb : c;
    }

    public async Task<List<(int Id, string Display)>> LoadActiveUserOptionsAsync(CancellationToken ct)
    {
        return await _db.EUsers.AsNoTracking()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.LoginId)
            .Select(x => new ValueTuple<int, string>(x.DataId, $"{x.LoginId} - {x.RealName}".Trim()))
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

    public async Task NormalizeFormForSaveAsync(EUserHandoverFormVm model, CancellationToken ct)
    {
        model.HandoverType = (model.HandoverType ?? "").Trim();
        var map = await GetHandoverTypeMapAsync(ct);
        foreach (var key in map.Keys)
        {
            if (string.Equals(key, model.HandoverType, StringComparison.OrdinalIgnoreCase))
            {
                model.HandoverType = key;
                break;
            }
        }
        if (model.HandoverType.Length > 30) model.HandoverType = model.HandoverType[..30];
        var r = (model.Remark ?? "").Trim();
        model.Remark = r.Length == 0 ? null : r;
    }

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(EUserHandoverFormVm model, CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();
        if (model.SourceUserId == model.TargetUserId)
            errors.Add((nameof(model.TargetUserId), "交出方与接收方不能为同一用户。"));

        var typeMap = await GetHandoverTypeMapAsync(ct);
        if (!typeMap.ContainsKey(model.HandoverType))
            errors.Add((nameof(model.HandoverType), "交接类型无效。"));

        if (!await _db.EUsers.AsNoTracking().AnyAsync(x => x.DataId == model.SourceUserId, ct))
            errors.Add((nameof(model.SourceUserId), "交出方用户不存在。"));
        if (!await _db.EUsers.AsNoTracking().AnyAsync(x => x.DataId == model.TargetUserId, ct))
            errors.Add((nameof(model.TargetUserId), "接收方用户不存在。"));

        var since = DateTime.Now.AddMinutes(-DuplicateWindowMinutes);
        var dup = await _db.EUserHandovers.AsNoTracking()
            .AnyAsync(x =>
                x.SourceUserId == model.SourceUserId
                && x.TargetUserId == model.TargetUserId
                && x.HandoverType == model.HandoverType
                && x.HandoverTime >= since, ct);
        if (dup)
            errors.Add((nameof(model.HandoverType), $"近 {DuplicateWindowMinutes} 分钟内已存在相同交出方、接收方与类型的记录，请勿重复登记。"));

        return errors;
    }

    public async Task CreateAsync(EUserHandoverFormVm model, int operatorUserId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var entity = new EUserHandover
        {
            SourceUserId = model.SourceUserId,
            TargetUserId = model.TargetUserId,
            HandoverType = model.HandoverType,
            HandoverTime = now,
            OperatorUserId = operatorUserId,
            Remark = model.Remark
        };
        _db.EUserHandovers.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<EUserHandoverListRowVm> pageRows, int totalRecords, int totalPages, int page)> GetIndexPageAsync(
        string? handoverType1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EUserHandovers.AsNoTracking().ToListAsync(ct);
        var ht = (handoverType1 ?? "").Trim();
        if (ht.Length > 0)
            rows = rows.Where(x => string.Equals(x.HandoverType, ht, StringComparison.OrdinalIgnoreCase)).ToList();

        var userIds = rows.SelectMany(x => new[] { x.SourceUserId, x.TargetUserId, x.OperatorUserId }).Distinct().ToList();
        var users = await _db.EUsers.AsNoTracking().Where(x => userIds.Contains(x.DataId)).ToListAsync(ct);
        var userMap = users.ToDictionary(x => x.DataId, x => $"{x.LoginId} - {x.RealName}".Trim());
        var typeMap = await GetHandoverTypeMapAsync(ct);

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            bool MatchUser(int id) => userMap.TryGetValue(id, out var d) && d.Contains(kw, StringComparison.OrdinalIgnoreCase);
            rows = (searchField ?? "Source") switch
            {
                "Source" => rows.Where(x => MatchUser(x.SourceUserId)).ToList(),
                "Target" => rows.Where(x => MatchUser(x.TargetUserId)).ToList(),
                "Operator" => rows.Where(x => MatchUser(x.OperatorUserId)).ToList(),
                "Remark" => rows.Where(x => (x.Remark ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "HandoverType" => rows.Where(x => x.HandoverType.Contains(kw, StringComparison.OrdinalIgnoreCase)
                    || ToHandoverTypeText(x.HandoverType, typeMap).Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x =>
        {
            var remark = x.Remark ?? "";
            const int rmax = 80;
            var shortR = remark.Length <= rmax ? remark : remark[..rmax] + "…";
            return new EUserHandoverListRowVm
            {
                DataId = x.DataId,
                HandoverTime = x.HandoverTime,
                SourceDisplay = userMap.TryGetValue(x.SourceUserId, out var sd) ? sd : $"#{x.SourceUserId}",
                TargetDisplay = userMap.TryGetValue(x.TargetUserId, out var td) ? td : $"#{x.TargetUserId}",
                HandoverTypeText = $"{x.HandoverType} - {ToHandoverTypeText(x.HandoverType, typeMap)}".Trim(),
                OperatorDisplay = userMap.TryGetValue(x.OperatorUserId, out var od) ? od : $"#{x.OperatorUserId}",
                RemarkShort = string.IsNullOrWhiteSpace(shortR) ? "-" : shortR
            };
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<EUserHandoverDetailVm?> GetDetailAsync(int id, CancellationToken ct)
    {
        var x = await _db.EUserHandovers.AsNoTracking().FirstOrDefaultAsync(e => e.DataId == id, ct);
        if (x == null) return null;
        var typeMap = await GetHandoverTypeMapAsync(ct);
        var ids = new[] { x.SourceUserId, x.TargetUserId, x.OperatorUserId }.Distinct().ToList();
        var users = await _db.EUsers.AsNoTracking().Where(u => ids.Contains(u.DataId)).ToListAsync(ct);
        var map = users.ToDictionary(u => u.DataId, u => $"{u.LoginId} - {u.RealName}".Trim());
        return new EUserHandoverDetailVm
        {
            DataId = x.DataId,
            HandoverTime = x.HandoverTime,
            SourceUserId = x.SourceUserId,
            SourceDisplay = map.TryGetValue(x.SourceUserId, out var sd) ? sd : $"#{x.SourceUserId}",
            TargetUserId = x.TargetUserId,
            TargetDisplay = map.TryGetValue(x.TargetUserId, out var td) ? td : $"#{x.TargetUserId}",
            HandoverType = x.HandoverType,
            HandoverTypeText = ToHandoverTypeText(x.HandoverType, typeMap),
            OperatorUserId = x.OperatorUserId,
            OperatorDisplay = map.TryGetValue(x.OperatorUserId, out var od) ? od : $"#{x.OperatorUserId}",
            Remark = x.Remark
        };
    }

    private static List<EUserHandover> SortRows(List<EUserHandover> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        IEnumerable<EUserHandover> ordered = field switch
        {
            "HandoverTime" => desc ? rows.OrderByDescending(x => x.HandoverTime) : rows.OrderBy(x => x.HandoverTime),
            "HandoverType" => desc ? rows.OrderByDescending(x => x.HandoverType) : rows.OrderBy(x => x.HandoverType),
            "DataID" => desc ? rows.OrderByDescending(x => x.DataId) : rows.OrderBy(x => x.DataId),
            "SourceUserID" => desc ? rows.OrderByDescending(x => x.SourceUserId) : rows.OrderBy(x => x.SourceUserId),
            "TargetUserID" => desc ? rows.OrderByDescending(x => x.TargetUserId) : rows.OrderBy(x => x.TargetUserId),
            _ => rows.OrderByDescending(x => x.HandoverTime).ThenByDescending(x => x.DataId)
        };
        return ordered.ToList();
    }
}
