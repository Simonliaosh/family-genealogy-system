using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class EDutyService
{
    private readonly FrameworkDbContext _db;
    private readonly DictService _dict;

    public static IReadOnlyDictionary<string, string> DutyCategoryFallback { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["APPROVAL"] = "审批",
            ["SALES"] = "销售",
            ["SERVICE"] = "服务",
            ["FINANCE"] = "财务"
        };

    public EDutyService(FrameworkDbContext db, DictService dict)
    {
        _db = db;
        _dict = dict;
    }

    public Task<List<(string Value, string Text)>> GetDutyCategoryFormOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.DutyCategory, forFilter: false, formEmptyLabel: "（未填）",
            DutyCategoryFallback.Select(x => (x.Key, x.Value)).ToList(), ct: ct);

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

    public EDutyFormVm ToForm(EEDuty row)
    {
        return new EDutyFormVm
        {
            DataId = row.DataId,
            DutyCode = row.DutyCode ?? "",
            DutyCName = row.DutyCName ?? "",
            DutyEName = row.DutyEName ?? "",
            DutyCategory = row.DutyCategory,
            DutyDispSeq = row.DutyDispSeq < 0 ? 0 : row.DutyDispSeq,
            BStatus = NormalizeBStatus(row.BStatus),
            DDescription = row.DDescription ?? "",
            DutyFlow = row.DutyFlow ?? "",
            Remark = row.Remark ?? ""
        };
    }

    public void NormalizeFormForSave(EDutyFormVm model)
    {
        model.DutyCode = Normalize(model.DutyCode, 30);
        model.DutyCName = Normalize(model.DutyCName, 100);
        var en = (model.DutyEName ?? "").Trim();
        model.DutyEName = en.Length == 0 ? null : (en.Length <= 100 ? en : en[..100]);
        model.DutyCategory = string.IsNullOrWhiteSpace(model.DutyCategory)
            ? null
            : Normalize(model.DutyCategory, 50);
        model.BStatus = NormalizeBStatus(model.BStatus);
        model.DDescription = (model.DDescription ?? "").Trim();
        model.DutyFlow = (model.DutyFlow ?? "").Trim();
        model.Remark = (model.Remark ?? "").Trim();
    }

    public async Task<(IReadOnlyList<EDutyListRowVm> pageRows, int totalRecords, int totalPages, int page)> GetIndexPageAsync(
        string? bStatus1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EEDuties.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(ct);

        rows = rows.Where(x => MatchBStatusFilter(x.BStatus, bStatus1)).ToList();

        var sc = (searchContent ?? "").Trim();
        if (sc.Length > 0)
        {
            rows = (searchField ?? "DutyCode") switch
            {
                "DutyCode" => rows.Where(x => (x.DutyCode ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "DutyCName" => rows.Where(x => (x.DutyCName ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "DutyEName" => rows.Where(x => (x.DutyEName ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "Remark" => rows.Where(x => (x.Remark ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "DDescription" => rows.Where(x => (x.DDescription ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "DutyFlow" => rows.Where(x => (x.DutyFlow ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EDutyListRowVm
        {
            DataId = x.DataId,
            DutyCode = x.DutyCode ?? "",
            DutyCName = x.DutyCName ?? "",
            DutyEName = x.DutyEName ?? "",
            BStatus = ToStatusDisplay(x.BStatus)
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(EDutyFormVm model, bool isEdit, CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();
        if (model.DutyCode.Length == 0)
            errors.Add((nameof(EDutyFormVm.DutyCode), "职责代码不能为空。"));

        var dup = await _db.EEDuties.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.DutyCode == model.DutyCode && (!isEdit || x.DataId != model.DataId), ct);
        if (dup)
            errors.Add((nameof(EDutyFormVm.DutyCode), "职责代码已存在，请换一个。"));

        return errors;
    }

    public async Task CreateAsync(EDutyFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var entity = new EEDuty
        {
            DutyCode = model.DutyCode,
            DutyCName = model.DutyCName,
            DutyEName = model.DutyEName,
            DutyCategory = model.DutyCategory,
            DutyDispSeq = model.DutyDispSeq,
            BStatus = NormalizeBStatus(model.BStatus),
            DDescription = string.IsNullOrWhiteSpace(model.DDescription) ? null : model.DDescription,
            DutyFlow = string.IsNullOrWhiteSpace(model.DutyFlow) ? null : model.DutyFlow,
            Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark,
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        };
        _db.EEDuties.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("职责代码已存在，请换一个。", ex);
        }
    }

    public async Task<bool> TryUpdateAsync(int id, EDutyFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.EEDuties.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return false;

        var bStatus = NormalizeBStatus(model.BStatus);
        var changed = false;
        if (!string.Equals((row.DutyCode ?? "").Trim(), model.DutyCode, StringComparison.Ordinal)) { row.DutyCode = model.DutyCode; changed = true; }
        if (!string.Equals((row.DutyCName ?? "").Trim(), model.DutyCName, StringComparison.Ordinal)) { row.DutyCName = model.DutyCName; changed = true; }
        if (!string.Equals((row.DutyEName ?? "").Trim(), (model.DutyEName ?? "").Trim(), StringComparison.Ordinal)) { row.DutyEName = model.DutyEName; changed = true; }
        if (!string.Equals((row.DutyCategory ?? "").Trim(), (model.DutyCategory ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) { row.DutyCategory = model.DutyCategory; changed = true; }
        if (row.DutyDispSeq != model.DutyDispSeq) { row.DutyDispSeq = model.DutyDispSeq; changed = true; }
        if (!string.Equals((row.BStatus ?? "").Trim(), bStatus, StringComparison.Ordinal)) { row.BStatus = bStatus; changed = true; }
        if (!string.Equals((row.DDescription ?? "").Trim(), (model.DDescription ?? "").Trim(), StringComparison.Ordinal)) { row.DDescription = model.DDescription; changed = true; }
        if (!string.Equals((row.DutyFlow ?? "").Trim(), (model.DutyFlow ?? "").Trim(), StringComparison.Ordinal)) { row.DutyFlow = model.DutyFlow; changed = true; }
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
            throw new InvalidOperationException("职责代码已存在，请换一个。", ex);
        }
        return true;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.EEDuties.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return;
        row.IsDeleted = true;
        row.BStatus = "2";
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return;
        var rows = await _db.EEDuties.Where(x => ids.Contains(x.DataId) && !x.IsDeleted).ToListAsync(ct);
        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.BStatus = "2";
            row.AmendDate = DateTime.Now;
        }
        if (rows.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    public async Task<EEDuty?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.EEDuties.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);

    private static List<EEDuty> SortRows(List<EEDuty> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        return field switch
        {
            "DutyCode" => desc ? rows.OrderByDescending(x => x.DutyCode).ToList() : rows.OrderBy(x => x.DutyCode).ToList(),
            "DutyCName" => desc ? rows.OrderByDescending(x => x.DutyCName).ToList() : rows.OrderBy(x => x.DutyCName).ToList(),
            "DutyEName" => desc ? rows.OrderByDescending(x => x.DutyEName).ToList() : rows.OrderBy(x => x.DutyEName).ToList(),
            "DutyDispSeq" => desc ? rows.OrderByDescending(x => x.DutyDispSeq).ToList() : rows.OrderBy(x => x.DutyDispSeq).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            _ => rows.OrderBy(x => x.DutyDispSeq).ThenBy(x => x.DutyCode).ToList()
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
