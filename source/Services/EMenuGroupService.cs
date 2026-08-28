using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class EMenuGroupService
{
    private readonly FrameworkDbContext _db;

    public EMenuGroupService(FrameworkDbContext db)
    {
        _db = db;
    }

    public string Normalize(string? value, int maxLen, string fallback = "")
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) v = fallback;
        return v.Length <= maxLen ? v : v[..maxLen];
    }

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

    public static string ToStatusDisplay(string? value)
    {
        var v = (value ?? "").Trim();
        return v switch
        {
            "1" or "启用" => "启用",
            "2" or "停用" => "停用",
            _ => string.IsNullOrEmpty(v) ? "-" : v
        };
    }

    public EMenuGroupFormVm ToForm(EMenuGroup row) => new()
    {
        AppCode = string.IsNullOrWhiteSpace(row.AppCode) ? "FRAME" : row.AppCode.Trim(),
        MenuGroupCode = row.MenuGroupCode ?? "",
        MenuGroupName = row.MenuGroupName ?? "",
        DispSeq = row.DispSeq < 0 ? 0 : row.DispSeq,
        BStatus = NormalizeBStatus(row.BStatus),
        Remark = row.Remark
    };

    public void NormalizeFormForSave(EMenuGroupFormVm model, bool isEdit)
    {
        model.AppCode = Normalize(model.AppCode, 50, "FRAME");
        model.MenuGroupCode = Normalize(model.MenuGroupCode, 50).ToUpperInvariant();
        model.MenuGroupName = Normalize(model.MenuGroupName, 100);
        model.BStatus = NormalizeBStatus(model.BStatus);
        model.Remark = (model.Remark ?? "").Trim();
        if (string.IsNullOrWhiteSpace(model.Remark)) model.Remark = null;

        if (isEdit && string.IsNullOrWhiteSpace(model.MenuGroupCode))
            throw new InvalidOperationException("菜单组编码不能为空。");
    }

    public async Task<(IReadOnlyList<EMenuGroupListRowVm> pageRows, int totalRecords, int totalPages, int page)> GetIndexPageAsync(
        string? bStatus1,
        string? appCode1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EMenuGroups.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(ct);

        rows = rows.Where(x => MatchBStatusFilter(x.BStatus, bStatus1)
                            && MatchAppCodeFilter(x.AppCode, appCode1)).ToList();

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            rows = (searchField ?? "MenuGroupCode") switch
            {
                "AppCode" => rows.Where(x => (x.AppCode ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "MenuGroupCode" => rows.Where(x => (x.MenuGroupCode ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "MenuGroupName" => rows.Where(x => (x.MenuGroupName ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "Remark" => rows.Where(x => (x.Remark ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow);
        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EMenuGroupListRowVm
        {
            AppCode = string.IsNullOrWhiteSpace(x.AppCode) ? "FRAME" : x.AppCode,
            MenuGroupCode = x.MenuGroupCode ?? "",
            MenuGroupName = x.MenuGroupName ?? "",
            DispSeq = x.DispSeq,
            BStatusText = ToStatusDisplay(x.BStatus),
            AmendDate = x.AmendDate,
            OperatorName = x.OperatorName ?? ""
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(EMenuGroupFormVm model, bool isEdit, string? originalCode, CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();

        if (model.BStatus is not "1" and not "2")
            errors.Add((nameof(model.BStatus), "状态无效。"));

        var dup = await _db.EMenuGroups.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.MenuGroupCode == model.MenuGroupCode
                && (!isEdit || !string.Equals(x.MenuGroupCode, originalCode, StringComparison.OrdinalIgnoreCase)), ct);
        if (dup)
            errors.Add((nameof(model.MenuGroupCode), "菜单组编码已存在，请换一个。"));

        return errors;
    }

    public async Task CreateAsync(EMenuGroupFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var row = new EMenuGroup
        {
            AppCode = model.AppCode,
            MenuGroupCode = model.MenuGroupCode,
            MenuGroupName = model.MenuGroupName,
            DispSeq = model.DispSeq,
            BStatus = model.BStatus,
            Remark = model.Remark,
            IsDeleted = false,
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        };
        _db.EMenuGroups.Add(row);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("菜单组编码已存在，请换一个。", ex);
        }
    }

    public async Task<bool> TryUpdateAsync(string code, EMenuGroupFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.EMenuGroups.FirstOrDefaultAsync(x => x.MenuGroupCode == code && !x.IsDeleted, ct);
        if (row == null) return false;

        var changed = false;
        if (!string.Equals(row.AppCode ?? "", model.AppCode, StringComparison.OrdinalIgnoreCase))
        {
            row.AppCode = model.AppCode;
            changed = true;
        }
        if (!string.Equals((row.MenuGroupName ?? "").Trim(), model.MenuGroupName, StringComparison.Ordinal))
        {
            row.MenuGroupName = model.MenuGroupName;
            changed = true;
        }
        if (row.DispSeq != model.DispSeq)
        {
            row.DispSeq = model.DispSeq;
            changed = true;
        }
        if (!string.Equals((row.BStatus ?? "").Trim(), model.BStatus, StringComparison.Ordinal))
        {
            row.BStatus = model.BStatus;
            changed = true;
        }
        if (!string.Equals((row.Remark ?? "").Trim(), (model.Remark ?? "").Trim(), StringComparison.Ordinal))
        {
            row.Remark = model.Remark;
            changed = true;
        }
        if (!changed) return true;

        row.AmendDate = DateTime.Now;
        row.OperatorName = operatorId;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task DeleteAsync(string code, CancellationToken ct)
    {
        var row = await _db.EMenuGroups.FirstOrDefaultAsync(x => x.MenuGroupCode == code && !x.IsDeleted, ct);
        if (row == null) return;
        row.IsDeleted = true;
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(IReadOnlyList<string> codes, CancellationToken ct)
    {
        if (codes.Count == 0) return;
        var set = new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
        var rows = await _db.EMenuGroups.Where(x => set.Contains(x.MenuGroupCode) && !x.IsDeleted).ToListAsync(ct);
        if (rows.Count == 0) return;
        var now = DateTime.Now;
        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.AmendDate = now;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<EMenuGroup?> GetByCodeNoTrackAsync(string code, CancellationToken ct) =>
        await _db.EMenuGroups.AsNoTracking().FirstOrDefaultAsync(x => x.MenuGroupCode == code && !x.IsDeleted, ct);

    public async Task<List<(string Value, string Text)>> LoadActiveMenuGroupOptionsAsync(string? appCode, CancellationToken ct)
    {
        var ac = (appCode ?? "").Trim();
        var q = _db.EMenuGroups.AsNoTracking().Where(x => !x.IsDeleted);
        if (ac.Length > 0)
            q = q.Where(x => x.AppCode == ac);

        var rows = await q.OrderBy(x => x.DispSeq).ThenBy(x => x.MenuGroupCode).ToListAsync(ct);
        return rows
            .Where(x => EBStatusHelper.IsActiveBStatus(x.BStatus))
            .Select(x => (x.MenuGroupCode, $"{x.MenuGroupCode} - {x.MenuGroupName}".Trim()))
            .ToList();
    }

    private static List<EMenuGroup> SortRows(List<EMenuGroup> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        return field switch
        {
            "AppCode" => desc ? rows.OrderByDescending(x => x.AppCode).ToList() : rows.OrderBy(x => x.AppCode).ToList(),
            "MenuGroupCode" => desc ? rows.OrderByDescending(x => x.MenuGroupCode).ToList() : rows.OrderBy(x => x.MenuGroupCode).ToList(),
            "MenuGroupName" => desc ? rows.OrderByDescending(x => x.MenuGroupName).ToList() : rows.OrderBy(x => x.MenuGroupName).ToList(),
            "DispSeq" => desc ? rows.OrderByDescending(x => x.DispSeq).ToList() : rows.OrderBy(x => x.DispSeq).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            "AmendDate" => desc ? rows.OrderByDescending(x => x.AmendDate).ToList() : rows.OrderBy(x => x.AmendDate).ToList(),
            _ => rows.OrderBy(x => x.DispSeq).ThenBy(x => x.MenuGroupCode).ToList()
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
