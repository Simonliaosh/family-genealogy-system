using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public sealed class EAppModuleService
{
    public static readonly HashSet<string> ProtectedAppCodes = new(StringComparer.OrdinalIgnoreCase) { "FRAME" };

    private readonly FrameworkDbContext _db;
    private readonly DictService _dict;

    public static IReadOnlyDictionary<string, string> AppTypeFallback { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FRAMEWORK"] = "框架应用",
            ["BUSINESS"] = "业务应用",
            ["PLUGIN"] = "插件"
        };

    public EAppModuleService(FrameworkDbContext db, DictService dict)
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

    private static List<(string Value, string Text)> FallbackAppTypeFormOptions() =>
        AppTypeFallback.Select(x => (x.Key, x.Value)).ToList();

    public Task<List<(string Value, string Text)>> GetAppTypeFilterOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.AppType, forFilter: true, formEmptyLabel: null,
            FallbackAppTypeFormOptions(), ct: ct);

    public Task<List<(string Value, string Text)>> GetAppTypeFormOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.AppType, forFilter: false, formEmptyLabel: null,
            FallbackAppTypeFormOptions(), ct: ct);

    public Task<Dictionary<string, string>> GetAppTypeMapAsync(CancellationToken ct) =>
        _dict.GetItemMapMergedAsync(DictCodes.AppType, AppTypeFallback, ct: ct);

    public static List<(string Value, string Text)> BuildBStatusFilterOptions() => EDictTypeService.BuildBStatusFilterOptions();
    public static List<(string Value, string Text)> BuildBStatusFormOptions() => EDictTypeService.BuildBStatusFormOptions();
    public static string NormalizeBStatus(string? value) => EBStatusHelper.NormalizeBStatusForSave(value);
    public static string ToStatusDisplay(string? value) => EAppModuleQueryService.ToStatusDisplay(value);

    public static string NormalizeAppType(string? value, IReadOnlyDictionary<string, string>? typeMap = null)
    {
        var v = (value ?? "").Trim().ToUpperInvariant();
        if (typeMap != null && typeMap.ContainsKey(v))
            return v;
        return AppTypeFallback.ContainsKey(v) ? v : "BUSINESS";
    }

    public static string ToAppTypeText(string? code, IReadOnlyDictionary<string, string>? map = null)
    {
        var key = (code ?? "").Trim().ToUpperInvariant();
        if (key.Length == 0) return "-";
        if (map != null && map.TryGetValue(key, out var name))
            return name;
        return AppTypeFallback.TryGetValue(key, out var fb) ? fb : key;
    }

    public EAppModuleFormVm ToForm(EAppModule row) => new()
    {
        DataId = row.DataId,
        AppCode = row.AppCode,
        AppName = row.AppName,
        AppType = NormalizeAppType(row.AppType, null),
        BaseUrl = row.BaseUrl,
        Icon = row.Icon,
        DispSeq = row.DispSeq < 0 ? 0 : row.DispSeq,
        BStatus = NormalizeBStatus(row.BStatus),
        Remark = row.Remark
    };

    public void NormalizeFormForSave(EAppModuleFormVm model, bool isEdit)
    {
        model.AppCode = Normalize(model.AppCode, 50).ToUpperInvariant();
        model.AppName = Normalize(model.AppName, 100);
        model.AppType = NormalizeAppType(model.AppType, null);
        var baseUrl = (model.BaseUrl ?? "").Trim();
        model.BaseUrl = baseUrl.Length == 0 ? null : (baseUrl.Length <= 300 ? baseUrl : baseUrl[..300]);
        var icon = (model.Icon ?? "").Trim();
        model.Icon = icon.Length == 0 ? null : (icon.Length <= 100 ? icon : icon[..100]);
        model.BStatus = NormalizeBStatus(model.BStatus);
        model.Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark.Trim();
        if (!isEdit && string.IsNullOrWhiteSpace(model.AppCode))
            throw new InvalidOperationException("应用编码不能为空。");
    }

    public async Task<(IReadOnlyList<EAppModuleManageListRowVm> pageRows, int total, int totalPages, int page)> GetIndexPageAsync(
        string? appType1,
        string? bStatus1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EAppModules.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(appType1))
            rows = rows.Where(x => string.Equals(x.AppType, appType1.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(bStatus1))
        {
            rows = bStatus1 switch
            {
                "1" => rows.Where(x => EBStatusHelper.IsActiveBStatus(x.BStatus)).ToList(),
                "2" => rows.Where(x => !EBStatusHelper.IsActiveBStatus(x.BStatus)).ToList(),
                _ => rows
            };
        }

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            rows = (searchField ?? "AppCode") switch
            {
                "AppName" => rows.Where(x => (x.AppName ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "BaseUrl" => rows.Where(x => (x.BaseUrl ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows.Where(x => (x.AppCode ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList()
            };
        }

        rows = SortRows(rows, sortField, sortArrow);
        var appTypeMap = await GetAppTypeMapAsync(ct);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EAppModuleManageListRowVm
        {
            DataId = x.DataId,
            AppCode = x.AppCode,
            AppName = x.AppName,
            AppType = x.AppType,
            AppTypeText = ToAppTypeText(x.AppType, appTypeMap),
            DispSeq = x.DispSeq,
            BStatusText = ToStatusDisplay(x.BStatus),
            AmendDate = x.AmendDate,
            OperatorName = x.OperatorName ?? "",
            IsCoreApp = ProtectedAppCodes.Contains(x.AppCode)
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<EAppModule?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.EAppModules.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);

    public async Task<IEnumerable<(string Key, string Message)>> GetSaveValidationErrorsAsync(
        EAppModuleFormVm model, bool isEdit, int? originalId, CancellationToken ct)
    {
        var list = new List<(string, string)>();
        if (string.IsNullOrWhiteSpace(model.AppCode))
            list.Add((nameof(model.AppCode), "应用编码不能为空。"));

        var typeMap = await GetAppTypeMapAsync(ct);
        var appType = NormalizeAppType(model.AppType, typeMap);
        if (!typeMap.ContainsKey(appType))
            list.Add((nameof(model.AppType), "应用类型无效。"));

        var code = model.AppCode.Trim();
        if (!isEdit || originalId == null)
        {
            if (await _db.EAppModules.AsNoTracking().AnyAsync(x => x.AppCode == code, ct))
                list.Add((nameof(model.AppCode), "应用编码已存在。"));
        }
        else
        {
            if (await _db.EAppModules.AsNoTracking()
                    .AnyAsync(x => x.AppCode == code && x.DataId != originalId.Value, ct))
                list.Add((nameof(model.AppCode), "应用编码已存在。"));
        }

        return list;
    }

    public async Task CreateAsync(EAppModuleFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        _db.EAppModules.Add(new EAppModule
        {
            AppCode = model.AppCode,
            AppName = model.AppName,
            AppType = model.AppType,
            BaseUrl = model.BaseUrl,
            Icon = model.Icon,
            DispSeq = model.DispSeq,
            Remark = model.Remark,
            BStatus = model.BStatus,
            IsDeleted = false,
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        });
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("应用编码已存在，请换一个。", ex);
        }
    }

    public async Task<bool> TryUpdateAsync(int id, EAppModuleFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.EAppModules.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return false;

        row.AppName = model.AppName;
        row.AppType = model.AppType;
        row.BaseUrl = model.BaseUrl;
        row.Icon = model.Icon;
        row.DispSeq = model.DispSeq;
        row.BStatus = model.BStatus;
        row.Remark = model.Remark;
        row.AmendDate = DateTime.Now;
        row.OperatorName = operatorId;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.EAppModules.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return;
        if (ProtectedAppCodes.Contains(row.AppCode))
            throw new InvalidOperationException("核心应用 FRAME 不可删除。");

        var inUse = await _db.EEventConfigs.AsNoTracking().AnyAsync(x => x.AppCode == row.AppCode && !x.IsDeleted, ct)
                    || await _db.EResources.AsNoTracking().AnyAsync(x => x.AppCode == row.AppCode && !x.IsDeleted, ct)
                    || await _db.EMenuGroups.AsNoTracking().AnyAsync(x => x.AppCode == row.AppCode && !x.IsDeleted, ct);
        if (inUse)
            throw new InvalidOperationException("该应用已被事件配置、资源或菜单组引用，无法删除。");

        row.IsDeleted = true;
        row.BStatus = "2";
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        foreach (var id in ids)
        {
            try { await DeleteAsync(id, ct); }
            catch (InvalidOperationException) { }
        }
    }

    private static List<EAppModule> SortRows(List<EAppModule> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        return field switch
        {
            "AppCode" => desc ? rows.OrderByDescending(x => x.AppCode).ToList() : rows.OrderBy(x => x.AppCode).ToList(),
            "AppName" => desc ? rows.OrderByDescending(x => x.AppName).ToList() : rows.OrderBy(x => x.AppName).ToList(),
            "AppType" => desc ? rows.OrderByDescending(x => x.AppType).ToList() : rows.OrderBy(x => x.AppType).ToList(),
            "DispSeq" => desc ? rows.OrderByDescending(x => x.DispSeq).ToList() : rows.OrderBy(x => x.DispSeq).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            "AmendDate" => desc ? rows.OrderByDescending(x => x.AmendDate).ToList() : rows.OrderBy(x => x.AmendDate).ToList(),
            "OperatorName" => desc ? rows.OrderByDescending(x => x.OperatorName).ToList() : rows.OrderBy(x => x.OperatorName).ToList(),
            _ => rows.OrderBy(x => x.DispSeq).ThenBy(x => x.AppCode).ToList()
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        for (var e = ex.InnerException; e != null; e = e.InnerException)
            if (e is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
                return true;
        return false;
    }
}
