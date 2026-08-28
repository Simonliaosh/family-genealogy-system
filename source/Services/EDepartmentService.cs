using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class EDepartmentService
{
    private readonly FrameworkDbContext _db;

    public EDepartmentService(FrameworkDbContext db)
    {
        _db = db;
    }

    public string Normalize(string? value, int maxLen, string fallback = "")
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) v = fallback;
        return v.Length <= maxLen ? v : v[..maxLen];
    }

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

    public EDepartmentFormVm ToForm(EDepartment row)
    {
        return new EDepartmentFormVm
        {
            DataId = row.DataId,
            DeptCode = row.DeptCode ?? "",
            DeptCName = row.DeptCName ?? "",
            DeptEName = row.DeptEName ?? "",
            ParentDeptId = row.ParentDeptId,
            DeptLevel = row.DeptLevel <= 0 ? 1 : row.DeptLevel,
            DispSeq = row.DispSeq < 0 ? 0 : row.DispSeq,
            BStatus = NormalizeBStatus(row.BStatus),
            DDescription = row.DDescription ?? "",
            Remark = row.Remark ?? ""
        };
    }

    public async Task<List<(string Value, string Text)>> GetParentDeptOptionsAsync(int? excludeDataId, CancellationToken ct)
    {
        var rows = await _db.EDepartments.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => !excludeDataId.HasValue || x.DataId != excludeDataId.Value)
            .OrderBy(x => x.DeptCode)
            .Select(x => new { x.DataId, x.DeptCode, x.DeptCName })
            .ToListAsync(ct);

        var list = new List<(string Value, string Text)> { ("", "全部") };
        list.AddRange(rows.Select(x => (x.DataId.ToString(), $"{x.DeptCode} - {x.DeptCName}")));
        return list;
    }

    public static List<(string Value, string Text)> BuildBStatusFilterOptions() =>
    [
        ("", "全部"),
        ("1", "启用"),
        ("2", "停用")
    ];

    public static List<(string Value, string Text)> BuildDeptLevelFilterOptions() =>
    [
        ("", "全部"),
        ("1", "一级"),
        ("2", "二级"),
        ("3", "三级"),
        ("4", "四级"),
        ("5", "五级")
    ];

    public static List<(string Value, string Text)> BuildParentDeptFilterOptions(IReadOnlyList<EDepartment> rows)
    {
        var list = new List<(string Value, string Text)> { ("", "全部") };
        foreach (var x in rows.OrderBy(r => r.DeptCode))
            list.Add((x.DataId.ToString(), $"{x.DeptCode} - {x.DeptCName}"));
        return list;
    }

    /// <summary>
    /// 列表数据：EF Core 生成的 SQL 使用参数绑定；筛选排序在内存中完成（部门表数据量通常可控）。
    /// </summary>
    public async Task<(
        IReadOnlyList<EDepartmentListRowVm> pageRows,
        int totalRecords,
        int totalPages,
        int page,
        List<(string Value, string Text)> parentFilterOptions)> GetIndexPageAsync(
        string? bStatus1,
        string? deptLevel1,
        string? parentDeptId1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var allRows = await _db.EDepartments.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(ct);
        var parentFilterOptions = BuildParentDeptFilterOptions(allRows);
        var rows = allRows.ToList();
        var byId = rows.ToDictionary(x => x.DataId, x => x, EqualityComparer<int>.Default);

        rows = rows.Where(x =>
                MatchBStatusFilter(x.BStatus, bStatus1)
                && MatchIntFilter(x.DeptLevel, deptLevel1)
                && MatchNullableIntFilter(x.ParentDeptId, parentDeptId1))
            .ToList();

        var sc = (searchContent ?? "").Trim();
        if (sc.Length > 0)
        {
            rows = (searchField ?? "DeptCode") switch
            {
                "DeptCode" => rows.Where(x => (x.DeptCode ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "DeptCName" => rows.Where(x => (x.DeptCName ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "DeptEName" => rows.Where(x => (x.DeptEName ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow);
        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        string ParentLabel(int? parentId)
        {
            if (!parentId.HasValue || !byId.TryGetValue(parentId.Value, out var p)) return "-";
            return $"{p.DeptCode} - {p.DeptCName}";
        }

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EDepartmentListRowVm
        {
            DataId = x.DataId,
            DeptCode = x.DeptCode ?? "",
            DeptCName = x.DeptCName ?? "",
            ParentDeptId = x.ParentDeptId,
            ParentDeptName = ParentLabel(x.ParentDeptId),
            BStatus = ToStatusDisplay(x.BStatus)
        }).ToList();

        return (pageRows, total, totalPages, page, parentFilterOptions);
    }

    public void NormalizeFormForSave(EDepartmentFormVm model, bool isEdit, string? lockedDeptCode)
    {
        if (!isEdit)
            model.DeptCode = Normalize(model.DeptCode, 30);
        else if (!string.IsNullOrEmpty(lockedDeptCode))
            model.DeptCode = lockedDeptCode;

        model.DeptCName = Normalize(model.DeptCName, 100);
        var en = (model.DeptEName ?? "").Trim();
        model.DeptEName = en.Length == 0 ? null : (en.Length <= 100 ? en : en[..100]);
        model.DDescription = (model.DDescription ?? "").Trim();
        model.Remark = (model.Remark ?? "").Trim();
    }

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(
        EDepartmentFormVm model,
        bool isEdit,
        CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();
        var dup = await _db.EDepartments.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.DeptCode == model.DeptCode && (!isEdit || x.DataId != model.DataId), ct);
        if (dup)
            errors.Add((nameof(EDepartmentFormVm.DeptCode), "部门代码已存在，请换一个。"));

        if (model.ParentDeptId.HasValue)
        {
            if (isEdit && model.ParentDeptId.Value == model.DataId)
                errors.Add((nameof(EDepartmentFormVm.ParentDeptId), "上级部门不能选择自己。"));
            var exists = await _db.EDepartments.AsNoTracking()
                .AnyAsync(x => x.DataId == model.ParentDeptId.Value && !x.IsDeleted, ct);
            if (!exists)
                errors.Add((nameof(EDepartmentFormVm.ParentDeptId), "选择的上级部门不存在。"));
        }

        return errors;
    }

    /// <summary>EF Core SaveChanges 使用参数化命令；唯一约束冲突时给出友好提示。</summary>
    public async Task CreateAsync(EDepartmentFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var entity = new EDepartment
        {
            DeptCode = model.DeptCode,
            DeptCName = model.DeptCName,
            DeptEName = model.DeptEName,
            ParentDeptId = model.ParentDeptId,
            DeptLevel = model.DeptLevel,
            DispSeq = model.DispSeq,
            BStatus = NormalizeBStatus(model.BStatus),
            DDescription = string.IsNullOrEmpty(model.DDescription) ? null : model.DDescription,
            Remark = string.IsNullOrEmpty(model.Remark) ? null : model.Remark,
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        };
        _db.EDepartments.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
            entity.DeptPath = await BuildDeptPathAsync(entity.DataId, entity.ParentDeptId, ct);
            entity.DeptLevel = await ResolveDeptLevelAsync(entity.ParentDeptId, model.DeptLevel, ct);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("部门代码已存在，请换一个。", ex);
        }
    }

    public async Task<bool> TryUpdateAsync(int id, EDepartmentFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.EDepartments.FirstOrDefaultAsync(x => x.DataId == id, ct);
        if (row == null) return false;

        var bStatus = NormalizeBStatus(model.BStatus);
        var dDescription = model.DDescription ?? "";
        var remark = model.Remark ?? "";

        var changed = false;
        if (!string.Equals((row.DeptCName ?? "").Trim(), model.DeptCName, StringComparison.Ordinal)) { row.DeptCName = model.DeptCName; changed = true; }
        if (!string.Equals((row.DeptEName ?? "").Trim(), (model.DeptEName ?? "").Trim(), StringComparison.Ordinal)) { row.DeptEName = model.DeptEName; changed = true; }
        if (!string.Equals((row.BStatus ?? "").Trim(), bStatus, StringComparison.Ordinal)) { row.BStatus = bStatus; changed = true; }
        if (!string.Equals((row.DDescription ?? "").Trim(), dDescription, StringComparison.Ordinal)) { row.DDescription = dDescription; changed = true; }
        if (!string.Equals((row.Remark ?? "").Trim(), remark, StringComparison.Ordinal)) { row.Remark = remark; changed = true; }
        if (row.ParentDeptId != model.ParentDeptId) { row.ParentDeptId = model.ParentDeptId; changed = true; }
        if (row.DispSeq != model.DispSeq) { row.DispSeq = model.DispSeq; changed = true; }

        var newLevel = await ResolveDeptLevelAsync(model.ParentDeptId, model.DeptLevel, ct);
        if (row.DeptLevel != newLevel) { row.DeptLevel = newLevel; changed = true; }

        var newPath = await BuildDeptPathAsync(row.DataId, model.ParentDeptId, ct);
        if (!string.Equals(row.DeptPath, newPath, StringComparison.Ordinal)) { row.DeptPath = newPath; changed = true; }

        if (!changed) return true;

        row.AmendDate = DateTime.Now;
        row.OperatorName = operatorId;
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("部门代码已存在，请换一个。", ex);
        }

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.EDepartments.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return true;
        row.IsDeleted = true;
        row.BStatus = "2";
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task BatchDeleteAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return;
        var rows = await _db.EDepartments.Where(x => ids.Contains(x.DataId) && !x.IsDeleted).ToListAsync(ct);
        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.BStatus = "2";
            row.AmendDate = DateTime.Now;
        }
        if (rows.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    private async Task<string> BuildDeptPathAsync(int dataId, int? parentDeptId, CancellationToken ct)
    {
        if (!parentDeptId.HasValue)
            return $"/{dataId}/";
        var parent = await _db.EDepartments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == parentDeptId.Value, ct);
        var prefix = (parent?.DeptPath ?? $"/{parentDeptId.Value}/").TrimEnd('/');
        return $"{prefix}/{dataId}/";
    }

    private async Task<int> ResolveDeptLevelAsync(int? parentDeptId, int formLevel, CancellationToken ct)
    {
        if (!parentDeptId.HasValue)
            return formLevel > 0 ? formLevel : 1;
        var parent = await _db.EDepartments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == parentDeptId.Value, ct);
        if (parent == null)
            return formLevel > 0 ? formLevel : 1;
        return parent.DeptLevel + 1;
    }

    public async Task<EDepartment?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.EDepartments.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id, ct);

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        for (var e = ex.InnerException; e != null; e = e.InnerException)
        {
            if (e is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
                return true;
        }
        return false;
    }

    private static List<EDepartment> SortRows(List<EDepartment> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        return field switch
        {
            "DeptCode" => desc ? rows.OrderByDescending(x => x.DeptCode).ToList() : rows.OrderBy(x => x.DeptCode).ToList(),
            "DeptCName" => desc ? rows.OrderByDescending(x => x.DeptCName).ToList() : rows.OrderBy(x => x.DeptCName).ToList(),
            "ParentDeptID" => desc ? rows.OrderByDescending(x => x.ParentDeptId).ToList() : rows.OrderBy(x => x.ParentDeptId).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            _ => rows.OrderByDescending(x => x.DataId).ToList()
        };
    }

    private static bool MatchNullableIntFilter(int? value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        return int.TryParse(selected, out var n) && value == n;
    }

    private static bool MatchIntFilter(int value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        return int.TryParse(selected, out var n) && value == n;
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
}
