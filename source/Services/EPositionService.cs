using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class EPositionService
{
    private readonly FrameworkDbContext _db;
    private readonly DictService _dict;

    public EPositionService(FrameworkDbContext db, DictService dict)
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

    /// <summary>v1 数据范围编码（与字典 DATA_SCOPE 一致）。</summary>
    public static IReadOnlyDictionary<string, string> DataScopeMap { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["SELF"] = "仅本人",
        ["DEPT"] = "本部门",
        ["DEPT_TREE"] = "本部门及下级",
        ["ALL"] = "全部",
        ["CUSTOM"] = "自定义"
    };

    private static List<(string Value, string Text)> FallbackDataScopeFormOptions() =>
        DataScopeMap.Select(x => (x.Key, x.Value)).ToList();

    public Task<List<(string Value, string Text)>> GetDataScopeFilterOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.DataScope, forFilter: true, formEmptyLabel: null,
            FallbackDataScopeFormOptions(), ct: ct);

    public Task<List<(string Value, string Text)>> GetDataScopeFormOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.DataScope, forFilter: false, formEmptyLabel: null,
            FallbackDataScopeFormOptions(), ct: ct);

    public Task<Dictionary<string, string>> GetDataScopeMapAsync(CancellationToken ct) =>
        _dict.GetItemMapMergedAsync(DictCodes.DataScope, DataScopeMap, ct: ct);

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

    public string NormalizeBStatus(string? value) => EBStatusHelper.NormalizeBStatusForSave(value);

    public string ToStatusDisplay(string? value)
    {
        var v = (value ?? "").Trim();
        return v switch
        {
            "1" or "启用" => "启用",
            "2" or "停用" => "停用",
            _ => v
        };
    }

    public static string NormalizeDataScope(string? value)
    {
        var v = (value ?? "").Trim().ToUpperInvariant();
        if (DataScopeMap.ContainsKey(v)) return v;
        return v switch
        {
            "1" => "SELF",
            "2" => "DEPT",
            "3" => "ALL",
            _ => string.IsNullOrEmpty(v) ? "SELF" : v
        };
    }

    public string ToDataScopeDisplay(string? code, IReadOnlyDictionary<string, string> scopeMap)
    {
        var key = NormalizeDataScope(code);
        return scopeMap.TryGetValue(key, out var t) ? t : key;
    }

    public EPositionFormVm ToForm(EPosition row)
    {
        return new EPositionFormVm
        {
            DataId = row.DataId,
            PostCode = row.PostCode ?? "",
            PostCName = row.PostCName ?? "",
            PostEName = row.PostEName ?? "",
            PositionType = row.PositionType,
            DataScope = NormalizeDataScope(row.DataScope),
            DispSeq = row.DispSeq < 0 ? 0 : row.DispSeq,
            BStatus = NormalizeBStatus(row.BStatus),
            DDescription = row.DDescription ?? "",
            Remark = row.Remark ?? ""
        };
    }

    public void NormalizeFormForSave(EPositionFormVm model)
    {
        model.PostCode = Normalize(model.PostCode, 30);
        model.PostCName = Normalize(model.PostCName, 100);
        var en = (model.PostEName ?? "").Trim();
        model.PostEName = en.Length == 0 ? null : (en.Length <= 100 ? en : en[..100]);
        model.PositionType = string.IsNullOrWhiteSpace(model.PositionType)
            ? null
            : Normalize(model.PositionType, 30);
        model.DataScope = NormalizeDataScope(model.DataScope);
        model.BStatus = NormalizeBStatus(model.BStatus);
        model.DDescription = (model.DDescription ?? "").Trim();
        model.Remark = (model.Remark ?? "").Trim();
    }

    public async Task<(IReadOnlyList<EPositionListRowVm> pageRows, int totalRecords, int totalPages, int page)> GetIndexPageAsync(
        string? bStatus1,
        string? dataScope1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EPositions.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(ct);

        rows = rows.Where(x => MatchBStatusFilter(x.BStatus, bStatus1) && MatchDataScopeFilter(x.DataScope, dataScope1)).ToList();

        var sc = (searchContent ?? "").Trim();
        if (sc.Length > 0)
        {
            rows = (searchField ?? "PostCode") switch
            {
                "PostCode" => rows.Where(x => (x.PostCode ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "PostCName" => rows.Where(x => (x.PostCName ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "PostEName" => rows.Where(x => (x.PostEName ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow);

        var scopeMap = await GetDataScopeMapAsync(ct);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EPositionListRowVm
        {
            DataId = x.DataId,
            PostCode = x.PostCode ?? "",
            PostCName = x.PostCName ?? "",
            DataScopeText = ToDataScopeDisplay(x.DataScope, scopeMap),
            DispSeq = x.DispSeq,
            BStatus = ToStatusDisplay(x.BStatus)
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(EPositionFormVm model, bool isEdit, CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();
        var scopeMap = await GetDataScopeMapAsync(ct);
        if (!scopeMap.ContainsKey(NormalizeDataScope(model.DataScope)))
            errors.Add((nameof(EPositionFormVm.DataScope), "数据范围无效。"));

        var dup = await _db.EPositions.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.PostCode == model.PostCode && (!isEdit || x.DataId != model.DataId), ct);
        if (dup)
            errors.Add((nameof(EPositionFormVm.PostCode), "岗位代码已存在，请换一个。"));

        return errors;
    }

    public async Task CreateAsync(EPositionFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var entity = new EPosition
        {
            PostCode = model.PostCode,
            PostCName = model.PostCName,
            PostEName = model.PostEName,
            PositionType = model.PositionType,
            DataScope = NormalizeDataScope(model.DataScope),
            DispSeq = model.DispSeq,
            BStatus = NormalizeBStatus(model.BStatus),
            DDescription = string.IsNullOrWhiteSpace(model.DDescription) ? null : model.DDescription,
            Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark,
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        };
        _db.EPositions.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("岗位代码已存在，请换一个。", ex);
        }
    }

    public async Task<bool> TryUpdateAsync(int id, EPositionFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.EPositions.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return false;

        var bStatus = NormalizeBStatus(model.BStatus);
        var scope = NormalizeDataScope(model.DataScope);
        var changed = false;
        if (!string.Equals((row.PostCode ?? "").Trim(), model.PostCode, StringComparison.Ordinal)) { row.PostCode = model.PostCode; changed = true; }
        if (!string.Equals((row.PostCName ?? "").Trim(), model.PostCName, StringComparison.Ordinal)) { row.PostCName = model.PostCName; changed = true; }
        if (!string.Equals((row.PostEName ?? "").Trim(), (model.PostEName ?? "").Trim(), StringComparison.Ordinal)) { row.PostEName = model.PostEName; changed = true; }
        if (!string.Equals((row.PositionType ?? "").Trim(), (model.PositionType ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) { row.PositionType = model.PositionType; changed = true; }
        if (!string.Equals(NormalizeDataScope(row.DataScope), scope, StringComparison.OrdinalIgnoreCase)) { row.DataScope = scope; changed = true; }
        if (row.DispSeq != model.DispSeq) { row.DispSeq = model.DispSeq; changed = true; }
        if (!string.Equals((row.BStatus ?? "").Trim(), bStatus, StringComparison.Ordinal)) { row.BStatus = bStatus; changed = true; }
        if (!string.Equals((row.DDescription ?? "").Trim(), (model.DDescription ?? "").Trim(), StringComparison.Ordinal)) { row.DDescription = model.DDescription; changed = true; }
        if (!string.Equals((row.Remark ?? "").Trim(), (model.Remark ?? "").Trim(), StringComparison.Ordinal)) { row.Remark = model.Remark; changed = true; }
        if (!changed) return true;

        row.AmendDate = DateTime.Now;
        row.OperatorName = operatorId;
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("岗位代码已存在，请换一个。", ex);
        }
        return true;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.EPositions.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return;
        row.IsDeleted = true;
        row.BStatus = "2";
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return;
        var rows = await _db.EPositions.Where(x => ids.Contains(x.DataId) && !x.IsDeleted).ToListAsync(ct);
        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.BStatus = "2";
            row.AmendDate = DateTime.Now;
        }
        if (rows.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    public async Task<EPosition?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.EPositions.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);

    private static List<EPosition> SortRows(List<EPosition> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        return field switch
        {
            "PostCode" => desc ? rows.OrderByDescending(x => x.PostCode).ToList() : rows.OrderBy(x => x.PostCode).ToList(),
            "PostCName" => desc ? rows.OrderByDescending(x => x.PostCName).ToList() : rows.OrderBy(x => x.PostCName).ToList(),
            "DispSeq" => desc ? rows.OrderByDescending(x => x.DispSeq).ToList() : rows.OrderBy(x => x.DispSeq).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            "DataScope" => desc ? rows.OrderByDescending(x => x.DataScope).ToList() : rows.OrderBy(x => x.DataScope).ToList(),
            _ => rows.OrderByDescending(x => x.DataId).ToList()
        };
    }

    private static bool MatchBStatusFilter(string? value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        var v = (value ?? "").Trim();
        return selected switch
        {
            "1" => v == "1" || v == "启用",
            "2" => v == "2" || v == "停用",
            _ => true
        };
    }

    private static bool MatchDataScopeFilter(string? value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        return string.Equals(NormalizeDataScope(value), NormalizeDataScope(selected), StringComparison.OrdinalIgnoreCase);
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
