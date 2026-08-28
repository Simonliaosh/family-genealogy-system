using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class EResourcePermissionService
{
    private readonly FrameworkDbContext _db;

    public EResourcePermissionService(FrameworkDbContext db)
    {
        _db = db;
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
        ("启用", "启用"),
        ("停用", "停用")
    ];

    public static List<(string Value, string Text)> BuildBStatusFormOptions() =>
    [
        ("启用", "启用"),
        ("停用", "停用")
    ];

    public string NormalizeBStatus(string? value)
    {
        var v = (value ?? "").Trim();
        return v switch
        {
            "停用" or "2" => "停用",
            _ => "启用"
        };
    }

    public static string ToStatusDisplay(string? value)
    {
        var v = (value ?? "").Trim();
        return v is "停用" or "2" ? "停用" : "启用";
    }

    private static string BitText(bool v) => v ? "是" : "否";

    public async Task<List<(int Id, string Display)>> LoadActiveDutyOptionsAsync(CancellationToken ct)
    {
        return await _db.EEDuties.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.BStatus == null || x.BStatus == "1" || x.BStatus == "启用" || x.BStatus == "A")
            .OrderBy(x => x.DutyCode)
            .Select(x => new ValueTuple<int, string>(x.DataId, $"{x.DutyCode} - {x.DutyCName}".Trim()))
            .ToListAsync(ct);
    }

    public async Task<List<(string Code, string Display)>> LoadActiveResourceOptionsAsync(CancellationToken ct)
    {
        var rows = await _db.EResources.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.ResourceId)
            .ToListAsync(ct);
        rows = rows.Where(x => Helpers.EBStatusHelper.IsActiveBStatus(x.BStatus)).ToList();
        return rows.Select(x =>
        {
            var name = x.ResourceName ?? "";
            return (x.ResourceId, $"{x.ResourceId} - {name}".Trim());
        }).ToList();
    }

    public EResourcePermissionFormVm ToForm(EResourcePermission row)
    {
        return new EResourcePermissionFormVm
        {
            DataId = row.DataId,
            DutyId = row.DutyId,
            ResourceId = row.ResourceId ?? "",
            CanQuery = row.CanQuery,
            CanCreate = row.CanCreate,
            CanUpdate = row.CanUpdate,
            CanDelete = row.CanDelete,
            BStatus = NormalizeBStatus(row.BStatus)
        };
    }

    public void NormalizeFormForSave(EResourcePermissionFormVm model, bool keysEditable)
    {
        if (keysEditable)
            model.ResourceId = Normalize(model.ResourceId, 50);
        model.BStatus = NormalizeBStatus(model.BStatus);
    }

    public async Task<(IReadOnlyList<EResourcePermissionListRowVm> pageRows, int totalRecords, int totalPages, int page)> GetIndexPageAsync(
        string? bStatus1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EResourcePermissions.AsNoTracking().ToListAsync(ct);

        var dutyIds = rows.Select(x => x.DutyId).Distinct().ToList();
        var resIds = rows.Select(x => x.ResourceId).Distinct().ToList();

        var duties = await _db.EEDuties.AsNoTracking()
            .Where(x => dutyIds.Contains(x.DataId))
            .Select(x => new { x.DataId, x.DutyCode, x.DutyCName })
            .ToListAsync(ct);
        var resources = await _db.EResources.AsNoTracking()
            .Where(x => resIds.Contains(x.ResourceId))
            .Select(x => new { x.ResourceId, x.ResourceName })
            .ToListAsync(ct);

        var dutyMap = duties.ToDictionary(x => x.DataId, x => $"{x.DutyCode} - {x.DutyCName}".Trim());
        var resMap = resources.ToDictionary(x => x.ResourceId, x => $"{x.ResourceId} - {(x.ResourceName ?? "")}".Trim(), StringComparer.OrdinalIgnoreCase);

        rows = rows.Where(x => MatchBStatusFilter(x.BStatus, bStatus1)).ToList();

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            bool Match(string s) => s.Contains(kw, StringComparison.OrdinalIgnoreCase);
            rows = (searchField ?? "Duty") switch
            {
                "Duty" => rows.Where(x =>
                    dutyMap.TryGetValue(x.DutyId, out var d) && Match(d)).ToList(),
                "ResourceID" => rows.Where(x => Match(x.ResourceId)).ToList(),
                "ResourceName" => rows.Where(x =>
                    resMap.TryGetValue(x.ResourceId, out var r) && Match(r)).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EResourcePermissionListRowVm
        {
            DataId = x.DataId,
            DutyDisplay = dutyMap.TryGetValue(x.DutyId, out var dd) ? dd : $"#{x.DutyId}",
            ResourceDisplay = resMap.TryGetValue(x.ResourceId, out var rd) ? rd : x.ResourceId,
            CanQueryText = BitText(x.CanQuery),
            CanCreateText = BitText(x.CanCreate),
            CanUpdateText = BitText(x.CanUpdate),
            CanDeleteText = BitText(x.CanDelete),
            BStatusText = ToStatusDisplay(x.BStatus)
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(
        EResourcePermissionFormVm model, bool isEdit, bool keysEditable, CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();
        if (!model.CanQuery)
            errors.Add((nameof(EResourcePermissionFormVm.CanQuery), "须勾选「可查询」；不可全部为否。"));

        if (keysEditable)
        {
            if (model.DutyId <= 0)
                errors.Add((nameof(EResourcePermissionFormVm.DutyId), "请选择职责。"));
            if (model.ResourceId.Length == 0)
                errors.Add((nameof(EResourcePermissionFormVm.ResourceId), "请选择资源。"));
            if (model.DutyId > 0
                && !await _db.EEDuties.AsNoTracking().AnyAsync(x => x.DataId == model.DutyId, ct))
                errors.Add((nameof(EResourcePermissionFormVm.DutyId), "所选职责不存在。"));
            if (model.ResourceId.Length > 0
                && !await _db.EResources.AsNoTracking().AnyAsync(x => !x.IsDeleted && x.ResourceId == model.ResourceId, ct))
                errors.Add((nameof(EResourcePermissionFormVm.ResourceId), "所选资源编码不存在。"));
        }

        if (keysEditable)
        {
            var dup = await _db.EResourcePermissions.AsNoTracking()
                .AnyAsync(x =>
                    x.DutyId == model.DutyId
                    && string.Equals(x.ResourceId, model.ResourceId, StringComparison.Ordinal)
                    && (!isEdit || x.DataId != model.DataId), ct);
            if (dup)
                errors.Add((nameof(EResourcePermissionFormVm.ResourceId), "该职责下此资源权限已存在，请勿重复。"));
        }

        return errors;
    }

    public async Task CreateAsync(EResourcePermissionFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var entity = new EResourcePermission
        {
            DutyId = model.DutyId,
            ResourceId = model.ResourceId,
            CanQuery = model.CanQuery,
            CanCreate = model.CanCreate,
            CanUpdate = model.CanUpdate,
            CanDelete = model.CanDelete,
            BStatus = NormalizeBStatus(model.BStatus),
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        };
        _db.EResourcePermissions.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("该职责与资源组合已存在。", ex);
        }
    }

    public async Task<bool> TryUpdateAsync(int id, EResourcePermissionFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.EResourcePermissions.FirstOrDefaultAsync(x => x.DataId == id, ct);
        if (row == null) return false;

        var bStatus = NormalizeBStatus(model.BStatus);
        var changed = false;
        if (row.CanQuery != model.CanQuery) { row.CanQuery = model.CanQuery; changed = true; }
        if (row.CanCreate != model.CanCreate) { row.CanCreate = model.CanCreate; changed = true; }
        if (row.CanUpdate != model.CanUpdate) { row.CanUpdate = model.CanUpdate; changed = true; }
        if (row.CanDelete != model.CanDelete) { row.CanDelete = model.CanDelete; changed = true; }
        if (!string.Equals((row.BStatus ?? "").Trim(), bStatus, StringComparison.Ordinal)) { row.BStatus = bStatus; changed = true; }
        if (!changed) return true;

        row.AmendDate = DateTime.Now;
        row.OperatorName = operatorId;
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("该职责与资源组合已存在。", ex);
        }
        return true;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.EResourcePermissions.FirstOrDefaultAsync(x => x.DataId == id, ct);
        if (row == null) return;
        _db.EResourcePermissions.Remove(row);
        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return;
        var rows = await _db.EResourcePermissions.Where(x => ids.Contains(x.DataId)).ToListAsync(ct);
        if (rows.Count <= 0) return;
        _db.EResourcePermissions.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<EResourcePermission?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.EResourcePermissions.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id, ct);

    public async Task<string?> GetDutyDisplayAsync(int dutyId, CancellationToken ct)
    {
        var d = await _db.EEDuties.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == dutyId, ct);
        return d == null ? null : $"{d.DutyCode} - {d.DutyCName}".Trim();
    }

    public async Task<string?> GetResourceDisplayAsync(string resourceId, CancellationToken ct)
    {
        var r = await _db.EResources.AsNoTracking().FirstOrDefaultAsync(x => x.ResourceId == resourceId, ct);
        return r == null ? resourceId : $"{r.ResourceId} - {(r.ResourceName ?? "")}".Trim();
    }

    private static List<EResourcePermission> SortRows(List<EResourcePermission> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        return field switch
        {
            "DutyID" => desc ? rows.OrderByDescending(x => x.DutyId).ToList() : rows.OrderBy(x => x.DutyId).ToList(),
            "ResourceID" => desc ? rows.OrderByDescending(x => x.ResourceId).ToList() : rows.OrderBy(x => x.ResourceId).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            _ => rows.OrderBy(x => x.DutyId).ThenBy(x => x.ResourceId).ToList()
        };
    }

    private static bool MatchBStatusFilter(string? value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        var v = (value ?? "").Trim();
        return selected switch
        {
            "启用" => v == "启用" || v == "1",
            "停用" => v == "停用" || v == "2",
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
