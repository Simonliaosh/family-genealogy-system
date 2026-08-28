using System.Text.Json;
using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public sealed class FtPersonDraftService
{
    private readonly FrameworkDbContext _db;
    private readonly FtPersonService _persons;
    private readonly FtDutyAccess _duty;
    private readonly FtMatchService _match;
    private readonly FtPersonLinkService _links;

    public FtPersonDraftService(
        FrameworkDbContext db,
        FtPersonService persons,
        FtDutyAccess duty,
        FtMatchService match,
        FtPersonLinkService links)
    {
        _db = db;
        _persons = persons;
        _duty = duty;
        _match = match;
        _links = links;
    }

    public static List<(string Value, string Text)> RelationOptions() =>
    [
        ("SELF", "本人"), ("FATHER", "父亲"), ("MOTHER", "母亲"),
        ("GRANDFATHER", "祖父（爷爷）"), ("GRANDMOTHER", "祖母（奶奶）"),
        ("CHILD", "子女"), ("SIBLING", "兄弟姐妹"),
        ("FATHER_SIB", "父亲的兄弟姐妹（伯叔姑）"),
        ("MOTHER_SIB", "母亲的兄弟姐妹（舅姨）")
    ];

    public static string RelationText(string? code)
    {
        foreach (var o in RelationOptions())
        {
            if (string.Equals(o.Value, code, StringComparison.OrdinalIgnoreCase))
                return o.Text;
        }
        return code ?? "";
    }

    private static readonly HashSet<string> BasicRelations =
        new(StringComparer.OrdinalIgnoreCase) { "SELF", "FATHER", "MOTHER", "CHILD", "SIBLING" };
    private static readonly HashSet<string> CollateralRelations =
        new(StringComparer.OrdinalIgnoreCase) { "FATHER_SIB", "MOTHER_SIB" };
    private static readonly HashSet<string> GrandparentRelations =
        new(StringComparer.OrdinalIgnoreCase) { "GRANDFATHER", "GRANDMOTHER" };

    public FtDraftFormVm ToForm(FtPersonDraft row)
    {
        var extra = TryReadJson(row.DraftJson);
        return new FtDraftFormVm
        {
            DataId = row.DataId,
            FullName = row.FullName,
            FatherName = row.FatherName,
            MotherName = row.MotherName,
            BirthDate = row.BirthDate,
            RelationType = row.RelationType,
            Gender = extra.Gender,
            GenerationNo = extra.GenerationNo,
            WordOfGeneration = extra.Word,
            Remark = row.Remark
        };
    }

    public void Normalize(FtDraftFormVm m)
    {
        m.FullName = FtText.ClipReq(m.FullName, 64);
        m.FatherName = FtText.ClipReq(m.FatherName, 64);
        m.MotherName = FtText.Clip(m.MotherName, 64);
        m.BirthDate = FtText.Clip(m.BirthDate, 32);
        m.RelationType = FtText.ClipReq(m.RelationType, 16, "SELF").ToUpperInvariant();
        m.WordOfGeneration = FtText.Clip(m.WordOfGeneration, 32);
        m.Remark = FtText.Clip(m.Remark, 512);
        if (!BasicRelations.Contains(m.RelationType) && !CollateralRelations.Contains(m.RelationType)
            && !GrandparentRelations.Contains(m.RelationType))
            m.RelationType = "SELF";
    }

    public async Task<(IReadOnlyList<FtDraftListRowVm> page, int total, int pages, int pageOut)> GetIndexPageAsync(
        int userId, string searchField, string searchContent, string sortField, string sortArrow,
        int page, int pageSize, CancellationToken ct)
    {
        var super = await _duty.IsSuperAsync(userId, ct);
        var rows = await _db.FtPersonDrafts.AsNoTracking()
            .Where(x => !x.IsDeleted && (super || x.SubmitUserId == userId))
            .ToListAsync(ct);
        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
            rows = rows.Where(x => x.FullName.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();
        rows = rows.OrderByDescending(x => x.AmendDate).ToList();
        var (slice, total, pages, p) = FtPaging.Page(rows, page, pageSize);
        var personIds = slice.Where(x => x.ResultPersonId.HasValue).Select(x => x.ResultPersonId!.Value).Distinct().ToList();
        var blockedPersonIds = await FindPersonsWithOthersAttachedAsync(personIds, ct);

        var vm = slice.Select(x =>
        {
            var pass = string.Equals(x.AuditStatus, "PASS", StringComparison.OrdinalIgnoreCase);
            var blocked = pass && x.ResultPersonId.HasValue && blockedPersonIds.Contains(x.ResultPersonId.Value);
            return new FtDraftListRowVm
            {
                DataId = x.DataId,
                FullName = x.FullName,
                RelationType = RelationText(x.RelationType),
                AuditStatus = x.AuditStatus,
                ResultPersonId = x.ResultPersonId,
                AmendDate = x.AmendDate,
                AllowDelete = !blocked
            };
        }).ToList();
        return (vm, total, pages, p);
    }

    public async Task<FtPersonDraft?> GetAsync(int id, int userId, CancellationToken ct)
    {
        var row = await _db.FtPersonDrafts.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return null;
        if (row.SubmitUserId != userId && !await _duty.IsSuperAsync(userId, ct)) return null;
        return row;
    }

    public async Task<int> SaveDraftAsync(FtDraftFormVm m, int userId, string op, CancellationToken ct)
    {
        var now = DateTime.Now;
        var json = JsonSerializer.Serialize(new { m.Gender, m.GenerationNo, Word = m.WordOfGeneration });
        FtPersonDraft row;
        if (m.DataId > 0)
        {
            row = await GetAsync(m.DataId, userId, ct) ?? throw new InvalidOperationException("草稿不存在。");
            if (row.AuditStatus == "PASS") throw new InvalidOperationException("已完成的记录不能再改。");
        }
        else
        {
            row = new FtPersonDraft
            {
                SubmitUserId = userId,
                CreateDate = now,
                AuditStatus = "DRAFT"
            };
            _db.FtPersonDrafts.Add(row);
        }
        row.FullName = m.FullName;
        row.FatherName = m.FatherName;
        row.MotherName = m.MotherName;
        row.BirthDate = m.BirthDate;
        row.RelationType = m.RelationType;
        row.DraftJson = json;
        row.Remark = m.Remark;
        row.AmendDate = now;
        row.OperatorName = FtText.ClipReq(op, 30);
        await _db.SaveChangesAsync(ct);
        return row.DataId;
    }

    /// <summary>入档前匹配：用草稿关键字对照已入档主谱人物。草稿字段不改。</summary>
    public async Task<FtDraftArchiveConfirmVm> BuildArchiveConfirmAsync(int draftId, int userId, CancellationToken ct)
    {
        var draft = await GetAsync(draftId, userId, ct) ?? throw new InvalidOperationException("草稿不存在。");
        if (draft.AuditStatus == "PASS")
            throw new InvalidOperationException("该草稿已入档。");
        await EnsureCanCompleteAsync(draft, userId, ct);

        var probe = new FtPerson
        {
            DataId = 0,
            FullName = draft.FullName,
            FatherName = draft.FatherName,
            MotherName = draft.MotherName,
            BirthDate = draft.BirthDate,
            BirthYear = FtText.ParseBirthYear(draft.BirthDate)
        };
        var hints = (await _match.MatchCoreAsync(probe, ct))
            .Where(h => h.InMain)
            .OrderBy(h => h.Level)
            .ToList();

        return new FtDraftArchiveConfirmVm
        {
            DraftId = draft.DataId,
            FullName = draft.FullName,
            FatherName = draft.FatherName,
            MotherName = draft.MotherName,
            BirthDate = draft.BirthDate,
            RelationType = draft.RelationType,
            RelationLabel = RelationText(draft.RelationType),
            Hints = hints
        };
    }

    /// <summary>无主谱命中或用户选择新建：写入人物档案；草稿仅改状态。</summary>
    public async Task<(int PersonId, string Message)> ArchiveAsNewAsync(int draftId, int userId, string op, CancellationToken ct)
    {
        var draft = await GetAsync(draftId, userId, ct) ?? throw new InvalidOperationException("草稿不存在。");
        if (draft.AuditStatus == "PASS" && draft.ResultPersonId.HasValue)
            return (draft.ResultPersonId.Value, "该草稿已入档。");

        var pid = await MaterializePersonFromDraftAsync(draft, userId, op, ct);
        MarkDraftPassed(draft, pid);
        await _db.SaveChangesAsync(ct);
        return (pid, $"已写入人物档案 #{pid}（独立建档）。");
    }

    /// <summary>确认与已入档主谱人为同一人：建档并以目标为关系依据申请加入家族链；草稿仅改状态。</summary>
    public async Task<(int PersonId, string Message, int LinkId)> ArchiveJoinChainAsync(
        int draftId, int targetId, byte? level, int userId, string op, CancellationToken ct)
    {
        var draft = await GetAsync(draftId, userId, ct) ?? throw new InvalidOperationException("草稿不存在。");
        if (draft.AuditStatus == "PASS" && draft.ResultPersonId.HasValue)
            return (draft.ResultPersonId.Value, "该草稿已入档。", 0);

        var tgt = await _db.FtPersons.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == targetId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("目标人物不存在。");
        if (!tgt.InMainGenealogy)
            throw new InvalidOperationException("只能按已入主谱的人物加入家族链。");

        var pid = await MaterializePersonFromDraftAsync(draft, userId, op, ct);
        var (linkId, linkMsg) = await _links.ApplyAsync(pid, targetId, userId, op, level, ct);
        MarkDraftPassed(draft, pid);
        await _db.SaveChangesAsync(ct);
        return (pid, $"已写入档案，并按「{tgt.FullName}」提交链入申请。" + linkMsg, linkId);
    }

    /// <summary>兼容旧调用：先匹配；有主谱命中时抛出引导信息由控制器跳确认页。建议改用 BuildArchiveConfirm / Archive*。</summary>
    public async Task<(int PersonId, List<FtMatchHintVm> Hints)> CompleteAsync(int draftId, int userId, string op, CancellationToken ct)
    {
        var draft = await GetAsync(draftId, userId, ct) ?? throw new InvalidOperationException("草稿不存在。");
        if (draft.AuditStatus == "PASS" && draft.ResultPersonId.HasValue)
            return (draft.ResultPersonId.Value, await _match.MatchPersonAsync(draft.ResultPersonId.Value, ct));

        var preview = await BuildArchiveConfirmAsync(draftId, userId, ct);
        if (preview.Hints.Count > 0)
            return (0, preview.Hints);

        var (pid, _) = await ArchiveAsNewAsync(draftId, userId, op, ct);
        return (pid, new List<FtMatchHintVm>());
    }

    private async Task EnsureCanCompleteAsync(FtPersonDraft draft, int userId, CancellationToken ct)
    {
        var isStaff = await _duty.IsSuperAsync(userId, ct) || await _duty.IsBranchAdminAsync(userId, ct);
        var isCollateral = CollateralRelations.Contains(draft.RelationType);
        var isGrandparent = GrandparentRelations.Contains(draft.RelationType);
        if (!isStaff)
        {
            if (isCollateral)
            {
                if (!await IsBoundCertifiedAsync(userId, ct))
                    throw new InvalidOperationException("须先完成族员认证，才能录入父母的兄弟姐妹（用于旁系串链）。");
            }
            else if (isGrandparent)
            {
                if (!await HasFatherPersonAsync(userId, ct))
                    throw new InvalidOperationException("请先录入父亲并保存入档，再录入祖父/祖母。");
            }
            else if (!BasicRelations.Contains(draft.RelationType))
                throw new InvalidOperationException("普通族人只能填本人、父母、祖父母、兄弟姐妹、子女；已认证可再填父母的兄弟姐妹。");
        }
        if (string.IsNullOrWhiteSpace(draft.FullName))
            throw new InvalidOperationException("请先保存草稿并填写姓名后再入档。");
    }

    private async Task<int> MaterializePersonFromDraftAsync(FtPersonDraft draft, int userId, string op, CancellationToken ct)
    {
        await EnsureCanCompleteAsync(draft, userId, ct);

        var isCollateral = CollateralRelations.Contains(draft.RelationType);
        var isGrandparent = GrandparentRelations.Contains(draft.RelationType);
        var extra = TryReadJson(draft.DraftJson);
        var form = new FtPersonFormVm
        {
            FullName = draft.FullName,
            FatherName = draft.FatherName,
            MotherName = draft.MotherName,
            BirthDate = draft.BirthDate,
            Gender = extra.Gender,
            GenerationNo = extra.GenerationNo,
            WordOfGeneration = extra.Word,
            Remark = draft.Remark
        };
        _persons.NormalizeFormForSave(form);

        var self = await _db.FtPersons.FirstOrDefaultAsync(x => x.BindUserId == userId && !x.IsDeleted, ct);
        if (draft.RelationType == "FATHER" && self != null) form.FatherPersonId = null;
        if (draft.RelationType == "CHILD" && self != null)
        {
            if (self.Gender == 1) form.FatherPersonId = self.DataId;
            else form.MotherPersonId = self.DataId;
        }
        if (draft.RelationType == "SIBLING" && self != null)
        {
            form.FatherPersonId = self.FatherPersonId;
            form.MotherPersonId = self.MotherPersonId;
        }
        if (isCollateral && self != null)
        {
            FtPerson? sideParent = null;
            if (draft.RelationType == "FATHER_SIB" && self.FatherPersonId is int fid)
                sideParent = await _persons.GetAsync(fid, ct);
            else if (draft.RelationType == "MOTHER_SIB" && self.MotherPersonId is int mid)
                sideParent = await _persons.GetAsync(mid, ct);

            if (sideParent != null)
            {
                form.FatherPersonId = sideParent.FatherPersonId;
                form.MotherPersonId = sideParent.MotherPersonId;
                if (string.IsNullOrWhiteSpace(form.FatherName))
                    form.FatherName = sideParent.FatherName ?? "";
                if (string.IsNullOrWhiteSpace(form.MotherName))
                    form.MotherName = sideParent.MotherName;
                if (form.GenerationNo <= 0 && sideParent.GenerationNo > 0)
                    form.GenerationNo = sideParent.GenerationNo;
            }
        }
        if (isGrandparent && self?.FatherPersonId is int fatherId)
        {
            var father = await _persons.GetAsync(fatherId, ct);
            if (father == null)
                throw new InvalidOperationException("请先录入父亲并保存入档，再录入祖父/祖母。");
            if (draft.RelationType == "GRANDFATHER" && father.FatherPersonId.HasValue)
                throw new InvalidOperationException("祖父档案已存在。");
            if (draft.RelationType == "GRANDMOTHER" && father.MotherPersonId.HasValue)
                throw new InvalidOperationException("祖母档案已存在。");
        }

        var (pid, _) = await _persons.CreateAsync(form, userId, op, ct);
        var person = await _persons.GetAsync(pid, ct);
        if (person != null && draft.RelationType == "SELF" && person.BindUserId == null)
        {
            person.BindUserId = userId;
            await _db.SaveChangesAsync(ct);
        }
        if (person != null && draft.RelationType == "FATHER" && self != null)
        {
            self.FatherPersonId = person.DataId;
            self.FatherName = person.FullName;
            await _db.SaveChangesAsync(ct);
        }
        if (person != null && draft.RelationType == "MOTHER" && self != null)
        {
            self.MotherPersonId = person.DataId;
            self.MotherName = person.FullName;
            await _db.SaveChangesAsync(ct);
        }
        if (person != null && isGrandparent && self?.FatherPersonId is int fpId)
        {
            var father = await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == fpId && !x.IsDeleted, ct);
            if (father != null)
            {
                if (draft.RelationType == "GRANDFATHER")
                {
                    father.FatherPersonId = person.DataId;
                    father.FatherName = person.FullName;
                }
                else
                {
                    father.MotherPersonId = person.DataId;
                    father.MotherName = person.FullName ?? "";
                }
                await _db.SaveChangesAsync(ct);
            }
        }
        return pid;
    }

    private async Task<bool> HasFatherPersonAsync(int userId, CancellationToken ct)
    {
        var self = await _db.FtPersons.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BindUserId == userId && !x.IsDeleted, ct);
        return self?.FatherPersonId is > 0;
    }

    private static void MarkDraftPassed(FtPersonDraft draft, int personId)
    {
        draft.AuditStatus = "PASS";
        draft.ResultPersonId = personId;
        draft.AmendDate = DateTime.Now;
        // 草稿字段（姓名/父母/DraftJson 等）保持入档前原样，仅改状态与结果人物
    }

    private async Task<bool> IsBoundCertifiedAsync(int userId, CancellationToken ct)
    {
        var self = await _db.FtPersons.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BindUserId == userId && !x.IsDeleted, ct);
        return self != null && self.IsCertified;
    }

    public async Task SoftDeleteAsync(int id, int userId, CancellationToken ct)
    {
        var row = await GetAsync(id, userId, ct) ?? throw new InvalidOperationException("记录不存在或无权删除。");
        if (string.Equals(row.AuditStatus, "PASS", StringComparison.OrdinalIgnoreCase) && row.ResultPersonId.HasValue)
        {
            if (await HasOthersAttachedAsync(row.ResultPersonId.Value, ct))
                throw new InvalidOperationException("此人已入档，且已有其他人挂入族谱，不能删除。");
        }
        row.IsDeleted = true;
        row.AmendDate = DateTime.Now;
        row.OperatorName = "FT-DEL";
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>是否已有「其他人」以该人物（或其等同主谱目标）为接点挂入。</summary>
    public async Task<bool> HasOthersAttachedAsync(int personId, CancellationToken ct)
    {
        var blocked = await FindPersonsWithOthersAttachedAsync(new[] { personId }, ct);
        return blocked.Contains(personId);
    }

    private async Task<HashSet<int>> FindPersonsWithOthersAttachedAsync(IReadOnlyList<int> personIds, CancellationToken ct)
    {
        var result = new HashSet<int>();
        if (personIds.Count == 0) return result;

        var people = await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted && personIds.Contains(x.DataId))
            .Select(x => new { x.DataId, x.OwnerUserId, x.SameAsPersonId })
            .ToListAsync(ct);
        if (people.Count == 0) return result;

        // 检查范围：本人节点 + 已等同的主谱目标
        var checkIds = people.Select(x => x.DataId).ToList();
        foreach (var sa in people.Where(x => x.SameAsPersonId.HasValue).Select(x => x.SameAsPersonId!.Value))
        {
            if (!checkIds.Contains(sa)) checkIds.Add(sa);
        }

        var links = await _db.FtPersonLinks.AsNoTracking()
            .Where(x => !x.IsDeleted
                        && checkIds.Contains(x.TargetMainPersonId)
                        && (x.LinkStatus == "PENDING" || x.LinkStatus == "EFFECTIVE"))
            .Select(x => new { x.TargetMainPersonId, x.ApplyUserId })
            .ToListAsync(ct);

        var childOwners = await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted
                        && (x.FatherPersonId != null && checkIds.Contains(x.FatherPersonId.Value)
                            || x.MotherPersonId != null && checkIds.Contains(x.MotherPersonId.Value)))
            .Select(x => new { x.OwnerUserId, x.FatherPersonId, x.MotherPersonId })
            .ToListAsync(ct);

        var sameAsOwners = await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted && x.SameAsPersonId != null && checkIds.Contains(x.SameAsPersonId.Value))
            .Select(x => new { x.OwnerUserId, SameAs = x.SameAsPersonId!.Value })
            .ToListAsync(ct);

        foreach (var p in people)
        {
            var owner = p.OwnerUserId;
            var related = new HashSet<int> { p.DataId };
            if (p.SameAsPersonId.HasValue) related.Add(p.SameAsPersonId.Value);

            var otherLink = links.Any(l => related.Contains(l.TargetMainPersonId) && l.ApplyUserId != owner);
            var otherChild = childOwners.Any(c =>
                c.OwnerUserId != owner
                && ((c.FatherPersonId.HasValue && related.Contains(c.FatherPersonId.Value))
                    || (c.MotherPersonId.HasValue && related.Contains(c.MotherPersonId.Value))));
            var otherSame = sameAsOwners.Any(s => related.Contains(s.SameAs) && s.OwnerUserId != owner);

            if (otherLink || otherChild || otherSame)
                result.Add(p.DataId);
        }

        return result;
    }

    private static (byte Gender, int GenerationNo, string? Word) TryReadJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            var r = doc.RootElement;
            byte g = 1;
            var n = 0;
            string? w = null;
            if (r.TryGetProperty("Gender", out var gp) && gp.TryGetByte(out var gb)) g = gb;
            if (r.TryGetProperty("GenerationNo", out var np) && np.TryGetInt32(out var ni)) n = ni;
            if (r.TryGetProperty("Word", out var wp)) w = wp.GetString();
            return (g, n, w);
        }
        catch
        {
            return (1, 0, null);
        }
    }
}
