using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class ESubscriptionService
{
    private readonly FrameworkDbContext _db;
    private readonly DictService _dict;

    public ESubscriptionService(FrameworkDbContext db, DictService dict)
    {
        _db = db;
        _dict = dict;
    }

    public string Normalize(string? value, int maxLen, string fallback = "")
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) v = fallback;
        return v.Length <= maxLen ? v : v[..maxLen];
    }

    public static IReadOnlyDictionary<string, string> SubTypeMap { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["EVENT"] = "事件订阅",
        ["RESOURCE"] = "资源订阅",
        ["MIXED"] = "混合订阅"
    };

    public static List<(string Value, string Text)> BuildAppCodeFormOptions() =>
    [
        ("FRAME", "FRAME 框架"),
        ("CRM", "CRM"),
        ("OA", "OA")
    ];

    private static List<(string Value, string Text)> FallbackSubTypeFormOptions() =>
        SubTypeMap.Select(x => (x.Key, x.Value)).ToList();

    public Task<List<(string Value, string Text)>> GetSubTypeFilterOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.SubType, forFilter: true, formEmptyLabel: null,
            FallbackSubTypeFormOptions(), ct: ct);

    public Task<List<(string Value, string Text)>> GetSubTypeFormOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.SubType, forFilter: false, formEmptyLabel: null,
            FallbackSubTypeFormOptions(), ct: ct);

    public Task<Dictionary<string, string>> GetSubTypeMapAsync(CancellationToken ct) =>
        _dict.GetItemMapMergedAsync(DictCodes.SubType, SubTypeMap, ct: ct);

    public static List<(string Value, string Text)> BuildBStatusFilterOptions() =>
    [
        ("", "全部"),
        ("1", "启用"),
        ("2", "停用")
    ];

    public static List<(string Value, string Text)> BuildBStatusFormOptions() =>
    [
        ("1", "启用"),
        ("2", "停用")
    ];

    public static List<(string Value, string Text)> BuildSearchFieldOptions() =>
    [
        ("Duty", "职责"),
        ("AppCode", "应用编码"),
        ("SubType", "订阅类型"),
        ("EventCode", "事件编码"),
        ("ResourceID", "资源编码"),
        ("FunctionLimit", "功能限制"),
        ("Remark", "备注")
    ];

    public static string NormalizeBStatus(string? value) => EBStatusHelper.NormalizeBStatusForSave(value);

    public static string ToStatusDisplay(string? value)
    {
        var v = (value ?? "").Trim();
        return v switch
        {
            "1" or "启用" => "启用",
            "2" or "停用" => "停用",
            _ => v
        };
    }

    public void NormalizeFormForSave(ESubscriptionFormVm model)
    {
        model.AppCode = Normalize(model.AppCode, 50, "FRAME");
        model.SubType = Normalize(model.SubType, 20, "EVENT").ToUpperInvariant();
        model.EventCode = string.IsNullOrWhiteSpace(model.EventCode) ? null : Normalize(model.EventCode, 100);
        model.ResourceId = string.IsNullOrWhiteSpace(model.ResourceId) ? null : Normalize(model.ResourceId, 100);
        model.FunctionLimit = string.IsNullOrWhiteSpace(model.FunctionLimit) ? null : Normalize(model.FunctionLimit, 50);
        model.Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark.Trim();
        model.BStatus = NormalizeBStatus(model.BStatus);

        switch (model.SubType)
        {
            case "EVENT":
                model.ResourceId = null;
                break;
            case "RESOURCE":
                model.EventCode = null;
                model.AppCode = Normalize(model.AppCode, 50, "FRAME");
                break;
        }
    }

    public ESubscriptionFormVm ToForm(ESubscription row) => new()
    {
        DataId = row.DataId,
        DutyId = row.DutyId,
        AppCode = string.IsNullOrWhiteSpace(row.AppCode) ? "FRAME" : row.AppCode.Trim(),
        SubType = row.SubType,
        EventCode = row.EventCode,
        ResourceId = row.ResourceId,
        DeptId = row.DeptId,
        IsPrimary = row.IsPrimary,
        FunctionLimit = row.FunctionLimit,
        DispSeq = row.DispSeq,
        BStatus = NormalizeBStatus(row.BStatus),
        Remark = row.Remark
    };

    public async Task<List<(int Id, string Code, string Name)>> LoadActiveDutiesAsync(CancellationToken ct)
    {
        return await _db.EEDuties.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.DutyCode)
            .Select(x => new ValueTuple<int, string, string>(x.DataId, x.DutyCode, x.DutyCName))
            .ToListAsync(ct);
    }

    public async Task<List<(string Code, string Name)>> LoadActiveEventsAsync(string? appCode, CancellationToken ct)
    {
        var ac = (appCode ?? "").Trim();
        var q = _db.EEventConfigs.AsNoTracking().Where(x => !x.IsDeleted);
        if (ac.Length > 0)
            q = q.Where(x => x.AppCode == ac);
        var rows = await q.WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.EventCode)
            .ToListAsync(ct);
        return rows
            .Select(x => (x.EventCode, $"{x.AppCode}.{x.EventCode} - {(x.EventName ?? "")}".Trim()))
            .ToList();
    }

    public async Task<List<(string Code, string DisplayText)>> LoadActiveResourcesAsync(CancellationToken ct)
    {
        var rows = await _db.EResources.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.ResourceId)
            .ToListAsync(ct);
        return rows
            .Select(x =>
            {
                var name = x.ResourceName ?? "";
                var typeDisp = EResourceService.ToResourceTypeDisplay(x.ResourceType);
                var suffix = typeDisp == "-" ? "" : $" · {typeDisp}";
                return (x.ResourceId, $"{x.ResourceId} - {name}{suffix}".Trim());
            }).ToList();
    }

    public async Task<List<(int Id, string Code, string Name)>> LoadActiveDepartmentsAsync(CancellationToken ct)
    {
        return await _db.EDepartments.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.DeptCode)
            .Select(x => new ValueTuple<int, string, string>(x.DataId, x.DeptCode, x.DeptCName))
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<ESubscriptionListRowVm> pageRows, int totalRecords, int totalPages, int page)> GetIndexPageAsync(
        string? bStatus1,
        string? subType1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.ESubscriptions.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(ct);
        var dutyIds = rows.Select(x => x.DutyId).Distinct().ToList();
        var deptIds = rows.Where(x => x.DeptId.HasValue).Select(x => x.DeptId!.Value).Distinct().ToList();

        var duties = await _db.EEDuties.AsNoTracking()
            .Where(x => dutyIds.Contains(x.DataId))
            .Select(x => new { x.DataId, x.DutyCode, x.DutyCName })
            .ToListAsync(ct);
        var depts = await _db.EDepartments.AsNoTracking()
            .Where(x => deptIds.Contains(x.DataId))
            .Select(x => new { x.DataId, x.DeptCode, x.DeptCName })
            .ToListAsync(ct);

        var dutyMap = duties.ToDictionary(x => x.DataId, x => $"{x.DutyCode} - {x.DutyCName}".Trim());
        var deptMap = depts.ToDictionary(x => x.DataId, x => $"{x.DeptCode} - {x.DeptCName}".Trim());

        rows = rows.Where(x => MatchBStatusFilter(x.BStatus, bStatus1) && MatchSubTypeFilter(x.SubType, subType1)).ToList();

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            bool Match(string? s) => (s ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase);
            rows = (searchField ?? "Duty") switch
            {
                "Duty" => rows.Where(x => dutyMap.TryGetValue(x.DutyId, out var d) && Match(d)).ToList(),
                "AppCode" => rows.Where(x => Match(x.AppCode)).ToList(),
                "SubType" => rows.Where(x => Match(x.SubType)).ToList(),
                "EventCode" => rows.Where(x => Match(x.EventCode)).ToList(),
                "ResourceID" => rows.Where(x => Match(x.ResourceId)).ToList(),
                "FunctionLimit" => rows.Where(x => Match(x.FunctionLimit)).ToList(),
                "Remark" => rows.Where(x => Match(x.Remark)).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow, dutyMap);
        var subTypeMap = await GetSubTypeMapAsync(ct);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new ESubscriptionListRowVm
        {
            DataId = x.DataId,
            DutyId = x.DutyId,
            DutyDisplay = dutyMap.TryGetValue(x.DutyId, out var d) ? d : $"#{x.DutyId}",
            AppCode = string.IsNullOrWhiteSpace(x.AppCode) ? "FRAME" : x.AppCode,
            SubType = x.SubType,
            SubTypeText = subTypeMap.TryGetValue(x.SubType, out var st) ? st : x.SubType,
            EventCode = string.IsNullOrWhiteSpace(x.EventCode) ? "-" : x.EventCode!,
            ResourceId = string.IsNullOrWhiteSpace(x.ResourceId) ? "-" : x.ResourceId!,
            DeptId = x.DeptId,
            DeptDisplay = x.DeptId.HasValue ? (deptMap.TryGetValue(x.DeptId.Value, out var dep) ? dep : $"#{x.DeptId.Value}") : "不限",
            BStatusText = ToStatusDisplay(x.BStatus)
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(ESubscriptionFormVm model, bool isEdit, CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();
        var subTypeMap = await GetSubTypeMapAsync(ct);
        if (!subTypeMap.ContainsKey(model.SubType))
            errors.Add((nameof(model.SubType), "订阅类型无效。"));

        if (!await _db.EEDuties.AsNoTracking().AnyAsync(x => x.DataId == model.DutyId && !x.IsDeleted, ct))
            errors.Add((nameof(model.DutyId), "选择的职责不存在。"));
        if (model.DeptId.HasValue && !await _db.EDepartments.AsNoTracking().AnyAsync(x => x.DataId == model.DeptId.Value && !x.IsDeleted, ct))
            errors.Add((nameof(model.DeptId), "选择的部门不存在。"));

        switch (model.SubType)
        {
            case "EVENT":
            case "MIXED":
                if (string.IsNullOrWhiteSpace(model.EventCode))
                    errors.Add((nameof(model.EventCode), "事件/混合订阅必须选择事件编码。"));
                break;
            case "RESOURCE":
                if (string.IsNullOrWhiteSpace(model.ResourceId))
                    errors.Add((nameof(model.ResourceId), "资源订阅必须选择资源编码。"));
                break;
        }

        if (!string.IsNullOrWhiteSpace(model.EventCode) &&
            !await _db.EEventConfigs.AsNoTracking().AnyAsync(x =>
                !x.IsDeleted && x.AppCode == model.AppCode && x.EventCode == model.EventCode, ct))
            errors.Add((nameof(model.EventCode), "所选应用下不存在该事件编码。"));
        if (!string.IsNullOrWhiteSpace(model.ResourceId) &&
            !await _db.EResources.AsNoTracking().AnyAsync(x => !x.IsDeleted && x.ResourceId == model.ResourceId, ct))
            errors.Add((nameof(model.ResourceId), "选择的资源编码不存在。"));

        var deptNorm = model.DeptId ?? 0;
        var eventNorm = model.EventCode ?? "";
        var resNorm = model.ResourceId ?? "";
        var appNorm = model.AppCode;
        var dup = await _db.ESubscriptions.AsNoTracking().AnyAsync(x =>
            !x.IsDeleted
            && x.DutyId == model.DutyId
            && (x.DeptId ?? 0) == deptNorm
            && x.SubType == model.SubType
            && (x.AppCode ?? "FRAME") == appNorm
            && (x.EventCode ?? "") == eventNorm
            && (x.ResourceId ?? "") == resNorm
            && (!isEdit || x.DataId != model.DataId), ct);
        if (dup)
            errors.Add((nameof(model.SubType), "相同职责/应用/部门下，该订阅组合已存在。"));

        if (model.IsPrimary)
        {
            var hasPrimary = await _db.ESubscriptions.AsNoTracking().AnyAsync(x =>
                !x.IsDeleted
                && x.DutyId == model.DutyId
                && (x.DeptId ?? 0) == deptNorm
                && x.SubType == model.SubType
                && x.IsPrimary
                && (!isEdit || x.DataId != model.DataId), ct);
            if (hasPrimary)
                errors.Add((nameof(model.IsPrimary), "同一职责+部门+类型下只能有一条主订阅。"));
        }

        return errors;
    }

    public async Task CreateAsync(ESubscriptionFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var row = new ESubscription
        {
            DutyId = model.DutyId,
            AppCode = model.AppCode,
            SubType = model.SubType,
            EventCode = model.EventCode,
            ResourceId = model.ResourceId,
            DeptId = model.DeptId,
            IsPrimary = model.IsPrimary,
            FunctionLimit = model.FunctionLimit,
            DispSeq = model.DispSeq,
            BStatus = model.BStatus,
            Remark = model.Remark,
            IsDeleted = false,
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        };
        _db.ESubscriptions.Add(row);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> TryUpdateAsync(int id, ESubscriptionFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.ESubscriptions.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return false;
        var changed = false;
        if (row.DutyId != model.DutyId) { row.DutyId = model.DutyId; changed = true; }
        if (!string.Equals(row.AppCode ?? "", model.AppCode, StringComparison.OrdinalIgnoreCase)) { row.AppCode = model.AppCode; changed = true; }
        if (!string.Equals(row.SubType, model.SubType, StringComparison.Ordinal)) { row.SubType = model.SubType; changed = true; }
        if (!string.Equals((row.EventCode ?? "").Trim(), (model.EventCode ?? "").Trim(), StringComparison.Ordinal)) { row.EventCode = model.EventCode; changed = true; }
        if (!string.Equals((row.ResourceId ?? "").Trim(), (model.ResourceId ?? "").Trim(), StringComparison.Ordinal)) { row.ResourceId = model.ResourceId; changed = true; }
        if (row.DeptId != model.DeptId) { row.DeptId = model.DeptId; changed = true; }
        if (row.IsPrimary != model.IsPrimary) { row.IsPrimary = model.IsPrimary; changed = true; }
        if (!string.Equals((row.FunctionLimit ?? "").Trim(), (model.FunctionLimit ?? "").Trim(), StringComparison.Ordinal)) { row.FunctionLimit = model.FunctionLimit; changed = true; }
        if (row.DispSeq != model.DispSeq) { row.DispSeq = model.DispSeq; changed = true; }
        if (!string.Equals((row.BStatus ?? "").Trim(), model.BStatus, StringComparison.Ordinal)) { row.BStatus = model.BStatus; changed = true; }
        if (!string.Equals((row.Remark ?? "").Trim(), (model.Remark ?? "").Trim(), StringComparison.Ordinal)) { row.Remark = model.Remark; changed = true; }
        if (!changed) return true;
        row.AmendDate = DateTime.Now;
        row.OperatorName = operatorId;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.ESubscriptions.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return;
        row.IsDeleted = true;
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return;
        var rows = await _db.ESubscriptions.Where(x => ids.Contains(x.DataId) && !x.IsDeleted).ToListAsync(ct);
        if (rows.Count == 0) return;
        var now = DateTime.Now;
        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.AmendDate = now;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ESubscription?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.ESubscriptions.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);

    private static bool MatchBStatusFilter(string? value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        return selected switch
        {
            "1" => EBStatusHelper.IsActiveBStatus(value),
            "2" => !EBStatusHelper.IsActiveBStatus(value) && !string.IsNullOrWhiteSpace(value),
            _ => true
        };
    }

    private static bool MatchSubTypeFilter(string? value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        return string.Equals((value ?? "").Trim(), selected.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static List<ESubscription> SortRows(List<ESubscription> rows, string field, string arrow, Dictionary<int, string> dutyMap)
    {
        var desc = arrow == "1";
        string D(int id) => dutyMap.TryGetValue(id, out var v) ? v : "";
        return field switch
        {
            "Duty" => desc ? rows.OrderByDescending(x => D(x.DutyId)).ToList() : rows.OrderBy(x => D(x.DutyId)).ToList(),
            "AppCode" => desc ? rows.OrderByDescending(x => x.AppCode).ToList() : rows.OrderBy(x => x.AppCode).ToList(),
            "SubType" => desc ? rows.OrderByDescending(x => x.SubType).ToList() : rows.OrderBy(x => x.SubType).ToList(),
            "EventCode" => desc ? rows.OrderByDescending(x => x.EventCode).ToList() : rows.OrderBy(x => x.EventCode).ToList(),
            "ResourceID" => desc ? rows.OrderByDescending(x => x.ResourceId).ToList() : rows.OrderBy(x => x.ResourceId).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            _ => rows.OrderByDescending(x => x.DataId).ToList()
        };
    }
}
