using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public sealed class EDictItemService
{
    private readonly FrameworkDbContext _db;

    public EDictItemService(FrameworkDbContext db) => _db = db;

    public string Normalize(string? value, int maxLen, string fallback = "")
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) v = fallback;
        return v.Length <= maxLen ? v : v[..maxLen];
    }

    public static List<(string Value, string Text)> BuildBStatusFilterOptions() => EDictTypeService.BuildBStatusFilterOptions();
    public static List<(string Value, string Text)> BuildBStatusFormOptions() => EDictTypeService.BuildBStatusFormOptions();
    public static string NormalizeBStatus(string? value) => EDictTypeService.NormalizeBStatus(value);
    public static string ToStatusDisplay(string? value) => EDictTypeService.ToStatusDisplay(value);

    public EDictItemFormVm ToForm(EDictItem row) => new()
    {
        DataId = row.DataId,
        DictTypeCode = row.DictTypeCode,
        ItemCode = row.ItemCode,
        ItemName = row.ItemName,
        ItemNameEn = row.ItemNameEn,
        ParentItemCode = row.ParentItemCode,
        DispSeq = row.DispSeq < 0 ? 0 : row.DispSeq,
        ExtJson = row.ExtJson,
        BStatus = NormalizeBStatus(row.BStatus),
        Remark = row.Remark
    };

    public void NormalizeFormForSave(EDictItemFormVm model, bool isEdit)
    {
        model.DictTypeCode = Normalize(model.DictTypeCode, 50);
        model.ItemCode = Normalize(model.ItemCode, 50).ToUpperInvariant();
        model.ItemName = Normalize(model.ItemName, 100);
        var en = (model.ItemNameEn ?? "").Trim();
        model.ItemNameEn = en.Length == 0 ? null : (en.Length <= 100 ? en : en[..100]);
        var parent = (model.ParentItemCode ?? "").Trim();
        model.ParentItemCode = parent.Length == 0 ? null : (parent.Length <= 50 ? parent.ToUpperInvariant() : parent[..50].ToUpperInvariant());
        var ext = (model.ExtJson ?? "").Trim();
        model.ExtJson = ext.Length == 0 ? null : (ext.Length <= 500 ? ext : ext[..500]);
        model.BStatus = NormalizeBStatus(model.BStatus);
        model.Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark.Trim();
        if (!isEdit && string.IsNullOrWhiteSpace(model.ItemCode))
            throw new InvalidOperationException("条目编码不能为空。");
    }

    public async Task<EDictType?> GetTypeAsync(string dictTypeCode, CancellationToken ct)
    {
        var code = (dictTypeCode ?? "").Trim();
        return await _db.EDictTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.DictTypeCode == code && !t.IsDeleted, ct);
    }

    public async Task<(IReadOnlyList<EDictItemManageListRowVm> pageRows, int total, int totalPages, int page)> GetIndexPageAsync(
        string dictTypeCode,
        string? bStatus1,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var code = (dictTypeCode ?? "").Trim();
        var rows = await _db.EDictItems.AsNoTracking()
            .Where(i => i.DictTypeCode == code && !i.IsDeleted)
            .ToListAsync(ct);

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
            rows = rows.Where(x =>
                (x.ItemCode ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)
                || (x.ItemName ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();

        rows = SortRows(rows, sortField, sortArrow);
        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EDictItemManageListRowVm
        {
            DataId = x.DataId,
            ItemCode = x.ItemCode,
            ItemName = x.ItemName,
            ItemNameEn = x.ItemNameEn,
            ParentItemCode = x.ParentItemCode,
            DispSeq = x.DispSeq,
            BStatusText = ToStatusDisplay(x.BStatus),
            IsSystem = x.IsSystem,
            AmendDate = x.AmendDate
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<EDictItem?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.EDictItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);

    public async Task<IEnumerable<(string Key, string Message)>> GetSaveValidationErrorsAsync(
        EDictItemFormVm model, bool isEdit, int? originalId, CancellationToken ct)
    {
        var list = new List<(string, string)>();
        var type = await GetTypeAsync(model.DictTypeCode, ct);
        if (type == null)
            list.Add((nameof(model.DictTypeCode), "字典类型不存在。"));
        else if (!type.IsEditable && !isEdit)
            list.Add((nameof(model.DictTypeCode), "该字典类型不可新增条目。"));

        if (string.IsNullOrWhiteSpace(model.ItemCode))
            list.Add((nameof(model.ItemCode), "条目编码不能为空。"));
        else if (!isEdit || originalId == null)
        {
            var exists = await _db.EDictItems.AsNoTracking()
                .AnyAsync(x => x.DictTypeCode == model.DictTypeCode && x.ItemCode == model.ItemCode && !x.IsDeleted, ct);
            if (exists)
                list.Add((nameof(model.ItemCode), "该类型下条目编码已存在。"));
        }
        else
        {
            var exists = await _db.EDictItems.AsNoTracking()
                .AnyAsync(x => x.DictTypeCode == model.DictTypeCode && x.ItemCode == model.ItemCode
                               && x.DataId != originalId.Value && !x.IsDeleted, ct);
            if (exists)
                list.Add((nameof(model.ItemCode), "该类型下条目编码已存在。"));
        }

        return list;
    }

    public async Task CreateAsync(EDictItemFormVm model, CancellationToken ct)
    {
        var type = await _db.EDictTypes.FirstOrDefaultAsync(t => t.DictTypeCode == model.DictTypeCode && !t.IsDeleted, ct)
                   ?? throw new InvalidOperationException("字典类型不存在。");
        if (!type.IsEditable)
            throw new InvalidOperationException("该字典类型不可新增条目。");

        var now = DateTime.Now;
        _db.EDictItems.Add(new EDictItem
        {
            DictTypeCode = model.DictTypeCode,
            ItemCode = model.ItemCode,
            ItemName = model.ItemName,
            ItemNameEn = model.ItemNameEn,
            ParentItemCode = model.ParentItemCode,
            DispSeq = model.DispSeq,
            ExtJson = model.ExtJson,
            IsSystem = false,
            Remark = model.Remark,
            BStatus = model.BStatus,
            IsDeleted = false,
            CreateDate = now,
            AmendDate = now
        });
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("该类型下条目编码已存在。", ex);
        }
    }

    public async Task<bool> TryUpdateAsync(int id, EDictItemFormVm model, CancellationToken ct)
    {
        var row = await _db.EDictItems.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return false;
        if (row.IsSystem)
            throw new InvalidOperationException("系统内置字典条目不可修改。");

        var type = await _db.EDictTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.DictTypeCode == row.DictTypeCode && !t.IsDeleted, ct);
        if (type is { IsEditable: false })
            throw new InvalidOperationException("该字典类型已锁定，不可修改条目。");

        row.ItemName = model.ItemName;
        row.ItemNameEn = model.ItemNameEn;
        row.ParentItemCode = model.ParentItemCode;
        row.DispSeq = model.DispSeq;
        row.ExtJson = model.ExtJson;
        row.BStatus = model.BStatus;
        row.Remark = model.Remark;
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.EDictItems.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return;
        if (row.IsSystem)
            throw new InvalidOperationException("系统内置字典条目不可删除。");
        row.IsDeleted = true;
        row.BStatus = "2";
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return;
        var rows = await _db.EDictItems.Where(x => ids.Contains(x.DataId) && !x.IsDeleted).ToListAsync(ct);
        foreach (var row in rows)
        {
            if (row.IsSystem) continue;
            row.IsDeleted = true;
            row.BStatus = "2";
            row.AmendDate = DateTime.Now;
        }
        if (rows.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    private static List<EDictItem> SortRows(List<EDictItem> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        return field switch
        {
            "ItemCode" => desc ? rows.OrderByDescending(x => x.ItemCode).ToList() : rows.OrderBy(x => x.ItemCode).ToList(),
            "ItemName" => desc ? rows.OrderByDescending(x => x.ItemName).ToList() : rows.OrderBy(x => x.ItemName).ToList(),
            "DispSeq" => desc ? rows.OrderByDescending(x => x.DispSeq).ToList() : rows.OrderBy(x => x.DispSeq).ToList(),
            "AmendDate" => desc ? rows.OrderByDescending(x => x.AmendDate).ToList() : rows.OrderBy(x => x.AmendDate).ToList(),
            _ => rows.OrderBy(x => x.DispSeq).ThenBy(x => x.ItemCode).ToList()
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
