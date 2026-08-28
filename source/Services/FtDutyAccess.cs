using FamilyTree.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public sealed class FtDutyAccess
{
    public const string Member = "FT_MEMBER";
    public const string BranchAdmin = "FT_BRANCH_ADMIN";
    public const string ClanAdmin = "FT_CLAN_ADMIN";
    public const string SuperAdmin = "FT_SUPER_ADMIN";

    private readonly FrameworkDbContext _db;
    public FtDutyAccess(FrameworkDbContext db) => _db = db;

    public async Task<HashSet<string>> DutyCodesAsync(int userId, CancellationToken ct)
    {
        var access = new EPrincipalAccessService(_db);
        var dutyIds = await access.GetDutyIdsForUserAsync(userId, ct);
        if (dutyIds.Count == 0) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var codes = await _db.EEDuties.AsNoTracking()
            .Where(d => dutyIds.Contains(d.DataId) && !d.IsDeleted)
            .Select(d => d.DutyCode)
            .ToListAsync(ct);
        return new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> IsSuperAsync(int userId, CancellationToken ct) =>
        (await DutyCodesAsync(userId, ct)).Contains(SuperAdmin);

    /// <summary>是否挂有族谱管理员岗（不含纯超管）。</summary>
    public async Task<bool> IsClanAdminAsync(int userId, CancellationToken ct) =>
        (await DutyCodesAsync(userId, ct)).Contains(ClanAdmin);

    /// <summary>族谱/支链业务管理岗（不含纯超管；超管不插手业务）。</summary>
    public async Task<bool> IsBranchAdminAsync(int userId, CancellationToken ct)
    {
        var set = await DutyCodesAsync(userId, ct);
        return set.Contains(BranchAdmin) || set.Contains(ClanAdmin);
    }

    /// <summary>超管或族谱管理员（用于打开「家族」管理页）。</summary>
    public async Task<bool> CanManageClansAsync(int userId, CancellationToken ct)
    {
        var set = await DutyCodesAsync(userId, ct);
        return set.Contains(SuperAdmin) || set.Contains(ClanAdmin);
    }

    /// <summary>仅当系统中挂超管岗的账号不超过 1 人时，允许自审。</summary>
    public async Task<bool> AllowSelfApproveAsync(CancellationToken ct)
    {
        var superDuty = await _db.EEDuties.AsNoTracking()
            .Where(d => d.DutyCode == SuperAdmin && !d.IsDeleted)
            .Select(d => d.DataId)
            .FirstOrDefaultAsync(ct);
        if (superDuty == 0) return true;
        var posIds = await _db.EPositionDuties.AsNoTracking()
            .Where(pd => pd.DutyId == superDuty)
            .Select(pd => pd.PosId)
            .ToListAsync(ct);
        var n = await _db.EUserPositions.AsNoTracking()
            .Where(up => posIds.Contains(up.PosId))
            .CountAsync(ct);
        return n <= 1;
    }
}

public sealed class FtOpLogService
{
    private readonly FrameworkDbContext _db;
    public FtOpLogService(FrameworkDbContext db) => _db = db;

    public async Task WriteAsync(string opType, string objectType, string objectKey, int userId, string opName,
        string? remark, string? before, string? after, CancellationToken ct)
    {
        var now = DateTime.Now;
        _db.FtOpLogs.Add(new FtOpLog
        {
            OpType = opType.Length > 32 ? opType[..32] : opType,
            ObjectType = objectType.Length > 32 ? objectType[..32] : objectType,
            ObjectKey = objectKey.Length > 64 ? objectKey[..64] : objectKey,
            OpUserId = userId,
            Remark = remark,
            BeforeJson = before,
            AfterJson = after,
            BStatus = "1",
            CreateDate = now,
            AmendDate = now,
            OperatorName = opName.Length > 30 ? opName[..30] : opName
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<FamilyTree.Models.ViewModels.FtOpLogListRowVm> page, int total, int pages, int pageOut)>
        GetIndexPageAsync(string searchField, string searchContent, string sortField, string sortArrow, int page, int pageSize, CancellationToken ct)
    {
        var rows = await _db.FtOpLogs.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(ct);
        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            rows = (searchField ?? "") switch
            {
                "OpType" => rows.Where(x => x.OpType.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "ObjectType" => rows.Where(x => x.ObjectType.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows.Where(x => (x.Remark ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList()
            };
        }
        rows = (sortField, sortArrow) switch
        {
            ("OpType", "1") => rows.OrderByDescending(x => x.OpType).ToList(),
            ("OpType", _) => rows.OrderBy(x => x.OpType).ToList(),
            _ => rows.OrderByDescending(x => x.CreateDate).ToList()
        };
        var (slice, total, pages, p) = FamilyTree.Helpers.FtPaging.Page(rows, page, pageSize);
        var vm = slice.Select(x => new FamilyTree.Models.ViewModels.FtOpLogListRowVm
        {
            DataId = x.DataId,
            OpType = x.OpType,
            ObjectType = x.ObjectType,
            ObjectKey = x.ObjectKey,
            Remark = x.Remark,
            CreateDate = x.CreateDate
        }).ToList();
        return (vm, total, pages, p);
    }
}
