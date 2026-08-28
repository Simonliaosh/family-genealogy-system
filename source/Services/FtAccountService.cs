using FamilyTree.Configuration;
using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FamilyTree.Services;

public sealed class FtAccountService
{
    private readonly FrameworkDbContext _db;
    private readonly FamilyTreeOptions _opt;
    private readonly FtPersonService _persons;

    public FtAccountService(FrameworkDbContext db, IOptions<FamilyTreeOptions> opt, FtPersonService persons)
    {
        _db = db;
        _opt = opt.Value;
        _persons = persons;
    }

    public string HashId(string idCard) => FtText.HashIdCard(idCard, _opt.IdCardHashSalt);

    public async Task<EUser?> FindUserByIdCardAsync(string idCard, CancellationToken ct)
    {
        if (!FtText.IsIdCard(idCard)) return null;
        var hash = HashId(idCard);
        var bind = await _db.FtAccountBinds.AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.IdCardHash == hash, ct);
        if (bind == null) return null;
        return await _db.EUsers.FirstOrDefaultAsync(x => x.DataId == bind.UserId && !x.IsDeleted, ct);
    }

    public async Task<(bool Ok, string Msg, EUser? User)> RegisterAsync(FtMemberRegisterVm m, CancellationToken ct)
    {
        m.LoginName = FtText.NormalizeLoginName(m.LoginName);
        m.RealName = FtText.ClipReq(m.RealName, 50);
        if (!FtText.IsMemberLoginName(m.LoginName))
            return (false, "登录名至少 6 位，只能用字母或数字（如手机号、拼音）。", null);
        if (m.RealName.Length == 0) return (false, "请输入姓名。", null);
        if ((m.Password ?? "").Trim().Length < 6) return (false, "密码至少 6 位。", null);
        if (m.Password != m.Password2) return (false, "两次密码不一致。", null);

        // 查重：登录名不可重复（含已删账号占用同名则提示换一个）
        var loginTaken = await _db.EUsers.AsNoTracking()
            .AnyAsync(x => x.LoginId == m.LoginName, ct);
        if (loginTaken)
            return (false, "该登录名已被注册，请换一个。", null);

        var now = DateTime.Now;
        var (pwd, algo, ver) = PasswordHasher.HashForStore(m.Password);
        var user = new EUser
        {
            LoginId = m.LoginName,
            RealName = m.RealName,
            PwdHash = pwd,
            PasswordAlgo = algo,
            PasswordVersion = ver,
            UserType = "EMPLOYEE",
            IsEnabled = true,
            MaxLoginCount = 9999,
            MaxPwdErrorCount = 8,
            BStatus = "1",
            CreateDate = now,
            AmendDate = now,
            OperatorName = "FT-REG"
        };
        _db.EUsers.Add(user);
        await _db.SaveChangesAsync(ct);

        _db.FtAccountBinds.Add(new FtAccountBind
        {
            UserId = user.DataId,
            Mobile = LooksLikeMobileLogin(m.LoginName) ? m.LoginName : null,
            CreateDate = now,
            AmendDate = now,
            OperatorName = "FT-REG"
        });

        await AssignMemberPostAsync(user.DataId, "FT-REG", now, ct);
        await _db.SaveChangesAsync(ct);
        await _persons.EnsureSelfPersonAsync(user.DataId, m.RealName, null, ct);
        return (true, "注册成功。", user);
    }

    private static bool LooksLikeMobileLogin(string login) =>
        login.Length is >= 6 and <= 15 && login.All(char.IsDigit);

    public async Task AssignMemberPostAsync(int userId, string op, DateTime now, CancellationToken ct)
    {
        var pos = await _db.EPositions.FirstOrDefaultAsync(x => x.PostCode == FtDutyAccess.Member && !x.IsDeleted, ct);
        var deptId = await _db.EDepartments.AsNoTracking().Where(d => !d.IsDeleted).Select(d => d.DataId).FirstOrDefaultAsync(ct);
        if (pos == null || deptId == 0) return;
        var exists = await _db.EUserPositions.AnyAsync(x => x.UserId == userId && x.PosId == pos.DataId, ct);
        if (exists) return;
        _db.EUserPositions.Add(new EUserPosition
        {
            UserId = userId,
            PosId = pos.DataId,
            DeptId = deptId,
            IsPrimary = true,
            BStatus = "1",
            CreateDate = now,
            AmendDate = now,
            OperatorName = FtText.ClipReq(op, 8)
        });
    }

    public async Task<(bool Ok, string Msg, EUser? User)> TryPasswordLoginByIdCardAsync(string idCard, string password, CancellationToken ct)
    {
        var user = await FindUserByIdCardAsync(idCard, ct);
        if (user == null) return (false, "身份证未注册。", null);
        if (user.IsDeleted || !user.IsEnabled || user.IsLocked)
            return (false, "账号不可用。", null);
        var tracked = await _db.EUsers.FirstOrDefaultAsync(x => x.DataId == user.DataId, ct);
        if (tracked == null) return (false, "账号不可用。", null);
        if (!PasswordHasher.Verify(password ?? "", tracked.PwdHash, tracked.PasswordAlgo))
            return (false, "密码不正确。", null);
        return (true, "", tracked);
    }

    public async Task<(bool Ok, string Msg, EUser User)> EnsureWxUserAsync(string openId, CancellationToken ct)
    {
        var oid = FtText.ClipReq(openId, 64);
        if (oid.Length == 0) return (false, "OpenID 无效。", null!);
        var bind = await _db.FtAccountBinds.FirstOrDefaultAsync(x => !x.IsDeleted && x.WechatOpenId == oid, ct);
        if (bind != null)
        {
            var exist = await _db.EUsers.FirstOrDefaultAsync(x => x.DataId == bind.UserId && !x.IsDeleted, ct);
            if (exist != null) return (true, "", exist);
        }

        var now = DateTime.Now;
        var hash = FtText.HashIdCard(oid, _opt.IdCardHashSalt);
        var loginId = "WX" + hash[..16];
        if (await _db.EUsers.AnyAsync(x => x.LoginId == loginId && !x.IsDeleted, ct))
            loginId = "WX" + hash[..20];
        var pwd = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12));
        var (ph, algo, ver) = PasswordHasher.HashForStore(pwd);
        var user = new EUser
        {
            LoginId = loginId,
            RealName = "微信用户",
            PwdHash = ph,
            PasswordAlgo = algo,
            PasswordVersion = ver,
            UserType = "EMPLOYEE",
            IsEnabled = true,
            MaxLoginCount = 9999,
            MaxPwdErrorCount = 8,
            BStatus = "1",
            CreateDate = now,
            AmendDate = now,
            OperatorName = "FT-WX"
        };
        _db.EUsers.Add(user);
        await _db.SaveChangesAsync(ct);
        _db.FtAccountBinds.Add(new FtAccountBind
        {
            UserId = user.DataId,
            WechatOpenId = oid,
            CreateDate = now,
            AmendDate = now,
            OperatorName = "FT-WX"
        });
        await AssignMemberPostAsync(user.DataId, "FT-WX", now, ct);
        await _db.SaveChangesAsync(ct);
        return (true, "", user);
    }

    public async Task<(bool Ok, string Msg)> BindIdCardToUserAsync(int userId, string idCard, CancellationToken ct)
    {
        if (!FtText.IsIdCard(idCard)) return (false, "请输入18位身份证号。");
        var hash = HashId(idCard);
        var other = await _db.FtAccountBinds.AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.IdCardHash == hash && x.UserId != userId, ct);
        if (other != null) return (false, "该身份证已绑定其他账号。请用身份证登录后再绑定微信。");
        var bind = await _db.FtAccountBinds.FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted, ct);
        if (bind == null)
        {
            bind = new FtAccountBind
            {
                UserId = userId,
                CreateDate = DateTime.Now,
                OperatorName = "FT-BIND"
            };
            _db.FtAccountBinds.Add(bind);
        }
        if (!string.IsNullOrEmpty(bind.IdCardHash) && !string.Equals(bind.IdCardHash, hash, StringComparison.OrdinalIgnoreCase))
            return (false, "本账号已绑定其他身份证，不能更改。");
        bind.IdCardHash = hash;
        bind.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        return (true, "已绑定身份证。");
    }

    public async Task<(bool Ok, string Msg)> BindWechatToUserAsync(int userId, string openId, CancellationToken ct)
    {
        var oid = FtText.ClipReq(openId, 64);
        if (oid.Length == 0) return (false, "OpenID 无效。");
        var other = await _db.FtAccountBinds.AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.WechatOpenId == oid && x.UserId != userId, ct);
        if (other != null) return (false, "该微信已绑定其他账号。");
        var bind = await _db.FtAccountBinds.FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted, ct);
        if (bind == null)
        {
            bind = new FtAccountBind
            {
                UserId = userId,
                CreateDate = DateTime.Now,
                OperatorName = "FT-BIND"
            };
            _db.FtAccountBinds.Add(bind);
        }
        if (!string.IsNullOrEmpty(bind.WechatOpenId) && !string.Equals(bind.WechatOpenId, oid, StringComparison.Ordinal))
            return (false, "本账号已绑定其他微信。");
        bind.WechatOpenId = oid;
        bind.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        return (true, "已绑定微信。");
    }

    public async Task SaveMobileAsync(int userId, string? mobile, CancellationToken ct)
    {
        var bind = await _db.FtAccountBinds.FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted, ct);
        if (bind == null) return;
        bind.Mobile = FtText.Clip(mobile, 32);
        bind.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class FtConflictService
{
    private readonly FrameworkDbContext _db;
    private readonly FtDutyAccess _duty;
    public FtConflictService(FrameworkDbContext db, FtDutyAccess duty)
    {
        _db = db;
        _duty = duty;
    }

    public async Task<(IReadOnlyList<FtConflictListRowVm> page, int total, int pages, int pageOut)> GetIndexPageAsync(
        string? status1, int page, int pageSize, CancellationToken ct)
    {
        var rows = await _db.FtMatchConflicts.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(status1))
            rows = rows.Where(x => string.Equals(x.ResolveStatus, status1, StringComparison.OrdinalIgnoreCase)).ToList();
        rows = rows.OrderByDescending(x => x.CreateDate).ToList();
        var (slice, total, pages, p) = FtPaging.Page(rows, page, pageSize);
        var vm = slice.Select(x => new FtConflictListRowVm
        {
            DataId = x.DataId,
            ConflictType = x.ConflictType,
            ResolveStatus = x.ResolveStatus,
            ConflictDetail = x.ConflictDetail,
            CreateDate = x.CreateDate
        }).ToList();
        return (vm, total, pages, p);
    }

    public async Task ResolveAsync(int id, string status, int userId, string remark, CancellationToken ct)
    {
        if (!await _duty.IsSuperAsync(userId, ct))
            throw new InvalidOperationException("仅超管可处理冲突。");
        var row = await _db.FtMatchConflicts.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("记录不存在。");
        row.ResolveStatus = status is "IGNORED" ? "IGNORED" : "RESOLVED";
        row.ResolveUserId = userId;
        row.Remark = FtText.Clip(remark, 512);
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class FtBranchAdminService
{
    private readonly FrameworkDbContext _db;
    private readonly FtDutyAccess _duty;
    public FtBranchAdminService(FrameworkDbContext db, FtDutyAccess duty)
    {
        _db = db;
        _duty = duty;
    }

    public async Task<List<FtBranchAdminRowVm>> ListAsync(int viewerUserId, CancellationToken ct)
    {
        var pos = await _db.EPositions.AsNoTracking().FirstOrDefaultAsync(x => x.PostCode == FtDutyAccess.BranchAdmin && !x.IsDeleted, ct);
        var assigned = new HashSet<int>();
        if (pos != null)
        {
            var ups = await _db.EUserPositions.AsNoTracking()
                .Where(x => x.PosId == pos.DataId)
                .ToListAsync(ct);
            assigned = ups.Where(x => EBStatusHelper.IsActiveBStatus(x.BStatus)).Select(x => x.UserId).ToHashSet();
        }

        HashSet<int>? clanUsers = null;
        if (!await _duty.IsSuperAsync(viewerUserId, ct))
        {
            var myClan = await _db.FtUserClans.AsNoTracking()
                .Where(x => !x.IsDeleted && x.UserId == viewerUserId)
                .Select(x => (int?)x.ClanId)
                .FirstOrDefaultAsync(ct);
            if (myClan == null) return new List<FtBranchAdminRowVm>();
            clanUsers = (await _db.FtUserClans.AsNoTracking()
                .Where(x => !x.IsDeleted && x.ClanId == myClan)
                .Select(x => x.UserId)
                .ToListAsync(ct)).ToHashSet();
        }

        var users = await _db.EUsers.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.LoginId).Take(500).ToListAsync(ct);
        if (clanUsers != null)
            users = users.Where(u => clanUsers.Contains(u.DataId)).ToList();
        return users.Select(u => new FtBranchAdminRowVm
        {
            UserId = u.DataId,
            LoginId = u.LoginId,
            RealName = u.RealName,
            IsBranchAdmin = assigned.Contains(u.DataId)
        }).ToList();
    }

    public async Task SetAsync(int userId, bool grant, int opUserId, string op, CancellationToken ct)
    {
        if (await _duty.IsSuperAsync(opUserId, ct) && !await _duty.IsClanAdminAsync(opUserId, ct))
            throw new InvalidOperationException("超管不管理支链管理员，请由该家族的族谱管理员操作。");
        if (!await _duty.IsClanAdminAsync(opUserId, ct))
            throw new InvalidOperationException("仅族谱管理员可任免本家族支链管理员。");

        var adminClan = await _db.FtUserClans.AsNoTracking()
            .Where(x => !x.IsDeleted && x.UserId == opUserId).Select(x => (int?)x.ClanId).FirstOrDefaultAsync(ct);
        var targetClan = await _db.FtUserClans.AsNoTracking()
            .Where(x => !x.IsDeleted && x.UserId == userId).Select(x => (int?)x.ClanId).FirstOrDefaultAsync(ct);
        if (adminClan == null || targetClan == null || adminClan != targetClan)
            throw new InvalidOperationException("只能任免本家族成员为支链管理员。");

        var pos = await _db.EPositions.FirstOrDefaultAsync(x => x.PostCode == FtDutyAccess.BranchAdmin && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("未找到支链管理员岗位，请先执行种子脚本。");
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
}

public sealed class FtMyProfileService
{
    private readonly FrameworkDbContext _db;
    private readonly FtAccountService _acc;
    public FtMyProfileService(FrameworkDbContext db, FtAccountService acc)
    {
        _db = db;
        _acc = acc;
    }

    public async Task<FtPerson?> BoundPersonAsync(int userId, CancellationToken ct) =>
        await _db.FtPersons.FirstOrDefaultAsync(x => x.BindUserId == userId && !x.IsDeleted, ct);

    public async Task SaveAsync(FtProfileFormVm m, int userId, CancellationToken ct)
    {
        var p = await BoundPersonAsync(userId, ct) ?? throw new InvalidOperationException("尚未绑定本人档案，请先完成「本人」录入。");
        p.NickName = FtText.Clip(m.NickName, 128);
        p.SelfIntro = m.SelfIntro;
        p.WechatId = FtText.Clip(m.WechatId, 128);
        p.PrivacyLevel = m.PrivacyLevel is < 1 or > 4 ? p.PrivacyLevel : m.PrivacyLevel;
        if (p.PrivacyLevel < 1 || p.PrivacyLevel > 4) p.PrivacyLevel = 1;
        p.ShowPhoto = m.ShowPhoto;
        p.ShowWechat = m.ShowWechat;
        p.ShowSelfIntro = m.ShowSelfIntro;
        p.ShowBirthDetail = m.ShowBirthDetail;
        p.ShowResume = m.ShowResume;
        p.PrintAllow = m.PrintAllow;
        p.AmendDate = DateTime.Now;
        await _acc.SaveMobileAsync(userId, m.Mobile, ct);
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class FtBatchMatchService
{
    private readonly FrameworkDbContext _db;
    private readonly FtMatchService _match;
    public FtBatchMatchService(FrameworkDbContext db, FtMatchService match)
    {
        _db = db;
        _match = match;
    }

    public async Task<int> RunAsync(CancellationToken ct)
    {
        var ids = await _db.FtPersons.AsNoTracking().Where(x => !x.IsDeleted).Select(x => x.DataId).ToListAsync(ct);
        var n = 0;
        foreach (var id in ids)
        {
            var hints = await _match.MatchPersonAsync(id, ct);
            n += hints.Count;
        }
        return n;
    }
}
