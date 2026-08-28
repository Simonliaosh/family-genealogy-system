using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public sealed class EDictTypeService
{
    private readonly FrameworkDbContext _db;

    public EDictTypeService(FrameworkDbContext db) => _db = db;

    public string Normalize(string? value, int maxLen, string fallback = "")
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) v = fallback;
        return v.Length <= maxLen ? v : v[..maxLen];
    }

    public static List<(string Value, string Text)> BuildAppCodeFormOptions() =>
    [
        ("FRAME", "FRAME 框架"),
        ("FamilyTree", "FamilyTree 族谱"),
        ("CRM", "CRM"),
        ("OA", "OA")
    ];

    public static List<(string Value, string Text)> BuildAppCodeFilterOptions()
    {
        var list = new List<(string, string)> { ("", "全部") };
        list.AddRange(BuildAppCodeFormOptions());
        return list;
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

    public static string NormalizeBStatus(string? value) => EBStatusHelper.NormalizeBStatusForSave(value);

    public static string ToStatusDisplay(string? value) => EDictQueryService.ToStatusDisplay(value);

    public EDictTypeFormVm ToForm(EDictType row) => new()
    {
        AppCode = string.IsNullOrWhiteSpace(row.AppCode) ? "FRAME" : row.AppCode.Trim(),
        DictTypeCode = row.DictTypeCode,
        DictTypeName = row.DictTypeName,
        IsEditable = row.IsEditable,
        BStatus = NormalizeBStatus(row.BStatus),
        Remark = row.Remark
    };

    public void NormalizeFormForSave(EDictTypeFormVm model, bool isEdit)
    {
        model.AppCode = Normalize(model.AppCode, 50, "FRAME");
        model.DictTypeCode = Normalize(model.DictTypeCode, 50).ToUpperInvariant();
        model.DictTypeName = Normalize(model.DictTypeName, 100);
        model.BStatus = NormalizeBStatus(model.BStatus);
        model.Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark.Trim();
        if (!isEdit && string.IsNullOrWhiteSpace(model.DictTypeCode))
            throw new InvalidOperationException("字典类型编码不能为空。");
    }

    public async Task<(IReadOnlyList<EDictTypeManageListRowVm> pageRows, int total, int totalPages, int page)> GetIndexPageAsync(
        string? appCode1,
        string? bStatus1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EDictTypes.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(appCode1))
            rows = rows.Where(x => string.Equals(x.AppCode, appCode1.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

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
            rows = (searchField ?? "DictTypeCode") switch
            {
                "DictTypeName" => rows.Where(x => (x.DictTypeName ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "Remark" => rows.Where(x => (x.Remark ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows.Where(x => (x.DictTypeCode ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList()
            };
        }

        var itemCounts = await _db.EDictItems.AsNoTracking()
            .Where(i => !i.IsDeleted)
            .GroupBy(i => i.DictTypeCode)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var countMap = itemCounts.ToDictionary(x => x.Key, x => x.Count, StringComparer.OrdinalIgnoreCase);

        rows = SortRows(rows, sortField, sortArrow, countMap);
        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EDictTypeManageListRowVm
        {
            DictTypeCode = x.DictTypeCode,
            DictTypeName = x.DictTypeName,
            AppCode = x.AppCode,
            BStatusText = ToStatusDisplay(x.BStatus),
            IsSystem = x.IsSystem,
            IsEditable = x.IsEditable,
            ItemCount = countMap.TryGetValue(x.DictTypeCode, out var c) ? c : 0,
            AmendDate = x.AmendDate
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<EDictType?> GetByCodeNoTrackAsync(string code, CancellationToken ct) =>
        await _db.EDictTypes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DictTypeCode == code && !x.IsDeleted, ct);

    public async Task<IEnumerable<(string Key, string Message)>> GetSaveValidationErrorsAsync(
        EDictTypeFormVm model, bool isEdit, string? originalCode, CancellationToken ct)
    {
        var list = new List<(string, string)>();
        if (string.IsNullOrWhiteSpace(model.DictTypeCode))
            list.Add((nameof(model.DictTypeCode), "字典类型编码不能为空。"));

        var code = model.DictTypeCode.Trim();
        if (!isEdit || !string.Equals(originalCode, code, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _db.EDictTypes.AsNoTracking()
                .AnyAsync(x => x.DictTypeCode == code && !x.IsDeleted, ct);
            if (exists)
                list.Add((nameof(model.DictTypeCode), "字典类型编码已存在。"));
        }

        return list;
    }

    public async Task CreateAsync(EDictTypeFormVm model, CancellationToken ct)
    {
        var now = DateTime.Now;
        var row = new EDictType
        {
            DictTypeCode = model.DictTypeCode,
            DictTypeName = model.DictTypeName,
            AppCode = model.AppCode,
            IsSystem = false,
            IsEditable = model.IsEditable,
            Remark = model.Remark,
            BStatus = model.BStatus,
            IsDeleted = false,
            CreateDate = now,
            AmendDate = now
        };
        _db.EDictTypes.Add(row);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("字典类型编码已存在，请换一个。", ex);
        }
    }

    public async Task<bool> TryUpdateAsync(string code, EDictTypeFormVm model, CancellationToken ct)
    {
        var row = await _db.EDictTypes.FirstOrDefaultAsync(x => x.DictTypeCode == code && !x.IsDeleted, ct);
        if (row == null) return false;
        if (row.IsSystem)
            throw new InvalidOperationException("系统内置字典类型不可修改。");
        if (!row.IsEditable)
            throw new InvalidOperationException("该字典类型已锁定为不可编辑。");

        row.DictTypeName = model.DictTypeName;
        row.IsEditable = model.IsEditable;
        row.BStatus = model.BStatus;
        row.Remark = model.Remark;
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task DeleteAsync(string code, CancellationToken ct)
    {
        var row = await _db.EDictTypes.FirstOrDefaultAsync(x => x.DictTypeCode == code && !x.IsDeleted, ct);
        if (row == null) return;
        if (row.IsSystem)
            throw new InvalidOperationException("系统内置字典类型不可删除。");

        row.IsDeleted = true;
        row.BStatus = "2";
        row.AmendDate = DateTime.Now;

        var items = await _db.EDictItems.Where(x => x.DictTypeCode == code && !x.IsDeleted).ToListAsync(ct);
        foreach (var item in items)
        {
            if (item.IsSystem) continue;
            item.IsDeleted = true;
            item.BStatus = "2";
            item.AmendDate = DateTime.Now;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(IReadOnlyList<string> codes, CancellationToken ct)
    {
        foreach (var code in codes)
        {
            try { await DeleteAsync(code, ct); }
            catch (InvalidOperationException) { /* 跳过系统类型 */ }
        }
    }

    private static List<EDictType> SortRows(
        List<EDictType> rows,
        string field,
        string arrow,
        IReadOnlyDictionary<string, int> countMap)
    {
        var desc = arrow == "1";
        return field switch
        {
            "DictTypeCode" => desc ? rows.OrderByDescending(x => x.DictTypeCode).ToList() : rows.OrderBy(x => x.DictTypeCode).ToList(),
            "DictTypeName" => desc ? rows.OrderByDescending(x => x.DictTypeName).ToList() : rows.OrderBy(x => x.DictTypeName).ToList(),
            "AppCode" => desc ? rows.OrderByDescending(x => x.AppCode).ToList() : rows.OrderBy(x => x.AppCode).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            "IsSystem" => desc ? rows.OrderByDescending(x => x.IsSystem).ToList() : rows.OrderBy(x => x.IsSystem).ToList(),
            "IsEditable" => desc ? rows.OrderByDescending(x => x.IsEditable).ToList() : rows.OrderBy(x => x.IsEditable).ToList(),
            "ItemCount" => desc
                ? rows.OrderByDescending(x => countMap.TryGetValue(x.DictTypeCode, out var c) ? c : 0).ToList()
                : rows.OrderBy(x => countMap.TryGetValue(x.DictTypeCode, out var c) ? c : 0).ToList(),
            "AmendDate" => desc ? rows.OrderByDescending(x => x.AmendDate).ToList() : rows.OrderBy(x => x.AmendDate).ToList(),
            _ => rows.OrderBy(x => x.AppCode).ThenBy(x => x.DictTypeCode).ToList()
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
