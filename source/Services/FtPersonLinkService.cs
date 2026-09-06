using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public sealed class FtPersonLinkService
{
    public const string ObjectType = "PersonLink";
    public const string EvApply = "FT.LINK.APPLY";
    public const string EvOk = "FT.LINK.APPROVED";
    public const string EvNo = "FT.LINK.REJECTED";
    public const string EvUn = "FT.LINK.UNLINKED";

    private readonly FrameworkDbContext _db;
    private readonly EventPublisherService _publisher;
    private readonly FtMatchService _match;
    private readonly FtDutyAccess _duty;
    private readonly FtOpLogService _log;
    private readonly FtClanService _clans;

    public FtPersonLinkService(
        FrameworkDbContext db,
        EventPublisherService publisher,
        FtMatchService match,
        FtDutyAccess duty,
        FtOpLogService log,
        FtClanService clans)
    {
        _db = db;
        _publisher = publisher;
        _match = match;
        _duty = duty;
        _log = log;
        _clans = clans;
    }

    public async Task<(IReadOnlyList<FtLinkListRowVm> page, int total, int pages, int pageOut)> GetIndexPageAsync(
        int userId, string? status1, int page, int pageSize, CancellationToken ct)
    {
        // 超管/分支管看全部（审批）；普通族人只看自己的申请
        var canAuditAll = await _duty.IsBranchAdminAsync(userId, ct);
        var rows = await _db.FtPersonLinks.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(ct);
        if (!canAuditAll)
            rows = rows.Where(x => x.ApplyUserId == userId).ToList();
        if (!string.IsNullOrWhiteSpace(status1))
            rows = rows.Where(x => string.Equals(x.LinkStatus, status1, StringComparison.OrdinalIgnoreCase)).ToList();
        rows = rows.OrderByDescending(x => x.CreateDate).ToList();
        var persons = await _db.FtPersons.AsNoTracking().Where(x => !x.IsDeleted).ToDictionaryAsync(x => x.DataId, x => x.FullName, ct);
        var (slice, total, pages, p) = FtPaging.Page(rows, page, pageSize);
        var vm = slice.Select(x => new FtLinkListRowVm
        {
            DataId = x.DataId,
            SourceName = persons.GetValueOrDefault(x.SourcePersonId, x.SourcePersonId.ToString()),
            TargetName = persons.GetValueOrDefault(x.TargetMainPersonId, x.TargetMainPersonId.ToString()),
            LinkStatus = x.LinkStatus,
            MatchLevel = x.MatchLevel,
            CreateDate = x.CreateDate
        }).ToList();
        return (vm, total, pages, p);
    }

    public static string LevelLabel(byte? level) => level switch
    {
        1 => "高置信（姓名+父母+出生全文）",
        2 => "中置信（姓名+父母/出生年）",
        3 => "低置信（姓名+父名，母名一方空）",
        4 => "低置信（姓名+出生年，请人工核对）",
        _ => "未分级"
    };

    public async Task<FtLinkConfirmVm> BuildConfirmAsync(int sourceId, int targetId, byte? level, int userId, CancellationToken ct)
    {
        var src = await _db.FtPersons.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == sourceId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("源人物不存在。");
        var tgt = await _db.FtPersons.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == targetId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("目标人物不存在。");

        async Task<string?> ParentNameAsync(int? id)
        {
            if (!id.HasValue) return null;
            var p = await _db.FtPersons.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id.Value && !x.IsDeleted, ct);
            return p?.FullName;
        }

        var vm = new FtLinkConfirmVm
        {
            SourceId = sourceId,
            TargetId = targetId,
            MatchLevel = level,
            LevelLabel = LevelLabel(level),
            Source = new FtLinkSideVm
            {
                PersonId = src.DataId,
                FullName = src.FullName,
                FatherName = src.FatherName,
                MotherName = src.MotherName,
                BirthDate = src.BirthDate,
                BirthYear = src.BirthYear,
                Gender = src.Gender == 2 ? "女" : src.Gender == 1 ? "男" : "—",
                InMain = src.InMainGenealogy,
                FatherOnTree = await ParentNameAsync(src.FatherPersonId),
                MotherOnTree = await ParentNameAsync(src.MotherPersonId)
            },
            Target = new FtLinkSideVm
            {
                PersonId = tgt.DataId,
                FullName = tgt.FullName,
                FatherName = tgt.FatherName,
                MotherName = tgt.MotherName,
                BirthDate = tgt.BirthDate,
                BirthYear = tgt.BirthYear,
                Gender = tgt.Gender == 2 ? "女" : tgt.Gender == 1 ? "男" : "—",
                InMain = tgt.InMainGenealogy,
                FatherOnTree = await ParentNameAsync(tgt.FatherPersonId),
                MotherOnTree = await ParentNameAsync(tgt.MotherPersonId)
            }
        };

        var kids = await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.FatherPersonId == sourceId || x.MotherPersonId == sourceId))
            .Select(x => x.FullName)
            .ToListAsync(ct);
        vm.ChildrenToAttach = kids;

        if (!tgt.InMainGenealogy)
        {
            vm.CanSubmit = false;
            vm.BlockReason = "目标尚未入主谱。两棵未入主谱的小树不合并，请等对方已在主谱后再链入。";
            return vm;
        }
        if (src.SameAsPersonId.HasValue)
        {
            vm.CanSubmit = false;
            vm.BlockReason = "此人已链入主谱，不可重复链入。";
            return vm;
        }
        var pending = await _db.FtPersonLinks.AnyAsync(x => !x.IsDeleted && x.SourcePersonId == sourceId && x.LinkStatus == "PENDING", ct);
        if (pending)
        {
            vm.CanSubmit = false;
            vm.BlockReason = "已有待审链入单，请到「我的链入」查看。";
            return vm;
        }

        if (!await _duty.IsSuperAsync(userId, ct) && !await _duty.IsBranchAdminAsync(userId, ct))
        {
            var self = await _db.FtPersons.AsNoTracking()
                .FirstOrDefaultAsync(x => x.BindUserId == userId && !x.IsDeleted, ct);
            if (self == null || !self.IsCertified)
            {
                vm.CanSubmit = false;
                vm.BlockReason = "须先完成族员认证，才可申请链入。";
                return vm;
            }
        }

        var (conflict, detail) = await _match.CheckAncestorConflictAsync(sourceId, targetId, ct);
        vm.HasAncestorConflict = conflict;
        vm.ConflictDetail = detail;
        if (conflict)
        {
            vm.CanSubmit = false;
            vm.BlockReason = "向上世系与主谱不一致，提交后将记入冲突单，不进入链入待审。" + (string.IsNullOrEmpty(detail) ? "" : " " + detail);
            return vm;
        }

        vm.CanSubmit = true;
        return vm;
    }

    public async Task<(int LinkId, string Message)> ApplyAsync(int sourceId, int targetId, int userId, string op, byte? level, CancellationToken ct)
    {
        var src = await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == sourceId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("源人物不存在。");
        var tgt = await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == targetId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("目标人物不存在。");
        if (!await _duty.IsSuperAsync(userId, ct) && !await _duty.IsBranchAdminAsync(userId, ct))
        {
            var self = await _db.FtPersons.AsNoTracking()
                .FirstOrDefaultAsync(x => x.BindUserId == userId && !x.IsDeleted, ct);
            if (self == null || !self.IsCertified)
                throw new InvalidOperationException("须先完成族员认证，才可申请链入主谱。");
        }
        if (!tgt.InMainGenealogy)
            throw new InvalidOperationException("只能链入已经在主谱中的人物。");
        if (src.DataId == tgt.DataId)
            throw new InvalidOperationException("不能链入自己。");
        var dup = await _db.FtPersonLinks.AnyAsync(x => !x.IsDeleted && x.SourcePersonId == sourceId && x.LinkStatus == "EFFECTIVE", ct);
        if (dup) throw new InvalidOperationException("该人已链入主谱。");
        var pending = await _db.FtPersonLinks.AnyAsync(x => !x.IsDeleted && x.SourcePersonId == sourceId && x.LinkStatus == "PENDING", ct);
        if (pending) throw new InvalidOperationException("已有待审链入单。");

        var (conflict, detail) = await _match.CheckAncestorConflictAsync(sourceId, targetId, ct);
        if (conflict)
        {
            await _match.OpenConflictAsync(sourceId, targetId, "ANCESTOR_UP", detail ?? "向上不一致", op, userId, ct);
            return (0, "向上世系不一致，已提交超管冲突单，未自动申请链入。");
        }

        var now = DateTime.Now;
        // 待审期间源人不得占主谱位
        if (src.InMainGenealogy)
        {
            src.InMainGenealogy = false;
            src.AmendDate = now;
        }
        var row = new FtPersonLink
        {
            SourcePersonId = sourceId,
            TargetMainPersonId = targetId,
            ApplyUserId = userId,
            LinkStatus = "PENDING",
            MatchLevel = level,
            CreateDate = now,
            AmendDate = now,
            OperatorName = FtText.ClipReq(op, 30)
        };
        // 草稿归档会把「建人物 → 建链接」串成一件事，此时外层已开事务，这里不再另开一个，
        // 否则一个逻辑操作被劈成两个事务，中间失败就会留下半成品。
        var ownsTx = _db.Database.CurrentTransaction == null;
        var tx = ownsTx ? await _db.Database.BeginTransactionAsync(ct) : null;
        _db.FtPersonLinks.Add(row);
        await _db.SaveChangesAsync(ct);
        var pub = await _publisher.PublishAsync(new EventPublishRequest
        {
            AppCode = "FamilyTree",
            EventCode = EvApply,
            ObjectType = ObjectType,
            ObjectKey = row.DataId.ToString(),
            ObjectTitle = $"{src.FullName} → {tgt.FullName}",
            ObjectUrl = $"/FtLinkAudit/Index",
            TriggerUserId = userId,
            IdempotencyKey = $"FT-LINK-APPLY-{row.DataId}",
            OccurredTime = now
        }, ct);
        if (!pub.Success)
        {
            if (ownsTx) await tx!.RollbackAsync(ct);
            throw new InvalidOperationException("链入申请发布失败：" + pub.Message);
        }
        if (ownsTx) await tx!.CommitAsync(ct);
        return (row.DataId, "已提交链入申请（待审）。请把二维码发给超管或分支管扫码审批；通过前不会挂入主谱。");
    }

    public async Task<FtLinkScanVm?> BuildScanAsync(int linkId, CancellationToken ct)
    {
        var row = await _db.FtPersonLinks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == linkId && !x.IsDeleted, ct);
        if (row == null) return null;
        var src = await _db.FtPersons.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == row.SourcePersonId && !x.IsDeleted, ct);
        var tgt = await _db.FtPersons.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == row.TargetMainPersonId && !x.IsDeleted, ct);
        return new FtLinkScanVm
        {
            LinkId = row.DataId,
            LinkStatus = row.LinkStatus,
            MatchLevel = row.MatchLevel,
            LevelLabel = LevelLabel(row.MatchLevel),
            ApplyUserId = row.ApplyUserId,
            CreateDate = row.CreateDate,
            SourceName = src?.FullName ?? ("#" + row.SourcePersonId),
            SourceFather = src?.FatherName,
            SourceMother = src?.MotherName,
            SourceBirth = src?.BirthDate,
            SourceId = row.SourcePersonId,
            TargetName = tgt?.FullName ?? ("#" + row.TargetMainPersonId),
            TargetFather = tgt?.FatherName,
            TargetMother = tgt?.MotherName,
            TargetBirth = tgt?.BirthDate,
            TargetId = row.TargetMainPersonId
        };
    }

    /// <summary>
    /// 查看链入申请（二维码页 / 扫码审批页）的归属校验。
    /// 申请人自己可看；管理岗只能看本族的单据——否则枚举 id 就能读到任意族两侧人物的姓名、父母、生日。
    /// </summary>
    public async Task EnsureApplicantCanViewQrAsync(int linkId, int userId, CancellationToken ct)
    {
        var row = await _db.FtPersonLinks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == linkId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("申请单不存在。");
        if (row.ApplyUserId == userId) return;
        if (await _duty.IsBranchAdminAsync(userId, ct) && await SameClanAsLinkAsync(row, userId, ct)) return;
        throw new InvalidOperationException("无权查看该申请二维码。");
    }

    /// <summary>申请单两侧人物是否都在调用者所属家族内。任一侧不同族即拒绝。</summary>
    private async Task<bool> SameClanAsLinkAsync(FtPersonLink row, int userId, CancellationToken ct)
    {
        var src = await _db.FtPersons.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == row.SourcePersonId && !x.IsDeleted, ct);
        var tgt = await _db.FtPersons.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == row.TargetMainPersonId && !x.IsDeleted, ct);
        if (src == null || tgt == null) return false;
        return await _clans.UserSharesClanAsync(userId, src, ct)
            && await _clans.UserSharesClanAsync(userId, tgt, ct);
    }

    /// <summary>
    /// 审批链入。授权判据全部在这里，不依赖视图层的 ViewBag.CanApprove：
    /// 必须是管理岗，且申请单两侧人物都在审批人所属家族内。
    /// 状态判断与人物读取放在事务内，配合 rowversion 拦住两名管理员并发审批同一单。
    /// </summary>
    public async Task<(bool Ok, string Msg)> ApproveAsync(int id, bool pass, int userId, string op, string? remark, CancellationToken ct)
    {
        if (!await _duty.IsBranchAdminAsync(userId, ct))
            return (false, "仅族谱管理员、支链管理员或超管可审批链入申请。");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var row = await _db.FtPersonLinks.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
            if (row == null) { await tx.RollbackAsync(ct); return (false, "单据不存在。"); }
            if (row.LinkStatus != "PENDING") { await tx.RollbackAsync(ct); return (false, "该单已处理。"); }
            if (row.ApplyUserId == userId)
            {
                // 仅「系统只有一名超管」时允许该超管自审；分支管不得自审自己的申请
                var isSuper = await _duty.IsSuperAsync(userId, ct);
                if (!isSuper || !await _duty.AllowSelfApproveAsync(ct))
                {
                    await tx.RollbackAsync(ct);
                    return (false, "不能审批自己的链入申请。");
                }
            }

            var src = await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == row.SourcePersonId && !x.IsDeleted, ct);
            var tgt = await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == row.TargetMainPersonId && !x.IsDeleted, ct);
            if (src == null || tgt == null) { await tx.RollbackAsync(ct); return (false, "人物不存在。"); }

            if (!await _clans.UserSharesClanAsync(userId, src, ct)
                || !await _clans.UserSharesClanAsync(userId, tgt, ct))
            {
                await tx.RollbackAsync(ct);
                return (false, "该申请不属于您所在的家族，无权审批。");
            }

            row.AuditSuperAdminId = userId;
            row.Remark = remark;
            row.AmendDate = DateTime.Now;
            row.OperatorName = FtText.ClipReq(op, 30);
            if (pass)
            {
                var (conflict, detail) = await _match.CheckAncestorConflictAsync(src.DataId, tgt.DataId, ct);
                if (conflict)
                {
                    await tx.RollbackAsync(ct);
                    return (false, "仍有向上冲突：" + detail);
                }
                row.LinkStatus = "EFFECTIVE";
                src.SameAsPersonId = tgt.DataId;
                src.InMainGenealogy = false;
                src.KeyLocked = true;
                tgt.EditLock = true;
                // 本人绑定迁到主谱目标，避免列表仍落在已等同的源人上
                if (src.BindUserId.HasValue && !tgt.BindUserId.HasValue)
                {
                    tgt.BindUserId = src.BindUserId;
                    src.BindUserId = null;
                }
                await AttachChildrenAsync(src.DataId, tgt.DataId, ct);
                await AttachAncestorsIfNoneAsync(src, tgt, ct);
            }
            else
                row.LinkStatus = "REJECTED";

            await _db.SaveChangesAsync(ct);
            var ev = pass ? EvOk : EvNo;
            var pub = await _publisher.PublishAsync(new EventPublishRequest
            {
                AppCode = "FamilyTree",
                EventCode = ev,
                ObjectType = ObjectType,
                ObjectKey = row.DataId.ToString(),
                ObjectTitle = pass ? "链入通过" : "链入驳回",
                ObjectUrl = "/FtPersonLink/Index",
                TriggerUserId = row.ApplyUserId,
                IdempotencyKey = $"FT-LINK-{row.LinkStatus}-{row.DataId}",
                OccurredTime = DateTime.Now
            }, ct);
            if (!pub.Success)
            {
                await tx.RollbackAsync(ct);
                return (false, "事件发布失败：" + pub.Message);
            }
            if (row.ApplyUserId == userId)
                await _log.WriteAsync("SELF_APPROVE", ObjectType, row.DataId.ToString(), userId, op, "仅一名超管自审", null, null, ct);
            await tx.CommitAsync(ct);
            return (true, pass ? "已通过。" : "已驳回。");
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            return (false, "该单已被他人处理，请刷新后再看。");
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<(bool Ok, string Msg)> UnlinkAsync(int id, int userId, string op, CancellationToken ct)
    {
        if (!await _duty.IsSuperAsync(userId, ct)) return (false, "仅超管可解链。");
        var row = await _db.FtPersonLinks.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return (false, "单据不存在。");
        if (row.LinkStatus != "EFFECTIVE") return (false, "仅生效中的链入可解链。");
        var src = await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == row.SourcePersonId && !x.IsDeleted, ct);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        row.LinkStatus = "UNLINK";
        row.UnlinkTime = DateTime.Now;
        row.AmendDate = DateTime.Now;
        if (src != null)
        {
            src.SameAsPersonId = null;
            src.InMainGenealogy = false;
            src.KeyLocked = false;
        }
        await _db.SaveChangesAsync(ct);
        var pub = await _publisher.PublishAsync(new EventPublishRequest
        {
            AppCode = "FamilyTree",
            EventCode = EvUn,
            ObjectType = ObjectType,
            ObjectKey = row.DataId.ToString(),
            ObjectTitle = "已解链",
            ObjectUrl = "/FtPersonLink/Index",
            TriggerUserId = row.ApplyUserId,
            IdempotencyKey = $"FT-LINK-UNLINK-{row.DataId}-{row.UnlinkTime:yyyyMMddHHmmss}",
            OccurredTime = DateTime.Now
        }, ct);
        if (!pub.Success)
        {
            await tx.RollbackAsync(ct);
            return (false, "事件发布失败：" + pub.Message);
        }
        await _log.WriteAsync("UNLINK", ObjectType, row.DataId.ToString(), userId, op, null, null, null, ct);
        await tx.CommitAsync(ct);
        return (true, "已解链。");
    }

    private async Task AttachChildrenAsync(int sourceId, int targetId, CancellationToken ct)
    {
        var kids = await _db.FtPersons.Where(x => !x.IsDeleted && (x.FatherPersonId == sourceId || x.MotherPersonId == sourceId)).ToListAsync(ct);
        foreach (var k in kids)
        {
            if (k.FatherPersonId == sourceId) k.FatherPersonId = targetId;
            if (k.MotherPersonId == sourceId) k.MotherPersonId = targetId;
            k.InMainGenealogy = true;
            k.KeyLocked = true;
        }
    }

    private async Task AttachAncestorsIfNoneAsync(FtPerson src, FtPerson tgt, CancellationToken ct)
    {
        // 链入时若主谱目标向上缺父，把源侧已有父链逐代补挂（不覆盖目标已有父亲）
        // guard++ < 40 只是跳数上限，不是环检测：这里对每条要落的边先做一次可达性判定。
        var graph = await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToDictionaryAsync(x => x.DataId, ct);

        var t = tgt;
        var srcFatherId = src.FatherPersonId;
        var guard = 0;
        while (guard++ < 40 && !t.FatherPersonId.HasValue && srcFatherId.HasValue)
        {
            var srcFather = await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == srcFatherId.Value && !x.IsDeleted, ct);
            if (srcFather == null) break;

            var attach = srcFather;
            if (srcFather.SameAsPersonId is int sameId)
            {
                var mapped = await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == sameId && !x.IsDeleted, ct);
                if (mapped != null) attach = mapped;
            }

            if (FtTreeService.WouldCycle(graph, t.DataId, attach.DataId)) break;

            attach.InMainGenealogy = true;
            attach.KeyLocked = true;
            t.FatherPersonId = attach.DataId;
            if (graph.TryGetValue(t.DataId, out var snap)) snap.FatherPersonId = attach.DataId;

            srcFatherId = srcFather.FatherPersonId;
            t = attach;
        }
    }
}
