using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>家族：创建、加入（一人一家）、可见范围、族谱管理员任免。</summary>
public sealed class FtClanService
{
    private readonly FrameworkDbContext _db;
    private readonly FtDutyAccess _duty;
    private readonly FtOpLogService _log;

    public FtClanService(FrameworkDbContext db, FtDutyAccess duty, FtOpLogService log)
    {
        _db = db;
        _duty = duty;
        _log = log;
    }

    public async Task<bool> SchemaReadyAsync(CancellationToken ct) =>
        await DatabaseSchemaHelper.TableExistsAsync(_db, "FamilyTree_Clan", ct)
        && await DatabaseSchemaHelper.TableExistsAsync(_db, "FamilyTree_UserClan", ct);

    public async Task<bool> CreateInviteSchemaReadyAsync(CancellationToken ct) =>
        await DatabaseSchemaHelper.TableExistsAsync(_db, "FamilyTree_ClanCreateInvite", ct);

    public async Task<int?> GetUserClanIdAsync(int userId, CancellationToken ct)
    {
        if (!await SchemaReadyAsync(ct)) return null;
        return await _db.FtUserClans.AsNoTracking()
            .Where(x => !x.IsDeleted && x.UserId == userId && x.BStatus == "1")
            .Select(x => (int?)x.ClanId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<FtClan?> GetUserClanAsync(int userId, CancellationToken ct)
    {
        var id = await GetUserClanIdAsync(userId, ct);
        if (id == null) return null;
        return await _db.FtClans.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
    }

    public async Task<FtClan?> GetByCodeAsync(string? code, CancellationToken ct)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();
        if (c.Length == 0) return null;
        return await _db.FtClans.AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.ClanCode == c, ct);
    }

    public async Task<FtClan?> GetAsync(int clanId, CancellationToken ct) =>
        await _db.FtClans.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == clanId && !x.IsDeleted, ct);

    /// <summary>超管或族谱管理员：生成「创建新家族」邀请码（7 天有效，一码一次）。</summary>
    public async Task<(bool Ok, string Msg, FtClanCreateInvite? Invite)> CreateClanCreateInviteAsync(
        int userId, string op, string? remark, CancellationToken ct)
    {
        if (!await CreateInviteSchemaReadyAsync(ct))
            return (false, "请先执行 scripts/47-ClanCreateInvite.sql。", null);
        if (!await _duty.CanManageClansAsync(userId, ct))
            return (false, "仅超管或族谱管理员可发起「创建新家族」邀请。", null);

        var now = DateTime.Now;
        var inviteCode = await NewInviteCodeAsync(ct);
        var invite = new FtClanCreateInvite
        {
            InviteCode = inviteCode,
            CreatedByUserId = userId,
            ExpireAt = now.AddDays(7),
            InviteStatus = "OPEN",
            Remark = FtText.Clip(remark, 128),
            BStatus = "1",
            CreateDate = now,
            AmendDate = now,
            OperatorName = FtText.ClipReq(op, 30)
        };
        _db.FtClanCreateInvites.Add(invite);
        await _db.SaveChangesAsync(ct);
        await _log.WriteAsync("CLAN_CREATE_INVITE", "ClanCreateInvite", invite.DataId.ToString(),
            userId, op, inviteCode, null, null, ct);
        return (true, "已生成创建新家族邀请码（7 天内有效，使用一次即失效）。", invite);
    }

    public async Task<FtClanCreateInvite?> FindOpenCreateInviteAsync(string? code, CancellationToken ct)
    {
        if (!await CreateInviteSchemaReadyAsync(ct)) return null;
        var c = (code ?? "").Trim().ToUpperInvariant();
        if (c.Length == 0) return null;
        var inv = await _db.FtClanCreateInvites
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.InviteCode == c && x.InviteStatus == "OPEN", ct);
        if (inv == null || inv.ExpireAt < DateTime.Now) return null;
        return inv;
    }

    /// <summary>检查创建家族邀请码是否可用；不可用时返回面向用户的提示文案。</summary>
    public async Task<(bool Valid, string? Hint)> CheckCreateInviteAsync(string? code, CancellationToken ct)
    {
        if (!await CreateInviteSchemaReadyAsync(ct)) return (false, null);
        var c = (code ?? "").Trim().ToUpperInvariant();
        if (c.Length == 0) return (false, null);

        var inv = await _db.FtClanCreateInvites.AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.InviteCode == c, ct);
        if (inv == null)
            return (false, "邀请码无效，请核对后重新输入。");

        if (string.Equals(inv.InviteStatus, "USED", StringComparison.OrdinalIgnoreCase))
        {
            string? clanName = null;
            if (inv.UsedClanId != null)
            {
                clanName = await _db.FtClans.AsNoTracking()
                    .Where(x => x.DataId == inv.UsedClanId && !x.IsDeleted)
                    .Select(x => x.ClanName)
                    .FirstOrDefaultAsync(ct);
            }
            var msg = !string.IsNullOrWhiteSpace(clanName)
                ? "该邀请码已被使用，已绑定家族「" + clanName + "」，不可重复绑定。"
                : "该邀请码已被使用，不可重复绑定。";
            return (false, msg);
        }

        if (inv.ExpireAt < DateTime.Now)
            return (false, "该邀请码已过期，请联系族谱管理员重新获取。");

        if (!string.Equals(inv.InviteStatus, "OPEN", StringComparison.OrdinalIgnoreCase))
            return (false, "该邀请码当前不可用。");

        return (true, null);
    }

    /// <summary>创建家族：须凭有效邀请码（超管可直接建）；创建者成为该新家族的族谱管理员。</summary>
    public async Task<(bool Ok, string Msg, FtClan? Clan)> CreateAsync(
        int userId, string? clanName, string op, string? inviteCode, CancellationToken ct)
    {
        if (!await SchemaReadyAsync(ct))
            return (false, "请先执行 scripts/43-Clan.sql。", null);
        if (await GetUserClanIdAsync(userId, ct) != null)
            return (false, "您已加入一个家族，每人只能加入一个家族。", null);

        var isSuper = await _duty.IsSuperAsync(userId, ct);
        FtClanCreateInvite? invite = null;
        if (!isSuper)
        {
            if (!await CreateInviteSchemaReadyAsync(ct))
                return (false, "请先执行 scripts/47-ClanCreateInvite.sql。", null);
            invite = await FindOpenCreateInviteAsync(inviteCode, ct);
            if (invite == null)
            {
                var (_, hint) = await CheckCreateInviteAsync(inviteCode, ct);
                return (false, hint ?? "创建新家族须凭超管或族谱管理员发出的邀请码。请扫邀请二维码或填写邀请码。", null);
            }
        }
        else if (!string.IsNullOrWhiteSpace(inviteCode))
        {
            invite = await FindOpenCreateInviteAsync(inviteCode, ct);
        }

        var rawName = (clanName ?? "").Trim();
        if (rawName.Length == 0)
            return (false, "请填写族谱名称。", null);
        var name = FtText.ClipReq(rawName, 64);
        var now = DateTime.Now;
        var code = await NewClanCodeAsync(ct);
        var clan = new FtClan
        {
            ClanCode = code,
            ClanName = name,
            OwnerUserId = userId,
            BStatus = "1",
            CreateDate = now,
            AmendDate = now,
            OperatorName = FtText.ClipReq(op, 30)
        };
        _db.FtClans.Add(clan);
        await _db.SaveChangesAsync(ct);

        await AttachUserAsync(userId, clan.DataId, op, now, ct);
        await StampOwnedPersonsAsync(userId, clan.DataId, ct);
        await SetClanAdminPostAsync(userId, true, op, ct);

        if (invite != null)
        {
            invite.InviteStatus = "USED";
            invite.UsedByUserId = userId;
            invite.UsedClanId = clan.DataId;
            invite.AmendDate = now;
            await _db.SaveChangesAsync(ct);
        }

        await _log.WriteAsync("CLAN_CREATE", "Clan", clan.DataId.ToString(), userId, op,
            name + "/" + code + (invite != null ? "/inv=" + invite.InviteCode : "/super"), null, null, ct);
        return (true, "家族已创建。请分享「加入家族」二维码邀请亲友加入。", clan);
    }

    /// <summary>凭家族 ID 加入；一人只能一个家族。</summary>
    public async Task<(bool Ok, string Msg)> JoinAsync(int userId, string? clanCode, string op, CancellationToken ct)
    {
        if (!await SchemaReadyAsync(ct))
            return (false, "请先执行 scripts/43-Clan.sql。");
        if (await GetUserClanIdAsync(userId, ct) != null)
            return (false, "您已加入一个家族，不能再加入其他家族。");

        var clan = await GetByCodeAsync(clanCode, ct);
        if (clan == null) return (false, "家族 ID 无效。");

        await AttachUserAsync(userId, clan.DataId, op, DateTime.Now, ct);
        await StampOwnedPersonsAsync(userId, clan.DataId, ct);
        await _log.WriteAsync("CLAN_JOIN", "Clan", clan.DataId.ToString(), userId, op, clan.ClanCode, null, null, ct);
        return (true, "已加入家族「" + clan.ClanName + "」。");
    }

    public async Task<HashSet<int>> GetClanPersonIdsAsync(int clanId, CancellationToken ct)
    {
        var ids = await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted && x.SameAsPersonId == null && x.ClanId == clanId)
            .Select(x => x.DataId)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task StampOwnedPersonsAsync(int userId, int clanId, CancellationToken ct)
    {
        var rows = await _db.FtPersons
            .Where(x => !x.IsDeleted && (x.OwnerUserId == userId || x.BindUserId == userId)
                        && (x.ClanId == null || x.ClanId == 0))
            .ToListAsync(ct);
        foreach (var p in rows)
            p.ClanId = clanId;
        if (rows.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    public async Task EnsurePersonClanAsync(FtPerson row, int userId, CancellationToken ct)
    {
        if (row.ClanId.HasValue && row.ClanId > 0) return;
        var clanId = await GetUserClanIdAsync(userId, ct);
        if (clanId != null) row.ClanId = clanId;
    }

    /// <summary>超管：为用户授予/撤销族谱管理员（须已在某家族）。</summary>
    public async Task<(bool Ok, string Msg)> SetClanAdminAsync(int targetUserId, bool grant, int opUserId, string op, CancellationToken ct)
    {
        if (!await _duty.IsSuperAsync(opUserId, ct))
            return (false, "仅超管可委任族谱管理员。");
        var clanId = await GetUserClanIdAsync(targetUserId, ct);
        if (clanId == null)
            return (false, "对方尚未加入任何家族，请先让其创建或加入家族。");
        await SetClanAdminPostAsync(targetUserId, grant, op, ct);
        await _log.WriteAsync(grant ? "CLAN_ADMIN_ON" : "CLAN_ADMIN_OFF", "Clan", clanId.Value.ToString(),
            opUserId, op, "user=" + targetUserId, null, null, ct);
        return (true, grant ? "已设为族谱管理员。" : "已取消族谱管理员。");
    }

    public async Task<List<FtClanAdminRowVm>> ListClansForAdminAsync(int userId, CancellationToken ct)
    {
        if (!await SchemaReadyAsync(ct)) return new List<FtClanAdminRowVm>();
        var isSuper = await _duty.IsSuperAsync(userId, ct);
        List<FtClan> clans;
        if (isSuper)
            clans = await _db.FtClans.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.ClanName).ToListAsync(ct);
        else
        {
            var my = await GetUserClanIdAsync(userId, ct);
            clans = my == null
                ? new List<FtClan>()
                : await _db.FtClans.AsNoTracking().Where(x => !x.IsDeleted && x.DataId == my).ToListAsync(ct);
        }

        var clanIds = clans.Select(x => x.DataId).ToList();
        var members = await _db.FtUserClans.AsNoTracking()
            .Where(x => !x.IsDeleted && clanIds.Contains(x.ClanId))
            .ToListAsync(ct);
        var userIds = members.Select(x => x.UserId).Distinct().ToList();
        var users = await _db.EUsers.AsNoTracking().Where(x => userIds.Contains(x.DataId)).ToDictionaryAsync(x => x.DataId, ct);
        var pos = await _db.EPositions.AsNoTracking().FirstOrDefaultAsync(x => x.PostCode == FtDutyAccess.ClanAdmin && !x.IsDeleted, ct);
        var clanAdmins = new HashSet<int>();
        if (pos != null)
        {
            var ups = await _db.EUserPositions.AsNoTracking()
                .Where(x => x.PosId == pos.DataId)
                .ToListAsync(ct);
            clanAdmins = ups.Where(x => EBStatusHelper.IsActiveBStatus(x.BStatus)).Select(x => x.UserId).ToHashSet();
        }

        return clans.Select(c =>
        {
            var mids = members.Where(m => m.ClanId == c.DataId).Select(m => m.UserId).ToList();
            users.TryGetValue(c.OwnerUserId, out var owner);
            return new FtClanAdminRowVm
            {
                ClanId = c.DataId,
                ClanCode = c.ClanCode,
                ClanName = c.ClanName,
                OwnerUserId = c.OwnerUserId,
                OwnerName = owner?.RealName ?? ("#" + c.OwnerUserId),
                MemberCount = mids.Count,
                ClanAdminNames = string.Join("、",
                    mids.Where(uid => clanAdmins.Contains(uid))
                        .Select(uid => users.TryGetValue(uid, out var u) ? u.RealName : "#" + uid))
            };
        }).ToList();
    }

    public async Task<List<FtClanMemberRowVm>> ListMembersAsync(int clanId, CancellationToken ct)
    {
        var rows = await _db.FtUserClans.AsNoTracking()
            .Where(x => !x.IsDeleted && x.ClanId == clanId)
            .ToListAsync(ct);
        var uids = rows.Select(x => x.UserId).ToList();
        var users = await _db.EUsers.AsNoTracking().Where(x => uids.Contains(x.DataId)).ToDictionaryAsync(x => x.DataId, ct);
        var posClan = await _db.EPositions.AsNoTracking().FirstOrDefaultAsync(x => x.PostCode == FtDutyAccess.ClanAdmin && !x.IsDeleted, ct);
        var posBa = await _db.EPositions.AsNoTracking().FirstOrDefaultAsync(x => x.PostCode == FtDutyAccess.BranchAdmin && !x.IsDeleted, ct);
        var clanAdmins = await ActiveUserIdsForPosAsync(posClan?.DataId ?? 0, ct);
        var branchAdmins = await ActiveUserIdsForPosAsync(posBa?.DataId ?? 0, ct);

        return rows.Select(r =>
        {
            users.TryGetValue(r.UserId, out var u);
            return new FtClanMemberRowVm
            {
                UserId = r.UserId,
                LoginId = u?.LoginId ?? "",
                RealName = u?.RealName ?? ("#" + r.UserId),
                JoinDate = r.JoinDate,
                IsClanAdmin = clanAdmins.Contains(r.UserId),
                IsBranchAdmin = branchAdmins.Contains(r.UserId)
            };
        }).OrderBy(x => x.RealName).ToList();
    }

    private async Task<HashSet<int>> ActiveUserIdsForPosAsync(int posId, CancellationToken ct)
    {
        if (posId <= 0) return new HashSet<int>();
        var ups = await _db.EUserPositions.AsNoTracking().Where(x => x.PosId == posId).ToListAsync(ct);
        return ups.Where(x => EBStatusHelper.IsActiveBStatus(x.BStatus)).Select(x => x.UserId).ToHashSet();
    }

    private async Task AttachUserAsync(int userId, int clanId, string op, DateTime now, CancellationToken ct)
    {
        _db.FtUserClans.Add(new FtUserClan
        {
            UserId = userId,
            ClanId = clanId,
            JoinDate = now,
            BStatus = "1",
            CreateDate = now,
            AmendDate = now,
            OperatorName = FtText.ClipReq(op, 30)
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetClanAdminPostAsync(int userId, bool grant, string op, CancellationToken ct)
    {
        var pos = await _db.EPositions.FirstOrDefaultAsync(x => x.PostCode == FtDutyAccess.ClanAdmin && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("未找到族谱管理员岗位，请先执行 scripts/44-Seed_FtClanAdmin.sql。");
        var deptId = await _db.EDepartments.AsNoTracking().Where(d => !d.IsDeleted).Select(d => d.DataId).FirstOrDefaultAsync(ct);
        if (deptId == 0) throw new InvalidOperationException("系统中没有任何部门，无法上岗。");
        var now = DateTime.Now;
        var up = await _db.EUserPositions.FirstOrDefaultAsync(x => x.UserId == userId && x.PosId == pos.DataId, ct);
        if (grant)
        {
            if (up == null)
            {
                _db.EUserPositions.Add(new EUserPosition
                {
                    UserId = userId,
                    PosId = pos.DataId,
                    DeptId = deptId,
                    IsPrimary = false,
                    BStatus = "1",
                    CreateDate = now,
                    AmendDate = now,
                    OperatorName = FtText.ClipReq(op, 8)
                });
            }
            else
            {
                up.BStatus = "1";
                up.AmendDate = now;
            }
        }
        else if (up != null)
        {
            up.BStatus = "2";
            up.AmendDate = now;
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task<string> NewClanCodeAsync(CancellationToken ct)
    {
        for (var i = 0; i < 20; i++)
        {
            var code = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var exists = await _db.FtClans.AsNoTracking().AnyAsync(x => !x.IsDeleted && x.ClanCode == code, ct);
            if (!exists) return code;
        }
        throw new InvalidOperationException("无法生成家族 ID，请重试。");
    }

    private async Task<string> NewInviteCodeAsync(CancellationToken ct)
    {
        for (var i = 0; i < 20; i++)
        {
            var code = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
            var exists = await _db.FtClanCreateInvites.AsNoTracking()
                .AnyAsync(x => !x.IsDeleted && x.InviteCode == code, ct);
            if (!exists) return code;
        }
        throw new InvalidOperationException("无法生成邀请码，请重试。");
    }
}
