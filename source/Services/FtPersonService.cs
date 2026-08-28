using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public sealed class FtPersonService
{
    private readonly FrameworkDbContext _db;
    private readonly FtDutyAccess _duty;
    private readonly FtMatchService _match;
    private readonly FtOpLogService _log;
    private readonly FtClanService _clans;

    public FtPersonService(FrameworkDbContext db, FtDutyAccess duty, FtMatchService match, FtOpLogService log, FtClanService clans)
    {
        _db = db;
        _duty = duty;
        _match = match;
        _log = log;
        _clans = clans;
    }

    public static string GenderText(byte g) => g == 0 ? "女" : "男";
    public static string GenText(int n) => n <= 0 ? "—" : n.ToString();

    public FtPersonFormVm ToForm(FtPerson row, bool nameLocked = false) => new()
    {
        DataId = row.DataId,
        FullName = row.FullName,
        FatherName = row.FatherName,
        MotherName = row.MotherName,
        BirthDate = row.BirthDate,
        Gender = row.Gender,
        GenerationNo = row.GenerationNo,
        WordOfGeneration = row.WordOfGeneration,
        FatherPersonId = row.FatherPersonId,
        MotherPersonId = row.MotherPersonId,
        NickName = row.NickName,
        SelfIntro = row.SelfIntro,
        WechatId = row.WechatId,
        DeathInfo = row.DeathInfo,
        IsDead = row.IsDead,
        IsCertified = row.IsCertified,
        PrivacyLevel = row.PrivacyLevel,
        ShowPhoto = row.ShowPhoto,
        ShowWechat = row.ShowWechat,
        ShowSelfIntro = row.ShowSelfIntro,
        ShowBirthDetail = row.ShowBirthDetail,
        ShowResume = row.ShowResume,
        PrintAllow = row.PrintAllow,
        Remark = row.Remark,
        BStatus = row.BStatus,
        InMainGenealogy = row.InMainGenealogy,
        KeyLocked = row.KeyLocked,
        NameLocked = nameLocked
    };

    public void NormalizeFormForSave(FtPersonFormVm m)
    {
        m.FullName = FtText.ClipReq(m.FullName, 64);
        m.FatherName = FtText.ClipReq(m.FatherName, 64);
        m.MotherName = FtText.Clip(m.MotherName, 64);
        m.BirthDate = FtText.Clip(m.BirthDate, 32);
        m.NickName = FtText.Clip(m.NickName, 128);
        m.WechatId = FtText.Clip(m.WechatId, 128);
        m.DeathInfo = FtText.Clip(m.DeathInfo, 128);
        if (!m.IsDead) m.DeathInfo = null;
        m.WordOfGeneration = FtText.Clip(m.WordOfGeneration, 32);
        m.Remark = FtText.Clip(m.Remark, 512);
        m.BStatus = EBStatusHelper.NormalizeBStatusForSave(m.BStatus);
        if (m.PrivacyLevel is < 1 or > 4) m.PrivacyLevel = 1;
    }

    public List<(string Key, string Msg)> GetSaveValidationErrors(FtPersonFormVm m)
    {
        var e = new List<(string, string)>();
        if (string.IsNullOrWhiteSpace(m.FullName)) e.Add((nameof(m.FullName), "姓名不能为空。"));
        return e;
    }

    public async Task<(IReadOnlyList<FtPersonListRowVm> page, int total, int pages, int pageOut)> GetIndexPageAsync(
        int userId, string? inMain1, string searchField, string searchContent,
        string sortField, string sortArrow, int page, int pageSize, CancellationToken ct, string? certified1 = null)
    {
        // 列表与配偶信息一致：普通族人只看自己录入/绑定本人；超管/分支管看全部
        // 已链入等同的源人（SameAs）不单独占一行，只保留主谱目标那一条
        var q = (await OwnedOrBoundPersonsAsync(userId, ct))
            .Where(x => x.SameAsPersonId == null)
            .ToList();
        if (inMain1 == "1") q = q.Where(x => x.InMainGenealogy).ToList();
        else if (inMain1 == "0") q = q.Where(x => !x.InMainGenealogy).ToList();
        if (certified1 == "1") q = q.Where(x => x.IsCertified).ToList();
        else if (certified1 == "0") q = q.Where(x => !x.IsCertified).ToList();

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            q = (searchField ?? "") switch
            {
                "FatherName" => q.Where(x => x.FatherName.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "BirthDate" => q.Where(x => (x.BirthDate ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => q.Where(x => x.FullName.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList()
            };
        }

        q = (sortField, sortArrow) switch
        {
            ("FatherName", "1") => q.OrderByDescending(x => x.FatherName).ToList(),
            ("FatherName", _) => q.OrderBy(x => x.FatherName).ToList(),
            ("AmendDate", "1") => q.OrderBy(x => x.AmendDate).ToList(),
            ("AmendDate", _) => q.OrderByDescending(x => x.AmendDate).ToList(),
            ("FullName", "1") => q.OrderByDescending(x => x.FullName).ToList(),
            _ => q.OrderBy(x => x.FullName).ToList()
        };

        var (slice, total, pages, p) = FtPaging.Page(q, page, pageSize);
        var vm = slice.Select(x => new FtPersonListRowVm
        {
            DataId = x.DataId,
            FullName = x.FullName,
            FatherName = x.FatherName,
            MotherName = x.MotherName,
            BirthDate = x.BirthDate,
            InMainText = x.InMainGenealogy ? "主谱" : "个人",
            GenerationText = GenText(x.GenerationNo),
            LifeText = x.IsDead
                ? (string.IsNullOrWhiteSpace(x.DeathInfo) ? "故去" : "故去 " + x.DeathInfo)
                : "在世",
            CertText = x.IsCertified ? "已认证" : "未认证",
            AmendDate = x.AmendDate
        }).ToList();
        return (vm, total, pages, p);
    }

    public async Task<List<FtPerson>> VisiblePersonsAsync(int userId, CancellationToken ct)
    {
        var all = await _db.FtPersons.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(ct);
        var mine = all.Where(x => x.OwnerUserId == userId || x.BindUserId == userId).ToList();
        // 纯超管不看业务人物全库
        if (await _duty.IsSuperAsync(userId, ct) && !await _duty.IsBranchAdminAsync(userId, ct))
            return mine.Where(x => x.SameAsPersonId == null).ToList();

        var clanId = await _clans.GetUserClanIdAsync(userId, ct);

        // 未入任何家族：只能看自己录入/绑定
        if (clanId == null)
            return mine.Where(x => x.SameAsPersonId == null).ToList();

        var inClan = all.Where(x => x.SameAsPersonId == null && x.ClanId == clanId).ToList();
        if (await _duty.IsBranchAdminAsync(userId, ct))
            return inClan;

        // 普通族人：未入主谱只看自己的；已入主谱可看本家族主谱
        if (!await UserHasJoinedMainAsync(userId, ct))
            return mine.Where(x => x.SameAsPersonId == null).ToList();

        return inClan.Where(x =>
            x.OwnerUserId == userId ||
            x.BindUserId == userId ||
            x.InMainGenealogy).ToList();
    }

    /// <summary>
    /// 配偶信息等「只看自己录入」的列表范围：超管/分支管看全部可见人；
    /// 普通族人始终只看 OwnerUserId / BindUserId（不因已入主谱而带出他人）。
    /// </summary>
    /// <summary>
    /// 本人名下（录入或绑定）人物，<strong>不含</strong>管理岗扩权。用于「我录入的小树」。
    /// </summary>
    public async Task<List<FtPerson>> OwnedOrBoundPersonsStrictAsync(int userId, CancellationToken ct)
    {
        return await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted && x.SameAsPersonId == null
                        && (x.OwnerUserId == userId || x.BindUserId == userId))
            .ToListAsync(ct);
    }

    public async Task<List<FtPerson>> OwnedOrBoundPersonsAsync(int userId, CancellationToken ct)
    {
        if (await _duty.IsSuperAsync(userId, ct) || await _duty.IsBranchAdminAsync(userId, ct))
            return await VisiblePersonsAsync(userId, ct);
        return await OwnedOrBoundPersonsStrictAsync(userId, ct);
    }

    public async Task<FtPerson?> GetAsync(int id, CancellationToken ct) =>
        await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);

    public async Task<bool> CanViewAsync(int userId, FtPerson p, CancellationToken ct)
    {
        if (await _duty.IsSuperAsync(userId, ct) || await _duty.IsBranchAdminAsync(userId, ct)) return true;
        if (p.OwnerUserId == userId || p.BindUserId == userId) return true;
        if (!p.InMainGenealogy) return false;
        // 主谱人物：仅当自己已链入生效/纳入后才可见
        return await UserHasJoinedMainAsync(userId, ct);
    }

    /// <summary>
    /// 是否已进入主谱可见。有<strong>待审</strong>链入申请时一律不算（只能看小树）；
    /// 须链入生效、等同主谱，或名下确有已纳入主谱且非待审源的人。
    /// </summary>
    public async Task<bool> UserHasJoinedMainAsync(int userId, CancellationToken ct)
    {
        // 本人还有待审链入 → 未入主谱，只能看小树（即使名下另有 InMain 人物）
        if (await _db.FtPersonLinks.AsNoTracking().AnyAsync(x =>
                !x.IsDeleted && x.LinkStatus == "PENDING" && x.ApplyUserId == userId, ct))
            return false;

        if (await _db.FtPersonLinks.AsNoTracking().AnyAsync(x =>
                !x.IsDeleted && x.LinkStatus == "EFFECTIVE" && x.ApplyUserId == userId, ct))
            return true;

        if (await _db.FtPersons.AsNoTracking().AnyAsync(x =>
                !x.IsDeleted && x.SameAsPersonId != null &&
                (x.OwnerUserId == userId || x.BindUserId == userId), ct))
            return true;

        return await _db.FtPersons.AsNoTracking().AnyAsync(x =>
            !x.IsDeleted && x.InMainGenealogy && x.SameAsPersonId == null
            && (x.OwnerUserId == userId || x.BindUserId == userId), ct);
    }

    /// <summary>待审链入源人强制出主谱标记，避免未审批就进主谱树/主谱可见。</summary>
    public async Task<int> ClearInMainOnPendingLinkSourcesAsync(CancellationToken ct)
    {
        var pendingIds = await _db.FtPersonLinks.AsNoTracking()
            .Where(x => !x.IsDeleted && x.LinkStatus == "PENDING")
            .Select(x => x.SourcePersonId)
            .Distinct()
            .ToListAsync(ct);
        if (pendingIds.Count == 0) return 0;
        var rows = await _db.FtPersons
            .Where(x => !x.IsDeleted && x.InMainGenealogy && pendingIds.Contains(x.DataId))
            .ToListAsync(ct);
        foreach (var r in rows)
        {
            r.InMainGenealogy = false;
            r.AmendDate = DateTime.Now;
        }
        if (rows.Count > 0) await _db.SaveChangesAsync(ct);
        return rows.Count;
    }

    /// <summary>历史链入：BindUserId 仍在等同源人上时，迁到主谱目标。</summary>
    public async Task<int> HealLinkedBindingsAsync(CancellationToken ct)
    {
        var sources = await _db.FtPersons
            .Where(x => !x.IsDeleted && x.SameAsPersonId != null && x.BindUserId != null)
            .ToListAsync(ct);
        var n = 0;
        foreach (var src in sources)
        {
            var tgt = await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == src.SameAsPersonId!.Value && !x.IsDeleted, ct);
            if (tgt == null) continue;
            if (!tgt.BindUserId.HasValue)
            {
                tgt.BindUserId = src.BindUserId;
                src.BindUserId = null;
                n++;
            }
            else if (tgt.BindUserId == src.BindUserId)
            {
                src.BindUserId = null;
                n++;
            }
        }
        if (n > 0) await _db.SaveChangesAsync(ct);
        return n;
    }

    public async Task<bool> IsStaffAsync(int userId, CancellationToken ct) =>
        await _duty.IsBranchAdminAsync(userId, ct);

    public async Task<bool> CanCertifyAsync(int userId, CancellationToken ct) =>
        await _duty.IsBranchAdminAsync(userId, ct);

    public async Task<bool> MemberCanApplyMainAsync(int userId, CancellationToken ct)
    {
        if (await _duty.IsBranchAdminAsync(userId, ct)) return true;
        var self = await GetSelfPersonAsync(userId, ct);
        return self != null && self.IsCertified;
    }

    /// <summary>
    /// 改档案内容：仅录入人（Owner）。族谱/支链管理员不能改他人已填字段，只能上下挂边。
    /// </summary>
    public Task<bool> CanEditAsync(int userId, FtPerson p, CancellationToken ct) =>
        Task.FromResult(p.OwnerUserId == userId);

    public async Task<bool> CanChangeNameAsync(int userId, FtPerson p, CancellationToken ct)
    {
        if (!p.KeyLocked) return true;
        // 入谱锁名纠错：本家族族谱管理员（超管不碰业务）
        return await _duty.IsClanAdminAsync(userId, ct);
    }

    public Task<HashSet<int>> EditableIdsAsync(int userId, IReadOnlyList<FtPerson> persons, CancellationToken ct)
    {
        var set = new HashSet<int>();
        if (persons.Count == 0) return Task.FromResult(set);
        foreach (var p in persons)
        {
            if (p.OwnerUserId == userId) set.Add(p.DataId);
        }
        return Task.FromResult(set);
    }

    public async Task<(int Id, List<FtMatchHintVm> Hints)> CreateAsync(FtPersonFormVm m, int userId, string op, CancellationToken ct)
    {
        var now = DateTime.Now;
        var reused = await FindReuseAsync(userId, m.FullName, m.FatherName, m.MotherName, ct);
        if (reused != null) return (reused.DataId, await _match.MatchPersonAsync(reused.DataId, ct));

        if (await WouldCycleAsync(null, m.FatherPersonId, m.MotherPersonId, ct))
            throw new InvalidOperationException("父母关系形成环路，已拒绝。");

        var row = new FtPerson
        {
            FullName = m.FullName,
            FatherName = m.FatherName,
            MotherName = m.MotherName,
            BirthDate = m.BirthDate,
            BirthYear = FtText.ParseBirthYear(m.BirthDate),
            Gender = m.Gender,
            GenerationNo = m.GenerationNo,
            WordOfGeneration = m.WordOfGeneration,
            FatherPersonId = m.FatherPersonId,
            MotherPersonId = m.MotherPersonId,
            NickName = m.NickName,
            SelfIntro = m.SelfIntro,
            WechatId = m.WechatId,
            DeathInfo = m.DeathInfo,
            IsDead = m.IsDead,
            PrivacyLevel = m.PrivacyLevel,
            ShowPhoto = m.ShowPhoto,
            ShowWechat = m.ShowWechat,
            ShowSelfIntro = m.ShowSelfIntro,
            ShowBirthDetail = m.ShowBirthDetail,
            ShowResume = m.ShowResume,
            PrintAllow = m.PrintAllow,
            Remark = m.Remark,
            BStatus = m.BStatus,
            AuditStatus = "PASS",
            OwnerUserId = userId,
            CreateDate = now,
            AmendDate = now,
            OperatorName = FtText.ClipReq(op, 30)
        };
        await _clans.EnsurePersonClanAsync(row, userId, ct);
        _db.FtPersons.Add(row);
        await _db.SaveChangesAsync(ct);
        await TryAttachParentsByNameAsync(row.DataId, ct);
        var hints = await _match.MatchPersonAsync(row.DataId, ct);
        return (row.DataId, hints);
    }

    public async Task<List<FtMatchHintVm>> TryUpdateAsync(int id, FtPersonFormVm m, int userId, string op, CancellationToken ct)
    {
        var row = await GetAsync(id, ct) ?? throw new InvalidOperationException("人物不存在。");
        if (!await CanEditAsync(userId, row, ct))
            throw new InvalidOperationException("无权修改该人物。");
        if (await WouldCycleAsync(id, row.FatherPersonId, row.MotherPersonId, ct))
            throw new InvalidOperationException("父母关系形成环路，已拒绝。");

        var nameLocked = !await CanChangeNameAsync(userId, row, ct);
        var keyChanged = false;
        if (nameLocked)
        {
            m.FullName = row.FullName;
        }
        else if (row.FullName != m.FullName)
        {
            keyChanged = true;
            row.FullName = m.FullName;
        }

        keyChanged = keyChanged
            || row.FatherName != m.FatherName
            || (row.MotherName ?? "") != (m.MotherName ?? "")
            || (row.BirthDate ?? "") != (m.BirthDate ?? "");
        row.FatherName = m.FatherName;
        row.MotherName = m.MotherName;
        row.BirthDate = m.BirthDate;
        row.BirthYear = FtText.ParseBirthYear(m.BirthDate);

        row.Gender = m.Gender;
        row.GenerationNo = m.GenerationNo;
        row.WordOfGeneration = m.WordOfGeneration;
        row.NickName = m.NickName;
        row.SelfIntro = m.SelfIntro;
        row.WechatId = m.WechatId;
        row.DeathInfo = m.DeathInfo;
        row.IsDead = m.IsDead;
        row.PrivacyLevel = m.PrivacyLevel;
        row.ShowPhoto = m.ShowPhoto;
        row.ShowWechat = m.ShowWechat;
        row.ShowSelfIntro = m.ShowSelfIntro;
        row.ShowBirthDetail = m.ShowBirthDetail;
        row.ShowResume = m.ShowResume;
        row.PrintAllow = m.PrintAllow;
        row.Remark = m.Remark;
        row.BStatus = m.BStatus;
        row.AmendDate = DateTime.Now;
        row.OperatorName = FtText.ClipReq(op, 30);
        await _db.SaveChangesAsync(ct);
        var parentLinked = await TryAttachParentsByNameAsync(id, ct);
        if (keyChanged || parentLinked)
            return await _match.MatchPersonAsync(id, ct);
        return new List<FtMatchHintVm>();
    }

    public async Task CorrectKeysAsync(int id, FtPersonFormVm m, int userId, string op, CancellationToken ct)
    {
        if (!await _duty.IsSuperAsync(userId, ct))
            throw new InvalidOperationException("仅超管可纠错关键字。");
        var row = await GetAsync(id, ct) ?? throw new InvalidOperationException("人物不存在。");
        var before = $"{row.FullName}/{row.FatherName}/{row.MotherName}/{row.BirthDate}";
        row.FullName = m.FullName;
        row.FatherName = m.FatherName;
        row.MotherName = m.MotherName;
        row.BirthDate = m.BirthDate;
        row.BirthYear = FtText.ParseBirthYear(m.BirthDate);
        row.AmendDate = DateTime.Now;
        row.OperatorName = FtText.ClipReq(op, 30);
        await _db.SaveChangesAsync(ct);
        await _log.WriteAsync("KEY_CORRECT", "Person", id.ToString(), userId, op, before, before,
            $"{row.FullName}/{row.FatherName}/{row.MotherName}/{row.BirthDate}", ct);
        await _match.MatchPersonAsync(id, ct);
    }

    public async Task<string> SetMainAsync(int id, bool include, bool withDesc, int userId, string op, CancellationToken ct, bool descendantsOnly = false)
    {
        var row = await GetAsync(id, ct) ?? throw new InvalidOperationException("人物不存在。");
        if (!await CanSetMainAsync(userId, row, ct))
            throw new InvalidOperationException("只能对自己录入的人物纳入或移出主谱。");
        var super = await _duty.IsSuperAsync(userId, ct);
        string msg;
        if (descendantsOnly)
        {
            var n = await ApplyMainToDescendantsAsync(id, markMain: false, userId, super, ct);
            if (n == 0) throw new InvalidOperationException("没有可移出的子孙（仅处理你自己录入的人）。");
            msg = $"已将 {n} 名子孙移出主谱，本人仍留在主谱。";
            await _log.WriteAsync("MAIN_OUT_DESC", "Person", id.ToString(), userId, op, "仅移出子孙", null, null, ct);
        }
        else if (!include)
        {
            var n = 0;
            if (withDesc)
                n = await ApplyMainToDescendantsAsync(id, markMain: false, userId, super, ct);
            row.InMainGenealogy = false;
            row.KeyLocked = false;
            msg = n > 0 ? $"已移出主谱，并移出 {n} 名子孙。" : "已移出主谱。";
            await _log.WriteAsync("MAIN_OUT", "Person", id.ToString(), userId, op, n > 0 ? $"连同{n}名子孙移出" : "移出主谱", null, null, ct);
        }
        else
        {
            if (!await MemberCanApplyMainAsync(userId, ct))
                throw new InvalidOperationException("须先完成族员认证。请打开右上角「申请认证」，把二维码发给分支管理员或超管扫码通过后，再申请加入主谱。");
            row.InMainGenealogy = true;
            row.KeyLocked = true;
            var n = 0;
            if (withDesc)
                n = await ApplyMainToDescendantsAsync(id, markMain: true, userId, super, ct);
            if (withDesc)
                msg = n > 0
                    ? $"已纳入主谱，并纳入 {n} 名子孙。"
                    : "已纳入本人。未找到你录入的子孙。";
            else
                msg = "已纳入主谱。";
            await _log.WriteAsync("MAIN_IN", "Person", id.ToString(), userId, op,
                withDesc ? (n > 0 ? $"连同{n}名子孙纳入" : "纳入主谱（无子孙）") : "纳入主谱", null, null, ct);
        }
        row.AmendDate = DateTime.Now;
        row.OperatorName = FtText.ClipReq(op, 30);
        await _db.SaveChangesAsync(ct);
        return msg;
    }

    public async Task TransferOwnerAsync(int id, int newOwner, int userId, string op, CancellationToken ct)
    {
        if (!await _duty.IsSuperAsync(userId, ct))
            throw new InvalidOperationException("仅超管可转移所有权。");
        var row = await GetAsync(id, ct) ?? throw new InvalidOperationException("人物不存在。");
        var old = row.OwnerUserId;
        row.OwnerUserId = newOwner;
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        await _log.WriteAsync("OWNER_XFER", "Person", id.ToString(), userId, op, $"{old}->{newOwner}", null, null, ct);
    }

    public async Task SoftDeleteAsync(int id, int userId, CancellationToken ct)
    {
        var row = await GetAsync(id, ct) ?? throw new InvalidOperationException("人物不存在。");
        if (!await _duty.IsSuperAsync(userId, ct) && row.OwnerUserId != userId)
            throw new InvalidOperationException("无权删除。");
        if (row.InMainGenealogy && !await _duty.IsSuperAsync(userId, ct))
            throw new InvalidOperationException("主谱人物仅超管可删。");
        row.IsDeleted = true;
        row.BStatus = "2";
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<FtPerson?> FindReuseAsync(int ownerId, string fullName, string fatherName, string? motherName, CancellationToken ct)
    {
        var n = FtText.NormName(fullName);
        var f = FtText.NormName(fatherName);
        var mo = FtText.NormName(motherName);
        var list = await _db.FtPersons.Where(x =>
            !x.IsDeleted && x.SameAsPersonId == null && x.OwnerUserId == ownerId && !x.InMainGenealogy).ToListAsync(ct);
        return list.FirstOrDefault(x =>
            FtText.NormName(x.FullName) == n &&
            FtText.NormName(x.FatherName) == f &&
            FtText.NormName(x.MotherName) == mo);
    }

    /// <summary>新建前查重：本人小树已有，或主谱高/中置信疑似同一人。</summary>
    public async Task<List<FtMatchHintVm>> FindPreCreateDuplicatesAsync(FtPersonFormVm m, int userId, CancellationToken ct)
    {
        var list = new List<FtMatchHintVm>();
        var reused = await FindReuseAsync(userId, m.FullName, m.FatherName, m.MotherName, ct);
        if (reused != null)
        {
            list.Add(new FtMatchHintVm
            {
                PersonId = reused.DataId,
                FullName = reused.FullName,
                FatherName = reused.FatherName,
                MotherName = reused.MotherName,
                BirthDate = reused.BirthDate,
                BirthYear = reused.BirthYear,
                InMain = reused.InMainGenealogy,
                Level = 1,
                Reason = "您已录入过相同姓名+父母，建议打开已有档案"
            });
        }

        var probe = new FtPerson
        {
            DataId = 0,
            FullName = m.FullName,
            FatherName = m.FatherName,
            MotherName = m.MotherName,
            BirthDate = m.BirthDate,
            BirthYear = FtText.ParseBirthYear(m.BirthDate)
        };
        foreach (var h in (await _match.MatchCoreAsync(probe, ct)).Where(x => x.InMain && x.Level <= 2))
        {
            if (list.Any(x => x.PersonId == h.PersonId)) continue;
            h.Reason = string.IsNullOrEmpty(h.Reason) ? "主谱疑似同一人" : h.Reason + "（主谱）";
            list.Add(h);
        }
        return list.OrderBy(x => x.Level).ThenByDescending(x => x.InMain).ToList();
    }

    /// <summary>
    /// 按父/母姓名补挂 ID 边：仅当候选唯一时写入；未入主谱子女<strong>不</strong>按姓名猜主谱父母
    /// （主谱引用必须已有明确的 FatherPersonId/MotherPersonId，挂上哪个 ID 就是哪个）。
    /// </summary>
    public async Task<bool> TryAttachParentsByNameAsync(int personId, CancellationToken ct)
    {
        var row = await GetAsync(personId, ct);
        if (row == null || row.SameAsPersonId.HasValue) return false;

        var pool = await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted && x.SameAsPersonId == null && x.DataId != personId)
            .ToListAsync(ct);
        var byName = pool
            .GroupBy(x => FtText.NormName(x.FullName), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var childInMain = row.InMainGenealogy;
        var changed = false;
        if (!row.FatherPersonId.HasValue)
        {
            var fn = FtText.NormName(row.FatherName);
            if (fn.Length > 0 && byName.TryGetValue(fn, out var cands))
            {
                var pick = PickUniqueParentCandidate(cands, childInMain);
                if (pick != null && !await WouldCycleAsync(personId, pick.DataId, row.MotherPersonId, ct))
                {
                    row.FatherPersonId = pick.DataId;
                    changed = true;
                }
            }
        }
        if (!row.MotherPersonId.HasValue)
        {
            var mn = FtText.NormName(row.MotherName);
            if (mn.Length > 0 && byName.TryGetValue(mn, out var cands))
            {
                var pick = PickUniqueParentCandidate(cands, childInMain);
                if (pick != null && !await WouldCycleAsync(personId, row.FatherPersonId, pick.DataId, ct))
                {
                    row.MotherPersonId = pick.DataId;
                    changed = true;
                }
            }
        }
        if (changed)
        {
            row.AmendDate = DateTime.Now;
            await _db.SaveChangesAsync(ct);
        }
        return changed;
    }

    /// <param name="childInMain">子女是否已在主谱；未入主谱时不得按姓名自动挂到主谱父母。</param>
    private static FtPerson? PickUniqueParentCandidate(List<FtPerson> cands, bool childInMain)
    {
        if (cands.Count == 0) return null;
        if (!childInMain)
        {
            var personal = cands.Where(x => !x.InMainGenealogy).ToList();
            return personal.Count == 1 ? personal[0] : null;
        }
        var main = cands.Where(x => x.InMainGenealogy).ToList();
        if (main.Count == 1) return main[0];
        if (main.Count > 1) return null;
        return cands.Count == 1 ? cands[0] : null;
    }

    /// <summary>保存后：有主谱命中则去确认链入页，否则回人物详情。</summary>
    public static (string Action, object Route) RedirectAfterSave(int personId, IReadOnlyList<FtMatchHintVm> hints)
    {
        var main = hints.Where(h => h.InMain).OrderBy(h => h.Level).ToList();
        if (main.Count == 1 && main[0].Level <= 2)
            return ("Confirm", new { sourceId = personId, targetId = main[0].PersonId, level = (byte)main[0].Level });
        if (main.Count > 0)
            return ("Suggest", new { sourceId = personId });
        return ("Edit", new { id = personId });
    }

    public async Task<bool> CanSetMainAsync(int userId, FtPerson p, CancellationToken ct)
    {
        if (await _duty.IsBranchAdminAsync(userId, ct)) return true;
        return p.OwnerUserId == userId;
    }

    public async Task SetCertifiedAsync(int id, bool certified, int userId, string op, CancellationToken ct)
    {
        var row = await GetAsync(id, ct) ?? throw new InvalidOperationException("人物不存在。");
        if (!await CanCertifyAsync(userId, ct))
            throw new InvalidOperationException("仅本家族的族谱管理员或支链管理员可认证。");
        if (!certified && row.InMainGenealogy)
            throw new InvalidOperationException("已在主谱中，须先退出主谱才能取消认证。");
        if (row.IsCertified == certified) return;
        row.IsCertified = certified;
        row.AmendDate = DateTime.Now;
        row.OperatorName = FtText.ClipReq(op, 30);
        await _db.SaveChangesAsync(ct);
        await _log.WriteAsync(certified ? "CERTIFY" : "UNCERTIFY", "Person", id.ToString(), userId, op,
            certified ? "认证" : "取消认证", null, null, ct);
    }

    public Task<FtPerson?> GetSelfPersonAsync(int userId, CancellationToken ct) =>
        _db.FtPersons.FirstOrDefaultAsync(x => x.BindUserId == userId && !x.IsDeleted, ct);

    public async Task<FtPerson?> FindByCertCodeAsync(string? code, CancellationToken ct)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();
        if (c.Length < 6) return null;
        return await _db.FtPersons.FirstOrDefaultAsync(x => !x.IsDeleted && x.CertCode == c, ct);
    }

    public async Task<FtPerson> EnsureSelfPersonAsync(int userId, string? realName, string? idCard, CancellationToken ct)
    {
        var self = await GetSelfPersonAsync(userId, ct);
        if (self != null)
        {
            if (string.IsNullOrWhiteSpace(self.CertCode))
            {
                self.CertCode = await AllocCertCodeAsync(ct);
                self.AmendDate = DateTime.Now;
                await _db.SaveChangesAsync(ct);
            }
            if (self.ClanId == null || self.ClanId <= 0)
            {
                await _clans.EnsurePersonClanAsync(self, userId, ct);
                if (self.ClanId != null)
                {
                    self.AmendDate = DateTime.Now;
                    await _db.SaveChangesAsync(ct);
                }
            }
            return self;
        }

        var now = DateTime.Now;
        var birth = FtText.BirthDateFromIdCard(idCard);
        self = new FtPerson
        {
            FullName = FtText.ClipReq(realName, 64, "未命名"),
            FatherName = "",
            BirthDate = birth,
            BirthYear = FtText.ParseBirthYear(birth),
            Gender = FtText.GenderFromIdCard(idCard),
            BindUserId = userId,
            OwnerUserId = userId,
            CertCode = await AllocCertCodeAsync(ct),
            AuditStatus = "PASS",
            BStatus = "1",
            PrintAllow = true,
            CreateDate = now,
            AmendDate = now,
            OperatorName = "FT-SELF"
        };
        await _clans.EnsurePersonClanAsync(self, userId, ct);
        _db.FtPersons.Add(self);
        await _db.SaveChangesAsync(ct);
        return self;
    }

    public async Task SetCertifiedByCodeAsync(string? code, int userId, string op, CancellationToken ct)
    {
        var row = await FindByCertCodeAsync(code, ct) ?? throw new InvalidOperationException("认证码无效或已过期。");
        await SetCertifiedAsync(row.DataId, true, userId, op, ct);
    }

    private async Task<string> AllocCertCodeAsync(CancellationToken ct)
    {
        for (var i = 0; i < 8; i++)
        {
            var code = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(5));
            var used = await _db.FtPersons.AnyAsync(x => x.CertCode == code, ct);
            if (!used) return code;
        }
        throw new InvalidOperationException("无法生成认证码，请重试。");
    }

    /// <summary>按父/母节点 ID，或子女档案上的父/母姓名，遍历子孙并纳入/移出主谱。非超管只改自己录入的人。</summary>
    private async Task<int> ApplyMainToDescendantsAsync(int rootId, bool markMain, int userId, bool super, CancellationToken ct)
    {
        var all = await _db.FtPersons.Where(x => !x.IsDeleted).ToListAsync(ct);
        var root = all.FirstOrDefault(x => x.DataId == rootId);
        if (root == null) return 0;
        var q = new Queue<FtPerson>();
        q.Enqueue(root);
        var seen = new HashSet<int> { rootId };
        var n = 0;
        while (q.Count > 0)
        {
            var parent = q.Dequeue();
            foreach (var c in ChildrenOf(parent, all))
            {
                if (!seen.Add(c.DataId)) continue;
                var canTouch = super || c.OwnerUserId == userId;
                if (canTouch)
                {
                    if (markMain)
                        TryLinkParentEdge(c, parent);
                    if (c.InMainGenealogy != markMain)
                    {
                        c.InMainGenealogy = markMain;
                        c.KeyLocked = markMain;
                        c.AmendDate = DateTime.Now;
                        n++;
                    }
                }
                q.Enqueue(c);
            }
        }
        return n;
    }

    private static IEnumerable<FtPerson> ChildrenOf(FtPerson parent, List<FtPerson> all)
    {
        var pn = FtText.NormName(parent.FullName);
        foreach (var x in all)
        {
            if (x.DataId == parent.DataId) continue;
            if (x.FatherPersonId == parent.DataId || x.MotherPersonId == parent.DataId)
            {
                yield return x;
                continue;
            }
            if (pn.Length == 0) continue;
            if (x.OwnerUserId != parent.OwnerUserId && !x.InMainGenealogy && !parent.InMainGenealogy)
                continue;
            if (FtText.NormName(x.FatherName) == pn || FtText.NormName(x.MotherName) == pn)
                yield return x;
        }
    }

    private static void TryLinkParentEdge(FtPerson child, FtPerson parent)
    {
        var pn = FtText.NormName(parent.FullName);
        if (pn.Length == 0) return;
        if (child.FatherPersonId == null && FtText.NormName(child.FatherName) == pn)
            child.FatherPersonId = parent.DataId;
        else if (child.MotherPersonId == null && FtText.NormName(child.MotherName) == pn)
            child.MotherPersonId = parent.DataId;
    }

    public async Task<bool> WouldCycleAsync(int? selfId, int? fatherId, int? motherId, CancellationToken ct)
    {
        if (!fatherId.HasValue && !motherId.HasValue) return false;
        var all = await _db.FtPersons.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(ct);
        bool Reach(int from, int to)
        {
            var seen = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(from);
            while (stack.Count > 0)
            {
                var id = stack.Pop();
                if (!seen.Add(id)) continue;
                if (id == to) return true;
                foreach (var c in all.Where(x => x.FatherPersonId == id || x.MotherPersonId == id))
                    stack.Push(c.DataId);
            }
            return false;
        }
        if (selfId.HasValue && fatherId.HasValue && (fatherId == selfId || Reach(selfId.Value, fatherId.Value)))
            return true;
        if (selfId.HasValue && motherId.HasValue && (motherId == selfId || Reach(selfId.Value, motherId.Value)))
            return true;
        return false;
    }

    public Task<bool> CanExtendTreeAsync(int userId, CancellationToken ct) =>
        _duty.IsBranchAdminAsync(userId, ct);

    public static string RelativeKindText(string kind) => kind switch
    {
        "FATHER" => "父亲",
        "MOTHER" => "母亲",
        "CHILD" => "子女",
        _ => "亲属"
    };

    public async Task<FtLineageVm> GetLineageAsync(FtPerson p, CancellationToken ct)
    {
        var vm = new FtLineageVm
        {
            FromId = p.DataId,
            FromName = p.FullName,
            FatherLabel = p.FatherName,
            MotherLabel = p.MotherName ?? ""
        };
        if (p.FatherPersonId is int fid)
        {
            var f = await _db.FtPersons.AsNoTracking()
                .FirstOrDefaultAsync(x => x.DataId == fid && !x.IsDeleted, ct);
            if (f != null)
            {
                vm.FatherId = f.DataId;
                vm.FatherLabel = f.FullName;
            }
        }
        if (p.MotherPersonId is int mid)
        {
            var mo = await _db.FtPersons.AsNoTracking()
                .FirstOrDefaultAsync(x => x.DataId == mid && !x.IsDeleted, ct);
            if (mo != null)
            {
                vm.MotherId = mo.DataId;
                vm.MotherLabel = mo.FullName;
            }
        }
        vm.Children = (await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.FatherPersonId == p.DataId || x.MotherPersonId == p.DataId))
            .ToListAsync(ct))
            .OrderBy(x => x.BirthYear ?? FtText.ParseBirthYear(x.BirthDate) ?? 9999)
            .ThenBy(x => (x.BirthDate ?? "").Trim(), StringComparer.Ordinal)
            .ThenBy(x => x.FullName)
            .Select(x => new FtLineageLinkVm { DataId = x.DataId, FullName = x.FullName })
            .ToList();
        return vm;
    }

    public FtPersonFormVm PrefillRelative(FtPerson from, string kind)
    {
        var m = new FtPersonFormVm();
        if (kind == "FATHER")
        {
            m.FullName = from.FatherName ?? "";
            m.Gender = 1;
            m.GenerationNo = from.GenerationNo > 1 ? from.GenerationNo - 1 : 0;
        }
        else if (kind == "MOTHER")
        {
            m.FullName = from.MotherName ?? "";
            m.Gender = 0;
            m.GenerationNo = from.GenerationNo > 1 ? from.GenerationNo - 1 : 0;
        }
        else if (kind == "CHILD")
        {
            if (from.Gender == 0)
            {
                m.MotherName = from.FullName;
                m.FatherName = from.FatherName;
            }
            else
            {
                m.FatherName = from.FullName;
                m.MotherName = from.MotherName;
            }
            m.Gender = 1;
            m.GenerationNo = from.GenerationNo > 0 ? from.GenerationNo + 1 : 0;
        }
        return m;
    }

    public async Task<(int NewId, List<FtMatchHintVm> Hints)> AddRelativeAsync(
        int fromId, string kind, FtPersonFormVm m, int userId, string op, CancellationToken ct)
    {
        kind = (kind ?? "").Trim().ToUpperInvariant();
        if (kind is not ("FATHER" or "MOTHER" or "CHILD"))
            throw new InvalidOperationException("只能添加父亲、母亲或子女。");
        if (!await CanExtendTreeAsync(userId, ct))
            throw new InvalidOperationException("仅分支管理员或超管可向上补父辈、向下补子女。");

        var from = await GetAsync(fromId, ct) ?? throw new InvalidOperationException("人物不存在。");
        if (kind == "FATHER" && from.FatherPersonId.HasValue)
        {
            var exist = await GetAsync(from.FatherPersonId.Value, ct);
            if (exist != null)
                throw new InvalidOperationException("父亲档案已存在。请打开父亲页，再继续向上填写祖父。");
        }
        if (kind == "MOTHER" && from.MotherPersonId.HasValue)
        {
            var exist = await GetAsync(from.MotherPersonId.Value, ct);
            if (exist != null)
                throw new InvalidOperationException("母亲档案已存在。请打开母亲页，再继续向上填写。");
        }

        m.FatherPersonId = null;
        m.MotherPersonId = null;
        if (kind == "CHILD")
        {
            if (from.Gender == 0) m.MotherPersonId = from.DataId;
            else m.FatherPersonId = from.DataId;
        }
        else
        {
            var reused = await FindReuseAsync(userId, m.FullName, m.FatherName, m.MotherName, ct);
            if (reused != null)
            {
                var fid = kind == "FATHER" ? reused.DataId : from.FatherPersonId;
                var mid = kind == "MOTHER" ? reused.DataId : from.MotherPersonId;
                if (await WouldCycleAsync(from.DataId, fid, mid, ct))
                    throw new InvalidOperationException("父母关系形成环路，已拒绝。");
            }
        }

        var (pid, hints) = await CreateAsync(m, userId, op, ct);

        if (kind == "FATHER")
        {
            from.FatherPersonId = pid;
            from.FatherName = m.FullName;
        }
        else if (kind == "MOTHER")
        {
            from.MotherPersonId = pid;
            from.MotherName = m.FullName;
        }
        from.AmendDate = DateTime.Now;
        from.OperatorName = FtText.ClipReq(op, 30);
        await _db.SaveChangesAsync(ct);
        await _log.WriteAsync("ADD_" + kind, "Person", from.DataId.ToString(), userId, op,
            $"{from.FullName}->{m.FullName}", null, pid.ToString(), ct);
        return (pid, hints);
    }
}
