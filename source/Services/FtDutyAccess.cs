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
    private readonly FtDutyAccess _duty;
    // 不注入 FtClanService：它自己依赖 FtOpLogService，注进来就是循环依赖。
    // 这里只需要一次「用户属于哪个族」的查询，直接走 DbContext。
    public FtOpLogService(FrameworkDbContext db, FtDutyAccess duty)
    {
        _db = db;
        _duty = duty;
    }

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

    /// <summary>
    /// 操作日志分页。两处修正：
    /// ① 过滤、排序、分页全部下推到数据库——原先是把整张<strong>只增不减</strong>的审计表
    ///    <c>ToListAsync</c> 进内存再切页，这是确定会发生的 OOM；
    /// ② 按调用者所属家族过滤——原先返回全库日志，含他族改动的 BeforeJson/AfterJson。
    /// </summary>
    public async Task<(IReadOnlyList<FamilyTree.Models.ViewModels.FtOpLogListRowVm> page, int total, int pages, int pageOut)>
        GetIndexPageAsync(int userId, string searchField, string searchContent, string sortField, string sortArrow,
            int page, int pageSize, CancellationToken ct)
    {
        var q = _db.FtOpLogs.AsNoTracking().Where(x => !x.IsDeleted);

        if (!await _duty.IsSuperAsync(userId, ct))
        {
            var clanId = await _db.FtUserClans.AsNoTracking()
                .Where(u => !u.IsDeleted && u.UserId == userId && u.BStatus == "1")
                .Select(u => (int?)u.ClanId)
                .FirstOrDefaultAsync(ct);
            if (clanId == null)
                return (Array.Empty<FamilyTree.Models.ViewModels.FtOpLogListRowVm>(), 0, 0, 1);
            // 日志表没有 ClanId 列，按「操作人是否同族」收敛
            var clanUserIds = _db.FtUserClans.AsNoTracking()
                .Where(u => !u.IsDeleted && u.BStatus == "1" && u.ClanId == clanId)
                .Select(u => u.UserId);
            q = q.Where(x => clanUserIds.Contains(x.OpUserId));
        }

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            // EF 可翻译的 Contains 重载（原来用的是带 StringComparison 的重载，只能在内存里跑）
            q = (searchField ?? "") switch
            {
                "OpType" => q.Where(x => x.OpType.Contains(kw)),
                "ObjectType" => q.Where(x => x.ObjectType.Contains(kw)),
                _ => q.Where(x => x.Remark != null && x.Remark.Contains(kw))
            };
        }

        q = (sortField, sortArrow) switch
        {
            ("OpType", "1") => q.OrderByDescending(x => x.OpType).ThenByDescending(x => x.DataId),
            ("OpType", _) => q.OrderBy(x => x.OpType).ThenByDescending(x => x.DataId),
            _ => q.OrderByDescending(x => x.CreateDate).ThenByDescending(x => x.DataId)
        };

        var (rows, total, pages, p) = await FamilyTree.Helpers.FtPaging.PageAsync(q, page, pageSize, ct);
        var vm = rows.Select(x => new FamilyTree.Models.ViewModels.FtOpLogListRowVm
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
