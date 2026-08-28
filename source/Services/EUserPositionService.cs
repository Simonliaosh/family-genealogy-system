using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class EUserPositionService
{
    private readonly FrameworkDbContext _db;

    public EUserPositionService(FrameworkDbContext db)
    {
        _db = db;
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

    /// <summary>读取生效（启用）用户列表，显示「LoginId - RealName」。</summary>
    public async Task<List<(int Id, string LoginId, string RealName)>> LoadActiveUsersAsync(CancellationToken ct)
    {
        return await _db.EUsers.AsNoTracking()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.LoginId)
            .Select(x => new ValueTuple<int, string, string>(x.DataId, x.LoginId, x.RealName))
            .ToListAsync(ct);
    }

    /// <summary>读取生效（启用）部门列表，显示「DeptCode - DeptCName」。</summary>
    public async Task<List<(int Id, string Code, string Name)>> LoadActiveDepartmentsAsync(CancellationToken ct)
    {
        return await _db.EDepartments.AsNoTracking()
            .Where(x => x.BStatus == "1" || x.BStatus == "启用")
            .OrderBy(x => x.DeptCode)
            .Select(x => new ValueTuple<int, string, string>(x.DataId, x.DeptCode, x.DeptCName))
            .ToListAsync(ct);
    }

    /// <summary>读取生效（启用）岗位列表，显示「PostCode - PostCName」。</summary>
    public async Task<List<(int Id, string Code, string Name)>> LoadActivePositionsAsync(CancellationToken ct)
    {
        return await _db.EPositions.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.BStatus == "1" || x.BStatus == "启用"))
            .OrderBy(x => x.PostCode)
            .Select(x => new ValueTuple<int, string, string>(x.DataId, x.PostCode, x.PostCName))
            .ToListAsync(ct);
    }

    public async Task<(
        IReadOnlyList<EUserPositionListRowVm> pageRows,
        int totalRecords,
        int totalPages,
        int page)> GetIndexPageAsync(
        string? bStatus1,
        string searchKeyword,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EUserPositions.AsNoTracking().ToListAsync(ct);

        var userIds = rows.Select(x => x.UserId).Distinct().ToList();
        var deptIds = rows.Select(x => x.DeptId).Distinct().ToList();
        var posIds = rows.Select(x => x.PosId).Distinct().ToList();

        var users = await _db.EUsers.AsNoTracking()
            .Where(x => userIds.Contains(x.DataId))
            .Select(x => new { x.DataId, x.LoginId, x.RealName })
            .ToListAsync(ct);
        var depts = await _db.EDepartments.AsNoTracking()
            .Where(x => deptIds.Contains(x.DataId))
            .Select(x => new { x.DataId, x.DeptCode, x.DeptCName })
            .ToListAsync(ct);
        var poses = await _db.EPositions.AsNoTracking()
            .Where(x => posIds.Contains(x.DataId))
            .Select(x => new { x.DataId, x.PostCode, x.PostCName })
            .ToListAsync(ct);

        var userMap = users.ToDictionary(x => x.DataId, x => $"{x.LoginId} - {x.RealName}".Trim());
        var deptMap = depts.ToDictionary(x => x.DataId, x => $"{x.DeptCode} - {x.DeptCName}".Trim());
        var posMap = poses.ToDictionary(x => x.DataId, x => $"{x.PostCode} - {x.PostCName}".Trim());

        rows = rows.Where(x => MatchBStatusFilter(x.BStatus, bStatus1)).ToList();

        var kw = (searchKeyword ?? "").Trim();
        if (kw.Length > 0)
        {
            bool Match(string s) => s.Contains(kw, StringComparison.OrdinalIgnoreCase);
            rows = rows.Where(x =>
                    (userMap.TryGetValue(x.UserId, out var u) && Match(u))
                    || (deptMap.TryGetValue(x.DeptId, out var d) && Match(d))
                    || (posMap.TryGetValue(x.PosId, out var p) && Match(p)))
                .ToList();
        }

        rows = SortRows(rows, sortField, sortArrow, userMap, deptMap, posMap);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EUserPositionListRowVm
        {
            DataId = x.DataId,
            UserId = x.UserId,
            UserDisplay = userMap.TryGetValue(x.UserId, out var u) ? u : $"#{x.UserId}",
            DeptId = x.DeptId,
            DeptDisplay = deptMap.TryGetValue(x.DeptId, out var d) ? d : $"#{x.DeptId}",
            PosId = x.PosId,
            PosDisplay = posMap.TryGetValue(x.PosId, out var p) ? p : $"#{x.PosId}",
            BStatus = ToStatusDisplay(x.BStatus)
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public EUserPositionFormVm ToForm(EUserPosition row) => new()
    {
        DataId = row.DataId,
        UserId = row.UserId,
        DeptId = row.DeptId,
        PosId = row.PosId,
        BStatus = NormalizeBStatus(row.BStatus)
    };

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(
        EUserPositionFormVm model, bool isEdit, CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();

        if (!await _db.EUsers.AsNoTracking().AnyAsync(x => x.DataId == model.UserId, ct))
            errors.Add((nameof(model.UserId), "选择的用户不存在。"));
        if (!await _db.EDepartments.AsNoTracking().AnyAsync(x => x.DataId == model.DeptId, ct))
            errors.Add((nameof(model.DeptId), "选择的部门不存在。"));
        if (!await _db.EPositions.AsNoTracking().AnyAsync(x => x.DataId == model.PosId, ct))
            errors.Add((nameof(model.PosId), "选择的岗位不存在。"));

        var dup = await _db.EUserPositions.AsNoTracking().AnyAsync(x =>
                x.UserId == model.UserId && x.DeptId == model.DeptId && x.PosId == model.PosId
                && (!isEdit || x.DataId != model.DataId),
            ct);
        if (dup)
            errors.Add((nameof(model.PosId), "该用户在该部门的此岗位已存在，请勿重复分配。"));

        return errors;
    }

    public async Task CreateAsync(EUserPositionFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var row = new EUserPosition
        {
            UserId = model.UserId,
            DeptId = model.DeptId,
            PosId = model.PosId,
            BStatus = NormalizeBStatus(model.BStatus),
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        };
        _db.EUserPositions.Add(row);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("该用户在该部门的此岗位已存在。", ex);
        }
    }

    public async Task<bool> TryUpdateAsync(int id, EUserPositionFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.EUserPositions.FirstOrDefaultAsync(x => x.DataId == id, ct);
        if (row == null) return false;

        var bStatus = NormalizeBStatus(model.BStatus);
        var changed = false;
        if (row.UserId != model.UserId) { row.UserId = model.UserId; changed = true; }
        if (row.DeptId != model.DeptId) { row.DeptId = model.DeptId; changed = true; }
        if (row.PosId != model.PosId) { row.PosId = model.PosId; changed = true; }
        if (!string.Equals(row.BStatus, bStatus, StringComparison.Ordinal)) { row.BStatus = bStatus; changed = true; }

        if (!changed) return true;
        row.AmendDate = DateTime.Now;
        row.OperatorName = operatorId;
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("该用户在该部门的此岗位已存在。", ex);
        }
        return true;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.EUserPositions.FirstOrDefaultAsync(x => x.DataId == id, ct);
        if (row == null) return;
        _db.EUserPositions.Remove(row);
        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return;
        var rows = await _db.EUserPositions.Where(x => ids.Contains(x.DataId)).ToListAsync(ct);
        if (rows.Count > 0)
        {
            _db.EUserPositions.RemoveRange(rows);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<EUserPosition?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.EUserPositions.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id, ct);

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

    private static List<EUserPosition> SortRows(
        List<EUserPosition> rows, string field, string arrow,
        Dictionary<int, string> userMap, Dictionary<int, string> deptMap, Dictionary<int, string> posMap)
    {
        var desc = arrow == "1";
        string U(int id) => userMap.TryGetValue(id, out var v) ? v : "";
        string D(int id) => deptMap.TryGetValue(id, out var v) ? v : "";
        string P(int id) => posMap.TryGetValue(id, out var v) ? v : "";
        return field switch
        {
            "UserID" => desc ? rows.OrderByDescending(x => U(x.UserId)).ToList() : rows.OrderBy(x => U(x.UserId)).ToList(),
            "DeptID" => desc ? rows.OrderByDescending(x => D(x.DeptId)).ToList() : rows.OrderBy(x => D(x.DeptId)).ToList(),
            "PosID" => desc ? rows.OrderByDescending(x => P(x.PosId)).ToList() : rows.OrderBy(x => P(x.PosId)).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
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
