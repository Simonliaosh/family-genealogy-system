using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public sealed class FtBranchAdminApplyService
{
    public const string ObjectType = "BranchAdminApply";
    public const string EvApply = "FT.BRANCH.APPLY";
    public const string EvOk = "FT.BRANCH.APPROVED";
    public const string EvNo = "FT.BRANCH.REJECTED";

    private readonly FrameworkDbContext _db;
    private readonly EventPublisherService _publisher;
    private readonly FtDutyAccess _duty;
    private readonly FtBranchAdminService _posts;
    private readonly FtOpLogService _log;

    public FtBranchAdminApplyService(
        FrameworkDbContext db,
        EventPublisherService publisher,
        FtDutyAccess duty,
        FtBranchAdminService posts,
        FtOpLogService log)
    {
        _db = db;
        _publisher = publisher;
        _duty = duty;
        _posts = posts;
        _log = log;
    }

    public async Task<(IReadOnlyList<FtBranchApplyRowVm> page, int total, int pages, int pageOut)> GetIndexPageAsync(
        int userId, string? status1, int page, int pageSize, CancellationToken ct)
    {
        if (!await DatabaseSchemaHelper.TableExistsAsync(_db, "FamilyTree_BranchAdminApply", ct))
            return (Array.Empty<FtBranchApplyRowVm>(), 0, 1, 1);
        var super = await _duty.IsSuperAsync(userId, ct);
        var rows = await _db.FtBranchAdminApplies.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(ct);
        if (!super)
            rows = rows.Where(x => x.ApplyUserId == userId).ToList();
        if (!string.IsNullOrWhiteSpace(status1))
            rows = rows.Where(x => string.Equals(x.ApplyStatus, status1, StringComparison.OrdinalIgnoreCase)).ToList();
        rows = rows.OrderByDescending(x => x.CreateDate).ToList();
        var users = await _db.EUsers.AsNoTracking().Where(x => !x.IsDeleted).ToDictionaryAsync(x => x.DataId, x => x, ct);
        var (slice, total, pages, p) = FtPaging.Page(rows, page, pageSize);
        var vm = slice.Select(x => ToRow(x, users)).ToList();
        return (vm, total, pages, p);
    }

    public async Task<List<FtBranchApplyRowVm>> ListPendingAsync(int viewerUserId, CancellationToken ct)
    {
        if (!await DatabaseSchemaHelper.TableExistsAsync(_db, "FamilyTree_BranchAdminApply", ct))
            return [];
        var rows = await _db.FtBranchAdminApplies.AsNoTracking()
            .Where(x => !x.IsDeleted && x.ApplyStatus == "PENDING")
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync(ct);
        if (!await _duty.IsSuperAsync(viewerUserId, ct))
        {
            var myClan = await _db.FtUserClans.AsNoTracking()
                .Where(x => !x.IsDeleted && x.UserId == viewerUserId)
                .Select(x => (int?)x.ClanId)
                .FirstOrDefaultAsync(ct);
            if (myClan == null) return [];
            var clanUserIds = await _db.FtUserClans.AsNoTracking()
                .Where(x => !x.IsDeleted && x.ClanId == myClan)
                .Select(x => x.UserId)
                .ToListAsync(ct);
            var set = clanUserIds.ToHashSet();
            rows = rows.Where(x => set.Contains(x.ApplyUserId)).ToList();
        }
        var users = await _db.EUsers.AsNoTracking().Where(x => !x.IsDeleted).ToDictionaryAsync(x => x.DataId, x => x, ct);
        return rows.Select(x => ToRow(x, users)).ToList();
    }

    public async Task<FtBranchApplyStatusVm> StatusForAsync(int userId, CancellationToken ct)
    {
        var isBa = await _duty.IsBranchAdminAsync(userId, ct);
        var isSuper = await _duty.IsSuperAsync(userId, ct);
        if (!await DatabaseSchemaHelper.TableExistsAsync(_db, "FamilyTree_BranchAdminApply", ct))
            return new FtBranchApplyStatusVm { IsBranchAdmin = isBa, IsSuperAdmin = isSuper };
        var pending = await _db.FtBranchAdminApplies.AsNoTracking()
            .Where(x => !x.IsDeleted && x.ApplyUserId == userId && x.ApplyStatus == "PENDING")
            .OrderByDescending(x => x.CreateDate)
            .FirstOrDefaultAsync(ct);
        return new FtBranchApplyStatusVm
        {
            IsBranchAdmin = isBa,
            IsSuperAdmin = isSuper,
            HasPending = pending != null,
            PendingId = pending?.DataId,
            PendingReason = pending?.ApplyReason
        };
    }

    public async Task<string> ApplyAsync(int userId, string? reason, string op, CancellationToken ct)
    {
        if (!await DatabaseSchemaHelper.TableExistsAsync(_db, "FamilyTree_BranchAdminApply", ct))
            throw new InvalidOperationException("请先执行 scripts/29-CreateTbl_FamilyTree_Core.sql（含 FamilyTree_BranchAdminApply 表），以及 scripts/31-Seed_FtBranchAdminApply.sql。");
        if (await _duty.IsSuperAsync(userId, ct))
            throw new InvalidOperationException("超管无需申请支链管理员。");
        if (await _duty.IsBranchAdminAsync(userId, ct))
            throw new InvalidOperationException("你已是支链管理员或族谱管理员。");
        var inClan = await _db.FtUserClans.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.UserId == userId, ct);
        if (!inClan)
            throw new InvalidOperationException("请先创建或加入一个家族，再申请本家族的支链管理员。");
        var pending = await _db.FtBranchAdminApplies.AnyAsync(
            x => !x.IsDeleted && x.ApplyUserId == userId && x.ApplyStatus == "PENDING", ct);
        if (pending)
            throw new InvalidOperationException("已有待审申请，请等待族谱管理员处理。");

        var why = FtText.ClipReq(reason, 512);
        if (why.Length == 0)
            throw new InvalidOperationException("请填写申请理由。");

        var user = await _db.EUsers.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == userId && !x.IsDeleted, ct);
        var name = user == null ? $"#{userId}" : $"{user.RealName}({user.LoginId})";
        var now = DateTime.Now;
        var row = new FtBranchAdminApply
        {
            ApplyUserId = userId,
            ApplyReason = why,
            ApplyStatus = "PENDING",
            CreateDate = now,
            AmendDate = now,
            OperatorName = FtText.ClipReq(op, 30)
        };
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        _db.FtBranchAdminApplies.Add(row);
        await _db.SaveChangesAsync(ct);
        var pub = await _publisher.PublishAsync(new EventPublishRequest
        {
            AppCode = "FamilyTree",
            EventCode = EvApply,
            ObjectType = ObjectType,
            ObjectKey = row.DataId.ToString(),
            ObjectTitle = $"分支管理员申请：{name}",
            ObjectUrl = "/FtBranchAdmin/Index",
            TriggerUserId = userId,
            IdempotencyKey = $"FT-BRANCH-APPLY-{row.DataId}",
            OccurredTime = now
        }, ct);
        if (!pub.Success)
        {
            await tx.RollbackAsync(ct);
            throw new InvalidOperationException("申请发布失败：" + pub.Message);
        }
        await _log.WriteAsync("BRANCH_APPLY", ObjectType, row.DataId.ToString(), userId, op, why, null, null, ct);
        await tx.CommitAsync(ct);
        return "已提交申请，等待本家族的族谱管理员审批。";
    }

    public async Task<(bool Ok, string Msg)> ApproveAsync(int id, bool pass, int userId, string op, string? remark, CancellationToken ct)
    {
        if (!await DatabaseSchemaHelper.TableExistsAsync(_db, "FamilyTree_BranchAdminApply", ct))
            return (false, "请先执行 FamilyTree_BranchAdminApply 建表脚本。");

        var row = await _db.FtBranchAdminApplies.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return (false, "申请不存在。");
        if (row.ApplyStatus != "PENDING") return (false, "该申请已处理。");
        if (row.ApplyUserId == userId && !await _duty.AllowSelfApproveAsync(ct))
            return (false, "不能审批自己的申请。");

        var isSuper = await _duty.IsSuperAsync(userId, ct);
        var isClanAdmin = await _duty.IsClanAdminAsync(userId, ct);
        if (isSuper && !isClanAdmin)
            return (false, "超管不审批支链管理员，请由该家族的族谱管理员处理。");
        if (!isClanAdmin)
            return (false, "仅族谱管理员可审批支链管理员申请。");

        {
            var adminClan = await _db.FtUserClans.AsNoTracking()
                .Where(x => !x.IsDeleted && x.UserId == userId)
                .Select(x => (int?)x.ClanId)
                .FirstOrDefaultAsync(ct);
            var applyClan = await _db.FtUserClans.AsNoTracking()
                .Where(x => !x.IsDeleted && x.UserId == row.ApplyUserId)
                .Select(x => (int?)x.ClanId)
                .FirstOrDefaultAsync(ct);
            if (adminClan == null || applyClan == null || adminClan != applyClan)
                return (false, "只能审批本家族成员的支链管理员申请。");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            row.AuditUserId = userId;
            row.Remark = FtText.Clip(remark, 512);
            row.AmendDate = DateTime.Now;
            row.OperatorName = FtText.ClipReq(op, 30);
            if (pass)
            {
                row.ApplyStatus = "APPROVED";
                await _posts.SetAsync(row.ApplyUserId, true, userId, op, ct);
            }
            else
                row.ApplyStatus = "REJECTED";

            await _db.SaveChangesAsync(ct);
            var pub = await _publisher.PublishAsync(new EventPublishRequest
            {
                AppCode = "FamilyTree",
                EventCode = pass ? EvOk : EvNo,
                ObjectType = ObjectType,
                ObjectKey = row.DataId.ToString(),
                ObjectTitle = pass ? "支链管理员申请已通过" : "支链管理员申请已驳回",
                ObjectUrl = "/FtBranchApply/Index",
                TriggerUserId = row.ApplyUserId,
                IdempotencyKey = $"FT-BRANCH-{row.ApplyStatus}-{row.DataId}",
                OccurredTime = DateTime.Now
            }, ct);
            if (!pub.Success)
            {
                await tx.RollbackAsync(ct);
                return (false, "事件发布失败：" + pub.Message);
            }
            if (row.ApplyUserId == userId)
                await _log.WriteAsync("SELF_APPROVE", ObjectType, row.DataId.ToString(), userId, op, "仅一名超管自审", null, null, ct);
            await _log.WriteAsync(pass ? "BRANCH_OK" : "BRANCH_NO", ObjectType, row.DataId.ToString(), userId, op, remark, null, row.ApplyStatus, ct);
            await tx.CommitAsync(ct);
            return (true, pass ? "已通过并上岗。" : "已驳回。");
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static FtBranchApplyRowVm ToRow(FtBranchAdminApply x, Dictionary<int, EUser> users)
    {
        users.TryGetValue(x.ApplyUserId, out var u);
        return new FtBranchApplyRowVm
        {
            DataId = x.DataId,
            ApplyUserId = x.ApplyUserId,
            LoginId = u?.LoginId ?? "",
            RealName = u?.RealName ?? $"#{x.ApplyUserId}",
            ApplyReason = x.ApplyReason,
            ApplyStatus = x.ApplyStatus,
            Remark = x.Remark,
            CreateDate = x.CreateDate
        };
    }
}
