using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class EManagerSubordinateService
{
    private readonly FrameworkDbContext _db;

    public EManagerSubordinateService(FrameworkDbContext db)
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

    public static string FormatUserDisplay(string loginId, string realName) =>
        $"{loginId} - {realName}".Trim();

    public async Task<List<(int Id, string Display)>> LoadActiveUserOptionsAsync(CancellationToken ct)
    {
        return await _db.EUsers.AsNoTracking()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.LoginId)
            .Select(x => new ValueTuple<int, string>(x.DataId, FormatUserDisplay(x.LoginId, x.RealName)))
            .ToListAsync(ct);
    }

    public async Task<List<(int Id, string Display)>> LoadActiveDepartmentOptionsAsync(CancellationToken ct)
    {
        return await _db.EDepartments.AsNoTracking()
            .Where(x => x.BStatus == "1" || x.BStatus == "启用")
            .OrderBy(x => x.DeptCode)
            .Select(x => new ValueTuple<int, string>(x.DataId, $"{x.DeptCode} - {x.DeptCName}".Trim()))
            .ToListAsync(ct);
    }

    public async Task<List<(int Id, string Display)>> LoadActivePositionOptionsAsync(CancellationToken ct)
    {
        return await _db.EPositions.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.BStatus == "1" || x.BStatus == "启用"))
            .OrderBy(x => x.PostCode)
            .Select(x => new ValueTuple<int, string>(x.DataId, $"{x.PostCode} - {x.PostCName}".Trim()))
            .ToListAsync(ct);
    }

    public EManagerSubordinateFormVm ToForm(EManagerSubordinate row)
    {
        return new EManagerSubordinateFormVm
        {
            DataId = row.DataId,
            ManagerUserId = row.ManagerUserId,
            SubUserId = row.SubUserId,
            DeptId = row.DeptId ?? 0,
            ManagerPostId = row.ManagerPostId ?? 0,
            SubPostId = row.SubPostId ?? 0,
            DispSeq = row.DispSeq < 0 ? 0 : row.DispSeq,
            BStatus = NormalizeBStatus(row.BStatus),
            Remark = row.Remark ?? ""
        };
    }

    public void NormalizeFormForSave(EManagerSubordinateFormVm model)
    {
        model.BStatus = NormalizeBStatus(model.BStatus);
        model.Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark.Trim();
    }

    private static int? ZeroToNull(int v) => v <= 0 ? null : v;

    public async Task<(IReadOnlyList<EManagerSubordinateListRowVm> pageRows, int totalRecords, int totalPages, int page)> GetIndexPageAsync(
        string? bStatus1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EManagerSubordinates.AsNoTracking().ToListAsync(ct);

        var userIds = rows.SelectMany(x => new[] { x.ManagerUserId, x.SubUserId }).Distinct().ToList();
        var deptIds = rows.Where(x => x.DeptId.HasValue).Select(x => x.DeptId!.Value).Distinct().ToList();
        var posIds = rows.SelectMany(x => new[] { x.ManagerPostId, x.SubPostId })
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();

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

        var userMap = users.ToDictionary(x => x.DataId, x => FormatUserDisplay(x.LoginId, x.RealName));
        var deptMap = depts.ToDictionary(x => x.DataId, x => $"{x.DeptCode} - {x.DeptCName}".Trim());
        var posMap = poses.ToDictionary(x => x.DataId, x => $"{x.PostCode} - {x.PostCName}".Trim());

        rows = rows.Where(x => MatchBStatusFilter(x.BStatus, bStatus1)).ToList();

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            bool Match(string s) => s.Contains(kw, StringComparison.OrdinalIgnoreCase);
            rows = (searchField ?? "Users") switch
            {
                "Users" => rows.Where(x =>
                    (userMap.TryGetValue(x.ManagerUserId, out var mu) && Match(mu))
                    || (userMap.TryGetValue(x.SubUserId, out var su) && Match(su))).ToList(),
                "Dept" => rows.Where(x =>
                    !x.DeptId.HasValue
                        ? false
                        : (deptMap.TryGetValue(x.DeptId.Value, out var d) && Match(d))).ToList(),
                "Remark" => rows.Where(x => Match(x.Remark ?? "")).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EManagerSubordinateListRowVm
        {
            DataId = x.DataId,
            ManagerDisplay = userMap.TryGetValue(x.ManagerUserId, out var md) ? md : $"#{x.ManagerUserId}",
            SubDisplay = userMap.TryGetValue(x.SubUserId, out var sd) ? sd : $"#{x.SubUserId}",
            ManagerPostText = x.ManagerPostId.HasValue && posMap.TryGetValue(x.ManagerPostId.Value, out var mp) ? mp : "-",
            SubPostText = x.SubPostId.HasValue && posMap.TryGetValue(x.SubPostId.Value, out var sp) ? sp : "-",
            DeptText = x.DeptId.HasValue && deptMap.TryGetValue(x.DeptId.Value, out var dep) ? dep : "-",
            BStatusText = ToStatusDisplay(x.BStatus)
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(
        EManagerSubordinateFormVm model, bool isEdit, CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();
        if (model.ManagerUserId == model.SubUserId)
            errors.Add((nameof(EManagerSubordinateFormVm.SubUserId), "上级用户与下级用户不能为同一人。"));

        if (!await _db.EUsers.AsNoTracking().AnyAsync(x => x.DataId == model.ManagerUserId, ct))
            errors.Add((nameof(EManagerSubordinateFormVm.ManagerUserId), "上级用户不存在。"));
        if (!await _db.EUsers.AsNoTracking().AnyAsync(x => x.DataId == model.SubUserId, ct))
            errors.Add((nameof(EManagerSubordinateFormVm.SubUserId), "下级用户不存在。"));

        var deptId = ZeroToNull(model.DeptId);
        var mgrPostId = ZeroToNull(model.ManagerPostId);
        var subPostId = ZeroToNull(model.SubPostId);

        if (deptId.HasValue
            && !await _db.EDepartments.AsNoTracking().AnyAsync(x => x.DataId == deptId.Value, ct))
            errors.Add((nameof(EManagerSubordinateFormVm.DeptId), "所选部门不存在。"));

        if (mgrPostId.HasValue
            && !await _db.EPositions.AsNoTracking().AnyAsync(x => x.DataId == mgrPostId.Value, ct))
            errors.Add((nameof(EManagerSubordinateFormVm.ManagerPostId), "所选上级岗位不存在。"));

        if (subPostId.HasValue
            && !await _db.EPositions.AsNoTracking().AnyAsync(x => x.DataId == subPostId.Value, ct))
            errors.Add((nameof(EManagerSubordinateFormVm.SubPostId), "所选下级岗位不存在。"));

        var dup = await _db.EManagerSubordinates.AsNoTracking()
            .AnyAsync(x =>
                x.ManagerUserId == model.ManagerUserId
                && x.SubUserId == model.SubUserId
                && Nullable.Equals(x.DeptId, deptId)
                && Nullable.Equals(x.ManagerPostId, mgrPostId)
                && Nullable.Equals(x.SubPostId, subPostId)
                && (!isEdit || x.DataId != model.DataId), ct);
        if (dup)
            errors.Add((nameof(EManagerSubordinateFormVm.SubUserId), "已存在相同的上级、下级、部门与岗位组合，请勿重复。"));

        return errors;
    }

    public async Task CreateAsync(EManagerSubordinateFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var entity = new EManagerSubordinate
        {
            ManagerUserId = model.ManagerUserId,
            SubUserId = model.SubUserId,
            DeptId = ZeroToNull(model.DeptId),
            ManagerPostId = ZeroToNull(model.ManagerPostId),
            SubPostId = ZeroToNull(model.SubPostId),
            DispSeq = model.DispSeq,
            BStatus = NormalizeBStatus(model.BStatus),
            Remark = model.Remark,
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        };
        _db.EManagerSubordinates.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> TryUpdateAsync(int id, EManagerSubordinateFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.EManagerSubordinates.FirstOrDefaultAsync(x => x.DataId == id, ct);
        if (row == null) return false;

        var bStatus = NormalizeBStatus(model.BStatus);
        var changed = false;
        if (row.ManagerUserId != model.ManagerUserId) { row.ManagerUserId = model.ManagerUserId; changed = true; }
        if (row.SubUserId != model.SubUserId) { row.SubUserId = model.SubUserId; changed = true; }
        var deptId = ZeroToNull(model.DeptId);
        var mgrPostId = ZeroToNull(model.ManagerPostId);
        var subPostId = ZeroToNull(model.SubPostId);
        if (!Nullable.Equals(row.DeptId, deptId)) { row.DeptId = deptId; changed = true; }
        if (!Nullable.Equals(row.ManagerPostId, mgrPostId)) { row.ManagerPostId = mgrPostId; changed = true; }
        if (!Nullable.Equals(row.SubPostId, subPostId)) { row.SubPostId = subPostId; changed = true; }
        if (row.DispSeq != model.DispSeq) { row.DispSeq = model.DispSeq; changed = true; }
        if (!string.Equals((row.BStatus ?? "").Trim(), bStatus, StringComparison.Ordinal)) { row.BStatus = bStatus; changed = true; }
        if (!string.Equals((row.Remark ?? "").Trim(), (model.Remark ?? "").Trim(), StringComparison.Ordinal)) { row.Remark = model.Remark; changed = true; }
        if (!changed) return true;

        row.AmendDate = DateTime.Now;
        row.OperatorName = operatorId;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.EManagerSubordinates.FirstOrDefaultAsync(x => x.DataId == id, ct);
        if (row == null) return;
        _db.EManagerSubordinates.Remove(row);
        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return;
        var rows = await _db.EManagerSubordinates.Where(x => ids.Contains(x.DataId)).ToListAsync(ct);
        if (rows.Count <= 0) return;
        _db.EManagerSubordinates.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<EManagerSubordinate?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.EManagerSubordinates.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id, ct);

    private static List<EManagerSubordinate> SortRows(List<EManagerSubordinate> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        return field switch
        {
            "ManagerUserId" => desc ? rows.OrderByDescending(x => x.ManagerUserId).ToList() : rows.OrderBy(x => x.ManagerUserId).ToList(),
            "SubUserId" => desc ? rows.OrderByDescending(x => x.SubUserId).ToList() : rows.OrderBy(x => x.SubUserId).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            "DispSeq" => desc ? rows.OrderByDescending(x => x.DispSeq).ToList() : rows.OrderBy(x => x.DispSeq).ToList(),
            _ => rows.OrderBy(x => x.DispSeq).ThenBy(x => x.DataId).ToList()
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
}
