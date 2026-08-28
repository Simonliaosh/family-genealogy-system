using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class EResourceService
{
    private readonly FrameworkDbContext _db;
    private readonly DictService _dict;

    /// <summary>资源类型回退码表（字典 RESOURCE_TYPE 未配置时使用）。</summary>
    public static IReadOnlyDictionary<string, string> ResourceTypeMap { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PAGE"] = "页面",
            ["GROUP"] = "分组",
            ["MENU"] = "菜单",
            ["BUTTON"] = "按钮",
            ["API"] = "API"
        };

    public EResourceService(FrameworkDbContext db, DictService dict)
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

    private static List<(string Value, string Text)> FallbackResourceTypeFormOptions() =>
        ResourceTypeMap.OrderBy(x => x.Key).Select(x => (x.Key, x.Value)).ToList();

    public Task<List<(string Value, string Text)>> GetResourceTypeFilterOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.ResourceType, forFilter: true, formEmptyLabel: null,
            FallbackResourceTypeFormOptions(), ct: ct);

    public Task<List<(string Value, string Text)>> GetResourceTypeFormOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.ResourceType, forFilter: false, formEmptyLabel: "（未指定）",
            FallbackResourceTypeFormOptions(), ct: ct);

    public Task<Dictionary<string, string>> GetResourceTypeMapAsync(CancellationToken ct) =>
        _dict.GetItemMapMergedAsync(DictCodes.ResourceType, ResourceTypeMap, ct: ct);

    public static List<(string Value, string Text)> BuildAppCodeFormOptions() =>
    [
        ("FRAME", "FRAME 框架"),
        ("CRM", "CRM"),
        ("OA", "OA")
    ];

    public static List<(string Value, string Text)> BuildAppCodeFilterOptions()
    {
        var list = new List<(string, string)> { ("", "全部") };
        list.AddRange(BuildAppCodeFormOptions());
        return list;
    }

    public async Task<List<(string Value, string Text)>> LoadActiveMenuGroupOptionsAsync(string? appCode, CancellationToken ct)
    {
        var ac = (appCode ?? "").Trim();
        var q = _db.EMenuGroups.AsNoTracking().Where(x => !x.IsDeleted);
        if (ac.Length > 0)
            q = q.Where(x => x.AppCode == ac);

        var rows = await q.WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.DispSeq).ThenBy(x => x.MenuGroupCode)
            .ToListAsync(ct);
        return rows
            .Select(x => (x.MenuGroupCode, $"{x.MenuGroupCode} - {x.MenuGroupName}".Trim()))
            .ToList();
    }

    public string NormalizeBStatus(string? value) => EBStatusHelper.NormalizeBStatusForSave(value);

    public string ToStatusDisplay(string? value)
    {
        var v = (value ?? "").Trim();
        return v switch
        {
            "1" or "启用" => "启用",
            "2" or "停用" => "停用",
            _ => string.IsNullOrEmpty(v) ? "-" : v
        };
    }

    public static string ToResourceTypeDisplay(string? code) =>
        ToResourceTypeDisplay(code, ResourceTypeMap);

    public static string ToResourceTypeDisplay(string? code, IReadOnlyDictionary<string, string> typeMap)
    {
        var c = (code ?? "").Trim();
        if (c.Length == 0) return "-";
        return typeMap.TryGetValue(c, out var t) ? t : c;
    }

    public string? NormalizeResourceTypeOrNull(string? value)
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) return null;
        v = v.Length <= 30 ? v.ToUpperInvariant() : v[..30].ToUpperInvariant();
        return v;
    }

    public EResourceFormVm ToForm(EResource row)
    {
        return new EResourceFormVm
        {
            DataId = row.DataId,
            AppCode = string.IsNullOrWhiteSpace(row.AppCode) ? "FRAME" : row.AppCode.Trim(),
            ResourceId = row.ResourceId ?? "",
            ResourceName = row.ResourceName ?? "",
            ResourceType = string.IsNullOrWhiteSpace(row.ResourceType) ? "" : row.ResourceType!.Trim().ToUpperInvariant(),
            MenuPath = row.MenuPath ?? "",
            MenuGroupCode = row.MenuGroupCode ?? "",
            BStatus = NormalizeBStatus(row.BStatus),
            Remark = row.Remark ?? ""
        };
    }

    public void NormalizeFormForSave(EResourceFormVm model, bool resourceIdEditable)
    {
        model.AppCode = Normalize(model.AppCode, 50, "FRAME");
        if (resourceIdEditable)
            model.ResourceId = Normalize(model.ResourceId, 100);
        model.ResourceName = Normalize(model.ResourceName, 100);
        var rt = NormalizeResourceTypeOrNull(model.ResourceType);
        model.ResourceType = string.IsNullOrEmpty(rt) ? null : rt;
        model.MenuPath = string.IsNullOrWhiteSpace(model.MenuPath) ? null : Normalize(model.MenuPath, 300);
        var mg = (model.MenuGroupCode ?? "").Trim();
        model.MenuGroupCode = mg.Length == 0 ? null : (mg.Length <= 50 ? mg.ToUpperInvariant() : mg[..50].ToUpperInvariant());
        model.BStatus = NormalizeBStatus(model.BStatus);
        if (string.IsNullOrWhiteSpace(model.Remark))
            model.Remark = null;
        else
        {
            var r = model.Remark.Trim();
            model.Remark = r.Length <= 4000 ? r : r[..4000];
        }
    }

    public async Task<(IReadOnlyList<EResourceListRowVm> pageRows, int totalRecords, int totalPages, int page)> GetIndexPageAsync(
        string? bStatus1,
        string? appCode1,
        string? resourceType1,
        string? menuGroupCode1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EResources.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(ct);

        rows = rows.Where(x => MatchBStatusFilter(x.BStatus, bStatus1)
                            && MatchAppCodeFilter(x.AppCode, appCode1)
                            && MatchResourceTypeFilter(x.ResourceType, resourceType1)
                            && MatchMenuGroupFilter(x.MenuGroupCode, menuGroupCode1)).ToList();

        var sc = (searchContent ?? "").Trim();
        if (sc.Length > 0)
        {
            rows = (searchField ?? "ResourceID") switch
            {
                "AppCode" => rows.Where(x => (x.AppCode ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "ResourceID" => rows.Where(x => (x.ResourceId ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "ResourceName" => rows.Where(x => (x.ResourceName ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "MenuPath" => rows.Where(x => (x.MenuPath ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "Remark" => rows.Where(x => (x.Remark ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow);

        var typeMap = await GetResourceTypeMapAsync(ct);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EResourceListRowVm
        {
            DataId = x.DataId,
            AppCode = string.IsNullOrWhiteSpace(x.AppCode) ? "FRAME" : x.AppCode,
            ResourceId = x.ResourceId ?? "",
            ResourceName = x.ResourceName ?? "",
            ResourceTypeText = ToResourceTypeDisplay(x.ResourceType, typeMap),
            MenuPath = string.IsNullOrWhiteSpace(x.MenuPath) ? "-" : x.MenuPath!,
            BStatusText = ToStatusDisplay(x.BStatus)
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<List<(string Value, string Text)>> BuildMenuGroupFilterOptionsAsync(string? appCode, CancellationToken ct)
    {
        var list = new List<(string Value, string Text)> { ("", "全部") };
        var options = await LoadActiveMenuGroupOptionsAsync(appCode, ct);
        list.AddRange(options);
        return list;
    }

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(EResourceFormVm model, bool isEdit, bool resourceIdEditable, CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();
        if (model.ResourceName.Length == 0)
            errors.Add((nameof(EResourceFormVm.ResourceName), "资源名称不能为空。"));
        if (model.BStatus != "1" && model.BStatus != "2")
            errors.Add((nameof(EResourceFormVm.BStatus), "状态无效。"));

        var typeMap = await GetResourceTypeMapAsync(ct);
        if (!string.IsNullOrEmpty(model.ResourceType) && !typeMap.ContainsKey(model.ResourceType))
            errors.Add((nameof(EResourceFormVm.ResourceType), "资源类型不在允许的码表中。"));

        if (!string.IsNullOrWhiteSpace(model.MenuGroupCode))
        {
            var menuGroupOk = await _db.EMenuGroups.AsNoTracking()
                .AnyAsync(x => !x.IsDeleted && x.AppCode == model.AppCode && x.MenuGroupCode == model.MenuGroupCode, ct);
            if (!menuGroupOk)
                errors.Add((nameof(EResourceFormVm.MenuGroupCode), "当前应用下不存在该菜单组编码。"));
        }

        if (resourceIdEditable)
        {
            if (model.ResourceId.Length == 0)
                errors.Add((nameof(EResourceFormVm.ResourceId), "资源编码不能为空。"));
            var dup = await _db.EResources.AsNoTracking()
                .AnyAsync(x => !x.IsDeleted && x.ResourceId == model.ResourceId && (!isEdit || x.DataId != model.DataId), ct);
            if (dup)
                errors.Add((nameof(EResourceFormVm.ResourceId), "资源编码已存在，请换一个。"));
        }

        return errors;
    }

    public async Task CreateAsync(EResourceFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var entity = new EResource
        {
            AppCode = model.AppCode,
            ResourceId = model.ResourceId,
            IsDeleted = false,
            ResourceName = model.ResourceName,
            ResourceType = model.ResourceType,
            MenuPath = model.MenuPath,
            MenuGroupCode = model.MenuGroupCode,
            BStatus = NormalizeBStatus(model.BStatus),
            Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark,
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        };
        _db.EResources.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("资源编码已存在，请换一个。", ex);
        }
    }

    public async Task<bool> TryUpdateAsync(int id, EResourceFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.EResources.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return false;

        var bStatus = NormalizeBStatus(model.BStatus);
        var changed = false;
        if (!string.Equals(row.AppCode ?? "", model.AppCode, StringComparison.OrdinalIgnoreCase)) { row.AppCode = model.AppCode; changed = true; }
        if (!string.Equals((row.ResourceName ?? "").Trim(), model.ResourceName, StringComparison.Ordinal)) { row.ResourceName = model.ResourceName; changed = true; }
        if (!string.Equals((row.ResourceType ?? "").Trim(), (model.ResourceType ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) { row.ResourceType = model.ResourceType; changed = true; }
        if (!string.Equals((row.MenuPath ?? "").Trim(), (model.MenuPath ?? "").Trim(), StringComparison.Ordinal)) { row.MenuPath = model.MenuPath; changed = true; }
        if (!string.Equals((row.MenuGroupCode ?? "").Trim(), (model.MenuGroupCode ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) { row.MenuGroupCode = model.MenuGroupCode; changed = true; }
        if (!string.Equals((row.BStatus ?? "").Trim(), bStatus, StringComparison.Ordinal)) { row.BStatus = bStatus; changed = true; }
        if (!string.Equals((row.Remark ?? "").Trim(), (model.Remark ?? "").Trim(), StringComparison.Ordinal)) { row.Remark = model.Remark; changed = true; }
        if (!changed) return true;

        row.AmendDate = DateTime.Now;
        row.OperatorName = operatorId;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.EResources.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return;
        row.IsDeleted = true;
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return;
        var rows = await _db.EResources.Where(x => ids.Contains(x.DataId) && !x.IsDeleted).ToListAsync(ct);
        if (rows.Count <= 0) return;
        var now = DateTime.Now;
        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.AmendDate = now;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<EResource?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.EResources.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);

    private static List<EResource> SortRows(List<EResource> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        return field switch
        {
            "AppCode" => desc ? rows.OrderByDescending(x => x.AppCode).ToList() : rows.OrderBy(x => x.AppCode).ToList(),
            "ResourceID" => desc ? rows.OrderByDescending(x => x.ResourceId).ToList() : rows.OrderBy(x => x.ResourceId).ToList(),
            "ResourceName" => desc ? rows.OrderByDescending(x => x.ResourceName).ToList() : rows.OrderBy(x => x.ResourceName).ToList(),
            "ResourceType" => desc ? rows.OrderByDescending(x => x.ResourceType).ToList() : rows.OrderBy(x => x.ResourceType).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            _ => rows.OrderBy(x => x.ResourceId).ToList()
        };
    }

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

    private static bool MatchAppCodeFilter(string? value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        return string.Equals((value ?? "FRAME").Trim(), selected.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchResourceTypeFilter(string? value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        var v = (value ?? "").Trim();
        return string.Equals(v, selected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchMenuGroupFilter(string? value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        return string.Equals((value ?? "").Trim(), selected.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        for (var e = ex.InnerException; e != null; e = e.InnerException)
        {
            if (e is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
                return true;
        }
        return false;
    }
}
