using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class EPositionDutyService
{
    private readonly FrameworkDbContext _db;

    public EPositionDutyService(FrameworkDbContext db)
    {
        _db = db;
    }

    public string Normalize(string? value, int maxLen, string fallback = "")
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) v = fallback;
        return v.Length <= maxLen ? v : v[..maxLen];
    }

    public static string NormalizeBStatus(string? value)
    {
        var v = (value ?? "").Trim();
        return v switch
        {
            "1" or "启用" => "1",
            "2" or "停用" => "2",
            _ => "1"
        };
    }

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
        ("Position", "岗位"),
        ("Duty", "职责"),
        ("BusinessLimit", "业务限制"),
        ("Remark", "备注")
    ];

    public async Task<List<(int Id, string Code, string Name)>> LoadActivePositionsAsync(CancellationToken ct)
    {
        return await _db.EPositions.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.BStatus == "1" || x.BStatus == "启用"))
            .OrderBy(x => x.PostCode)
            .Select(x => new ValueTuple<int, string, string>(x.DataId, x.PostCode, x.PostCName))
            .ToListAsync(ct);
    }

    public async Task<List<(int Id, string Code, string Name)>> LoadActiveDutiesAsync(CancellationToken ct)
    {
        return await _db.EEDuties.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.BStatus == null || x.BStatus == "1" || x.BStatus == "启用" || x.BStatus == "A")
            .OrderBy(x => x.DutyCode)
            .Select(x => new ValueTuple<int, string, string>(x.DataId, x.DutyCode, x.DutyCName))
            .ToListAsync(ct);
    }

    public async Task<List<(int Id, string Code, string Name)>> LoadActiveDepartmentsAsync(CancellationToken ct)
    {
        return await _db.EDepartments.AsNoTracking()
            .Where(x => x.BStatus == "1" || x.BStatus == "启用")
            .OrderBy(x => x.DeptCode)
            .Select(x => new ValueTuple<int, string, string>(x.DataId, x.DeptCode, x.DeptCName))
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<EPositionDutyListRowVm> pageRows, int totalRecords, int totalPages, int page)> GetIndexPageAsync(
        string? bStatus1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EPositionDuties.AsNoTracking().ToListAsync(ct);

        var posIds = rows.Select(x => x.PosId).Distinct().ToList();
        var dutyIds = rows.Select(x => x.DutyId).Distinct().ToList();
        var deptIds = rows.Where(x => x.DeptId.HasValue).Select(x => x.DeptId!.Value).Distinct().ToList();

        var positions = await _db.EPositions.AsNoTracking()
            .Where(x => posIds.Contains(x.DataId))
            .Select(x => new { x.DataId, Code = x.PostCode, Name = x.PostCName })
            .ToListAsync(ct);
        var duties = await _db.EEDuties.AsNoTracking()
            .Where(x => dutyIds.Contains(x.DataId))
            .Select(x => new { x.DataId, x.DutyCode, x.DutyCName })
            .ToListAsync(ct);
        var depts = await _db.EDepartments.AsNoTracking()
            .Where(x => deptIds.Contains(x.DataId))
            .Select(x => new { x.DataId, x.DeptCode, x.DeptCName })
            .ToListAsync(ct);

        var posMap = positions.ToDictionary(x => x.DataId, x => $"{x.Code} - {x.Name}".Trim());
        var dutyMap = duties.ToDictionary(x => x.DataId, x => $"{x.DutyCode} - {x.DutyCName}".Trim());
        var deptMap = depts.ToDictionary(x => x.DataId, x => $"{x.DeptCode} - {x.DeptCName}".Trim());

        rows = rows.Where(x => MatchBStatusFilter(x.BStatus, bStatus1)).ToList();

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            bool Match(string s) => s.Contains(kw, StringComparison.OrdinalIgnoreCase);
            rows = (searchField ?? "Position") switch
            {
                "Position" => rows.Where(x => posMap.TryGetValue(x.PosId, out var p) && Match(p)).ToList(),
                "Duty" => rows.Where(x => dutyMap.TryGetValue(x.DutyId, out var d) && Match(d)).ToList(),
                "BusinessLimit" => rows.Where(x => Match((x.BusinessLimit ?? "").Trim())).ToList(),
                "Remark" => rows.Where(x => Match((x.Remark ?? "").Trim())).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow, posMap);
        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EPositionDutyListRowVm
        {
            DataId = x.DataId,
            PosId = x.PosId,
            PosDisplay = posMap.TryGetValue(x.PosId, out var p) ? p : $"#{x.PosId}",
            DutyId = x.DutyId,
            DutyDisplay = dutyMap.TryGetValue(x.DutyId, out var d) ? d : $"#{x.DutyId}",
            DeptId = x.DeptId,
            DeptDisplay = x.DeptId.HasValue ? (deptMap.TryGetValue(x.DeptId.Value, out var dep) ? dep : $"#{x.DeptId.Value}") : "不限",
            BusinessLimit = string.IsNullOrWhiteSpace(x.BusinessLimit) ? "-" : x.BusinessLimit!,
            DispSeq = x.DispSeq,
            BStatus = ToStatusDisplay(x.BStatus),
            Remark = string.IsNullOrWhiteSpace(x.Remark) ? "-" : x.Remark!
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public EPositionDutyFormVm ToForm(EPositionDuty row) => new()
    {
        DataId = row.DataId,
        PosId = row.PosId,
        DutyId = row.DutyId,
        DeptId = row.DeptId,
        BusinessLimit = row.BusinessLimit ?? "",
        DispSeq = row.DispSeq,
        BStatus = NormalizeBStatus(row.BStatus),
        Remark = row.Remark ?? ""
    };

    public void NormalizeFormForSave(EPositionDutyFormVm model)
    {
        model.BusinessLimit = Normalize(model.BusinessLimit, 50);
        if (string.IsNullOrWhiteSpace(model.BusinessLimit)) model.BusinessLimit = null;
        model.Remark = (model.Remark ?? "").Trim();
        if (string.IsNullOrWhiteSpace(model.Remark)) model.Remark = null;
        model.BStatus = NormalizeBStatus(model.BStatus);
    }

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(EPositionDutyFormVm model, bool isEdit, CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();

        if (!await _db.EPositions.AsNoTracking().AnyAsync(x => x.DataId == model.PosId, ct))
            errors.Add((nameof(model.PosId), "选择的岗位不存在。"));
        if (!await _db.EEDuties.AsNoTracking().AnyAsync(x => x.DataId == model.DutyId, ct))
            errors.Add((nameof(model.DutyId), "选择的职责不存在。"));
        if (model.DeptId.HasValue && !await _db.EDepartments.AsNoTracking().AnyAsync(x => x.DataId == model.DeptId.Value, ct))
            errors.Add((nameof(model.DeptId), "选择的部门不存在。"));

        var deptNorm = model.DeptId ?? 0;
        var dup = await _db.EPositionDuties.AsNoTracking().AnyAsync(x =>
                x.PosId == model.PosId
                && x.DutyId == model.DutyId
                && (x.DeptId ?? 0) == deptNorm
                && (!isEdit || x.DataId != model.DataId),
            ct);
        if (dup)
            errors.Add((nameof(model.DutyId), "该岗位职责分配已存在（含部门维度），请勿重复。"));

        return errors;
    }

    public async Task CreateAsync(EPositionDutyFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var row = new EPositionDuty
        {
            PosId = model.PosId,
            DutyId = model.DutyId,
            DeptId = model.DeptId,
            BusinessLimit = model.BusinessLimit,
            DispSeq = model.DispSeq,
            BStatus = NormalizeBStatus(model.BStatus),
            Remark = model.Remark,
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        };
        _db.EPositionDuties.Add(row);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("该岗位职责分配已存在（含部门维度）。", ex);
        }
    }

    public async Task<bool> TryUpdateAsync(int id, EPositionDutyFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.EPositionDuties.FirstOrDefaultAsync(x => x.DataId == id, ct);
        if (row == null) return false;

        var bStatus = NormalizeBStatus(model.BStatus);
        var changed = false;
        if (row.PosId != model.PosId) { row.PosId = model.PosId; changed = true; }
        if (row.DutyId != model.DutyId) { row.DutyId = model.DutyId; changed = true; }
        if (row.DeptId != model.DeptId) { row.DeptId = model.DeptId; changed = true; }
        if (!string.Equals((row.BusinessLimit ?? "").Trim(), (model.BusinessLimit ?? "").Trim(), StringComparison.Ordinal)) { row.BusinessLimit = model.BusinessLimit; changed = true; }
        if (row.DispSeq != model.DispSeq) { row.DispSeq = model.DispSeq; changed = true; }
        if (!string.Equals((row.BStatus ?? "").Trim(), bStatus, StringComparison.Ordinal)) { row.BStatus = bStatus; changed = true; }
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
            throw new InvalidOperationException("该岗位职责分配已存在（含部门维度）。", ex);
        }
        return true;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.EPositionDuties.FirstOrDefaultAsync(x => x.DataId == id, ct);
        if (row == null) return;
        _db.EPositionDuties.Remove(row);
        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return;
        var rows = await _db.EPositionDuties.Where(x => ids.Contains(x.DataId)).ToListAsync(ct);
        if (rows.Count <= 0) return;
        _db.EPositionDuties.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<EPositionDuty?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.EPositionDuties.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id, ct);

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

    private static List<EPositionDuty> SortRows(List<EPositionDuty> rows, string field, string arrow, Dictionary<int, string> posMap)
    {
        var desc = arrow == "1";
        string P(int id) => posMap.TryGetValue(id, out var v) ? v : "";
        return field switch
        {
            "DispSeq" => desc ? rows.OrderByDescending(x => x.DispSeq).ToList() : rows.OrderBy(x => x.DispSeq).ToList(),
            "PostCode" => desc ? rows.OrderByDescending(x => P(x.PosId)).ToList() : rows.OrderBy(x => P(x.PosId)).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            "CreateDate" => desc ? rows.OrderByDescending(x => x.CreateDate).ToList() : rows.OrderBy(x => x.CreateDate).ToList(),
            _ => rows.OrderByDescending(x => x.DataId).ToList()
        };
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
