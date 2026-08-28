using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public sealed class FtGenerationWordService
{
    private readonly FrameworkDbContext _db;
    public FtGenerationWordService(FrameworkDbContext db) => _db = db;

    public FtGenWordFormVm ToForm(FtGenerationWord row) => new()
    {
        DataId = row.DataId,
        SeqNo = row.SeqNo,
        Word = row.Word,
        BStatus = EBStatusHelper.NormalizeBStatusForSave(row.BStatus),
        Remark = row.Remark
    };

    public void Normalize(FtGenWordFormVm m)
    {
        m.Word = FtText.ClipReq(m.Word, 16);
        m.Remark = FtText.Clip(m.Remark, 512);
        m.BStatus = EBStatusHelper.NormalizeBStatusForSave(m.BStatus);
    }

    public async Task<(IReadOnlyList<FtGenWordListRowVm> page, int total, int pages, int pageOut)> GetIndexPageAsync(
        string? bStatus1, string searchField, string searchContent, string sortField, string sortArrow,
        int page, int pageSize, CancellationToken ct)
    {
        var rows = await _db.FtGenerationWords.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(ct);
        if (bStatus1 == "1") rows = rows.Where(x => EBStatusHelper.IsActiveBStatus(x.BStatus)).ToList();
        if (bStatus1 == "2") rows = rows.Where(x => !EBStatusHelper.IsActiveBStatus(x.BStatus)).ToList();
        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
            rows = rows.Where(x => x.Word.Contains(kw, StringComparison.OrdinalIgnoreCase) || (x.Remark ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();
        rows = (sortField, sortArrow) switch
        {
            ("Word", "1") => rows.OrderByDescending(x => x.Word).ToList(),
            ("Word", _) => rows.OrderBy(x => x.Word).ToList(),
            ("AmendDate", _) => rows.OrderByDescending(x => x.AmendDate).ToList(),
            _ => rows.OrderBy(x => x.SeqNo).ToList()
        };
        var (slice, total, pages, p) = FtPaging.Page(rows, page, pageSize);
        var vm = slice.Select(x => new FtGenWordListRowVm
        {
            DataId = x.DataId,
            SeqNo = x.SeqNo,
            Word = x.Word,
            BStatusText = EBStatusHelper.IsActiveBStatus(x.BStatus) ? "启用" : "停用",
            Remark = x.Remark,
            AmendDate = x.AmendDate
        }).ToList();
        return (vm, total, pages, p);
    }

    public Task<FtGenerationWord?> GetAsync(int id, CancellationToken ct) =>
        _db.FtGenerationWords.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);

    public async Task CreateAsync(FtGenWordFormVm m, string op, CancellationToken ct)
    {
        var now = DateTime.Now;
        _db.FtGenerationWords.Add(new FtGenerationWord
        {
            SeqNo = m.SeqNo,
            Word = m.Word,
            BStatus = m.BStatus,
            Remark = m.Remark,
            CreateDate = now,
            AmendDate = now,
            OperatorName = FtText.ClipReq(op, 30)
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task TryUpdateAsync(int id, FtGenWordFormVm m, string op, CancellationToken ct)
    {
        var row = await GetAsync(id, ct) ?? throw new InvalidOperationException("记录不存在。");
        row.SeqNo = m.SeqNo;
        row.Word = m.Word;
        row.BStatus = m.BStatus;
        row.Remark = m.Remark;
        row.AmendDate = DateTime.Now;
        row.OperatorName = FtText.ClipReq(op, 30);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var row = await GetAsync(id, ct);
        if (row == null) return;
        row.IsDeleted = true;
        row.BStatus = "2";
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(int[] ids, CancellationToken ct)
    {
        foreach (var id in ids.Distinct())
            await DeleteAsync(id, ct);
    }
}

public sealed class FtTreeService
{
    private readonly FrameworkDbContext _db;
    private readonly FtPeerService _peers;
    private readonly FtDutyAccess _duty;
    private readonly FtClanService _clans;
    public FtTreeService(FrameworkDbContext db, FtPeerService peers, FtDutyAccess duty, FtClanService clans)
    {
        _db = db;
        _peers = peers;
        _duty = duty;
        _clans = clans;
    }

    public async Task<List<(int Id, string Name)>> CurrentRootsAsync(CancellationToken ct, int? clanId = null)
    {
        var q = _db.FtPersons.AsNoTracking().Where(x => !x.IsDeleted && x.InMainGenealogy && x.SameAsPersonId == null);
        if (clanId is int cid && cid > 0)
            q = q.Where(x => x.ClanId == cid);
        var all = await q.ToListAsync(ct);
        return RootsFromPersons(all, requireMain: false);
    }

    /// <summary>在给定人物集合内找「当前根」（无父 ID，且父名也无法对上集合内某人）。</summary>
    public static List<(int Id, string Name)> RootsFromPersons(IEnumerable<FtPerson> persons, bool requireMain)
    {
        var list = persons.Where(x => x.SameAsPersonId == null).ToList();
        if (requireMain) list = list.Where(x => x.InMainGenealogy).ToList();
        var ids = list.Select(x => x.DataId).ToHashSet();
        var byName = list
            .GroupBy(x => FtText.NormName(x.FullName), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.Select(x => x.DataId).ToHashSet(), StringComparer.OrdinalIgnoreCase);

        bool HasFatherInSet(FtPerson x)
        {
            if (x.FatherPersonId.HasValue && ids.Contains(x.FatherPersonId.Value)) return true;
            if (x.FatherPersonId.HasValue) return false;
            var fn = FtText.NormName(x.FatherName);
            if (fn.Length == 0) return false;
            return byName.TryGetValue(fn, out var ps) && ps.Any(id => id != x.DataId);
        }

        return list.Where(x => !HasFatherInSet(x))
            .OrderBy(x => x.FullName)
            .Select(x => (x.DataId, x.FullName))
            .ToList();
    }

    public async Task<FtTreeNodeVm?> BuildDownAsync(int rootId, CancellationToken ct, bool withPeerStubs = true,
        IReadOnlySet<int>? allowIds = null, bool mainGenealogyOnly = false)
    {
        var all = await _db.FtPersons.AsNoTracking().Where(x => !x.IsDeleted && x.SameAsPersonId == null).ToListAsync(ct);
        if (allowIds != null)
            all = all.Where(x => allowIds.Contains(x.DataId)).ToList();

        if (mainGenealogyOnly)
        {
            var pendingSources = await _db.FtPersonLinks.AsNoTracking()
                .Where(x => !x.IsDeleted && x.LinkStatus == "PENDING")
                .Select(x => x.SourcePersonId)
                .ToListAsync(ct);
            var pendingSet = pendingSources.ToHashSet();
            // 主谱树：只展已入主谱，且排除链入待审源人（即使误标了 InMain）
            all = all.Where(x => x.InMainGenealogy && !pendingSet.Contains(x.DataId)).ToList();
        }

        var map = all.ToDictionary(x => x.DataId);
        if (!map.ContainsKey(rootId)) return null;

        FtTreeNodeVm Build(int id, int depth)
        {
            var p = map[id];
            var node = new FtTreeNodeVm
            {
                Id = p.DataId,
                NodeKey = "local:" + p.DataId,
                Name = p.FullName,
                Birth = p.BirthDate
            };
            if (depth > 40) return node;
            foreach (var c in OrderSiblingsByAge(ChildrenForTree(p, all, mainGenealogyOnly)))
                node.Children.Add(Build(c.DataId, depth + 1));
            return node;
        }
        var root = Build(rootId, 0);
        if (withPeerStubs)
            await _peers.AttachPeerStubsAsync(root, ct);
        return root;
    }

    /// <summary>树上子女。主谱模式仅认父子 ID、只收已入主谱，并按姓名+出生去重，避免未审批/重复节点混进主谱。</summary>
    public static List<FtPerson> ChildrenForTree(FtPerson parent, List<FtPerson> all, bool mainGenealogyOnly = false)
    {
        var pn = FtText.NormName(parent.FullName);
        var list = new List<FtPerson>();
        var seen = new HashSet<int>();
        foreach (var x in all)
        {
            if (x.DataId == parent.DataId) continue;
            var byId = x.FatherPersonId == parent.DataId || x.MotherPersonId == parent.DataId;
            var byFatherName = !x.FatherPersonId.HasValue && pn.Length > 0 &&
                               string.Equals(FtText.NormName(x.FatherName), pn, StringComparison.OrdinalIgnoreCase);
            var byMotherName = !x.MotherPersonId.HasValue && pn.Length > 0 &&
                               string.Equals(FtText.NormName(x.MotherName), pn, StringComparison.OrdinalIgnoreCase);

            if (mainGenealogyOnly)
            {
                if (!byId || !x.InMainGenealogy) continue;
            }
            else
            {
                // 个人小树：允许「主谱父母 ← 未入谱子女」的引用边（双方都在 allow 集内即可）
                if (!byId && !byFatherName && !byMotherName) continue;
            }

            if (!seen.Add(x.DataId)) continue;
            list.Add(x);
        }

        return DedupeSiblingByNameBirth(list);
    }

    /// <summary>同父下姓名+出生年相同的多条，保留已挂父子 ID 的一条，避免主谱双影。</summary>
    public static List<FtPerson> DedupeSiblingByNameBirth(List<FtPerson> list)
    {
        if (list.Count <= 1) return list;
        return list
            .GroupBy(x =>
            {
                var n = FtText.NormName(x.FullName);
                var y = x.BirthYear ?? FtText.ParseBirthYear(x.BirthDate);
                return n + "|" + (y?.ToString() ?? (x.BirthDate ?? "").Trim());
            }, StringComparer.OrdinalIgnoreCase)
            .Select(g => g
                .OrderByDescending(x => x.FatherPersonId.HasValue || x.MotherPersonId.HasValue)
                .ThenByDescending(x => x.InMainGenealogy)
                .ThenBy(x => x.DataId)
                .First())
            .ToList();
    }

    /// <summary>可见集内把唯一可确定的父/母姓名写成 ID 边。未入主谱者不得补挂到主谱父母。</summary>
    public async Task<int> HealParentEdgesAsync(IReadOnlySet<int>? allowIds, CancellationToken ct)
    {
        var all = await _db.FtPersons.Where(x => !x.IsDeleted && x.SameAsPersonId == null).ToListAsync(ct);
        if (allowIds != null)
            all = all.Where(x => allowIds.Contains(x.DataId)).ToList();

        var byName = all
            .GroupBy(x => FtText.NormName(x.FullName), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var n = 0;
        foreach (var c in all)
        {
            if (!c.FatherPersonId.HasValue)
            {
                var fn = FtText.NormName(c.FatherName);
                if (fn.Length > 0 && byName.TryGetValue(fn, out var cands))
                {
                    var pick = PickUniqueParent(cands, c.DataId, c.InMainGenealogy);
                    if (pick != null)
                    {
                        c.FatherPersonId = pick.DataId;
                        c.AmendDate = DateTime.Now;
                        n++;
                    }
                }
            }
            if (!c.MotherPersonId.HasValue)
            {
                var mn = FtText.NormName(c.MotherName);
                if (mn.Length > 0 && byName.TryGetValue(mn, out var cands))
                {
                    var pick = PickUniqueParent(cands, c.DataId, c.InMainGenealogy);
                    if (pick != null)
                    {
                        c.MotherPersonId = pick.DataId;
                        c.AmendDate = DateTime.Now;
                        n++;
                    }
                }
            }
        }
        if (n > 0) await _db.SaveChangesAsync(ct);
        return n;
    }

    private static FtPerson? PickUniqueParent(List<FtPerson> cands, int childId, bool childInMain)
    {
        var list = cands.Where(x => x.DataId != childId).ToList();
        if (list.Count == 0) return null;
        if (!childInMain)
        {
            var personal = list.Where(x => !x.InMainGenealogy).ToList();
            return personal.Count == 1 ? personal[0] : null;
        }
        var main = list.Where(x => x.InMainGenealogy).ToList();
        if (main.Count == 1) return main[0];
        if (main.Count > 1) return null;
        return list.Count == 1 ? list[0] : null;
    }

    /// <summary>
    /// 个人小树可见集：本人录入/绑定 + 其 FatherPersonId/MotherPersonId 指向的人（按 ID，挂上哪个显示哪个）。
    /// </summary>
    public async Task<(List<FtPerson> Pool, HashSet<int> AllowIds, List<(int Id, string Name)> Roots)>
        BuildPersonalTreeScopeAsync(IReadOnlyList<FtPerson> mine, CancellationToken ct)
    {
        var allow = mine.Select(x => x.DataId).ToHashSet();
        foreach (var p in mine)
        {
            if (p.FatherPersonId is int f) allow.Add(f);
            if (p.MotherPersonId is int m) allow.Add(m);
        }

        var missing = allow.Where(id => mine.All(x => x.DataId != id)).ToList();
        var refs = missing.Count == 0
            ? new List<FtPerson>()
            : await _db.FtPersons.AsNoTracking()
                .Where(x => !x.IsDeleted && x.SameAsPersonId == null && missing.Contains(x.DataId))
                .ToListAsync(ct);

        var pool = mine.Concat(refs).GroupBy(x => x.DataId).Select(g => g.First()).ToList();
        var roots = RootsFromPersons(pool, requireMain: false)
            .Select(r =>
            {
                var p = pool.FirstOrDefault(x => x.DataId == r.Id);
                var label = r.Name;
                if (p != null && p.InMainGenealogy && mine.All(x => x.DataId != p.DataId))
                    label = p.FullName + "（主谱引用）";
                return (r.Id, label);
            })
            .ToList();
        return (pool, allow, roots);
    }

    public async Task<FtTreeNodeVm?> BuildUpAsync(int personId, CancellationToken ct, IReadOnlySet<int>? allowIds = null)
    {
        var all = await _db.FtPersons.AsNoTracking().Where(x => !x.IsDeleted && x.SameAsPersonId == null).ToListAsync(ct);
        if (allowIds != null)
            all = all.Where(x => allowIds.Contains(x.DataId)).ToList();
        var map = all.ToDictionary(x => x.DataId);
        if (!map.ContainsKey(personId)) return null;
        FtTreeNodeVm? child = null;
        var cur = personId;
        var guard = 0;
        while (guard++ < 40)
        {
            var p = map[cur];
            var node = new FtTreeNodeVm
            {
                Id = p.DataId,
                NodeKey = "local:" + p.DataId,
                Name = p.FullName,
                Birth = p.BirthDate
            };
            if (child != null) node.Children.Add(child);
            child = node;
            if (!p.FatherPersonId.HasValue || !map.ContainsKey(p.FatherPersonId.Value))
                break;
            cur = p.FatherPersonId.Value;
        }
        return child;
    }

    public async Task<List<FtTreeNodeVm>> SiblingsAsync(int personId, CancellationToken ct, IReadOnlySet<int>? allowIds = null)
    {
        var all = await _db.FtPersons.AsNoTracking().Where(x => !x.IsDeleted && x.SameAsPersonId == null).ToListAsync(ct);
        if (allowIds != null)
            all = all.Where(x => allowIds.Contains(x.DataId)).ToList();
        var me = all.FirstOrDefault(x => x.DataId == personId);
        if (me == null) return [];
        return OrderSiblingsByAge(all.Where(x =>
                (me.FatherPersonId.HasValue && x.FatherPersonId == me.FatherPersonId)
                || (me.MotherPersonId.HasValue && x.MotherPersonId == me.MotherPersonId)))
            .Select(x => new FtTreeNodeVm
            {
                Id = x.DataId,
                NodeKey = "local:" + x.DataId,
                Name = x.FullName,
                Birth = x.BirthDate
            })
            .ToList();
    }

    /// <summary>同辈按年龄从大到小（年长在前）；无出生信息的排后面。</summary>
    public static IEnumerable<FtPerson> OrderSiblingsByAge(IEnumerable<FtPerson> siblings)
    {
        return siblings
            .OrderBy(BirthSortKey)
            .ThenBy(x => (x.BirthDate ?? "").Trim(), StringComparer.Ordinal)
            .ThenBy(x => x.FullName);
    }

    private static int BirthSortKey(FtPerson x)
    {
        var y = x.BirthYear ?? FtText.ParseBirthYear(x.BirthDate);
        // 有年份：越小越年长，排越前；无年份：排最后
        return y ?? 9999;
    }

    public async Task<int?> PersonalRootAsync(int userId, CancellationToken ct, IReadOnlySet<int>? allowIds = null)
    {
        var self = await _db.FtPersons.AsNoTracking().FirstOrDefaultAsync(x => x.BindUserId == userId && !x.IsDeleted, ct);
        if (self == null) return null;
        // 已链入：展示用主谱目标节点，再向上找自己的家族链顶
        var startId = self.SameAsPersonId ?? self.DataId;
        if (allowIds != null && !allowIds.Contains(startId))
        {
            if (!allowIds.Contains(self.DataId)) return null;
            startId = self.DataId;
        }
        var all = await _db.FtPersons.AsNoTracking().Where(x => !x.IsDeleted && x.SameAsPersonId == null).ToListAsync(ct);
        if (allowIds != null)
            all = all.Where(x => allowIds.Contains(x.DataId)).ToList();
        var map = all.ToDictionary(x => x.DataId);
        if (!map.ContainsKey(startId) && self.SameAsPersonId.HasValue && map.ContainsKey(self.SameAsPersonId.Value))
            startId = self.SameAsPersonId.Value;
        if (!map.ContainsKey(startId)) return allowIds == null || allowIds.Contains(startId) ? startId : null;
        var cur = startId;
        var guard = 0;
        while (guard++ < 40 && map.TryGetValue(cur, out var p) && p.FatherPersonId.HasValue && map.ContainsKey(p.FatherPersonId.Value))
            cur = p.FatherPersonId.Value;
        return cur;
    }

    /// <summary>未指定 rootId 时：优先本人家族链顶，再回落到根列表首项。</summary>
    public async Task<int> PickDefaultRootAsync(
        int userId,
        IReadOnlyList<(int Id, string Name)> roots,
        IReadOnlySet<int>? allowIds,
        CancellationToken ct)
    {
        var personal = await PersonalRootAsync(userId, ct, allowIds);
        if (personal.GetValueOrDefault() > 0 &&
            (allowIds == null || allowIds.Contains(personal!.Value)))
            return personal.Value;
        return roots.Count > 0 ? roots[0].Id : 0;
    }

    /// <summary>
    /// 确保「我的家族链」根出现在下拉中，并排在最前，便于默认选中。
    /// </summary>
    public async Task<List<(int Id, string Name)>> PreferPersonalRootInListAsync(
        int userId,
        List<(int Id, string Name)> roots,
        IReadOnlySet<int>? allowIds,
        int preferredRootId,
        CancellationToken ct)
    {
        if (preferredRootId <= 0) return roots;
        var idx = roots.FindIndex(x => x.Id == preferredRootId);
        if (idx == 0) return roots;
        if (idx > 0)
        {
            var item = roots[idx];
            roots.RemoveAt(idx);
            roots.Insert(0, (item.Id, item.Name.Contains("我的") ? item.Name : item.Name + "（我的）"));
            return roots;
        }
        var name = await _db.FtPersons.AsNoTracking()
            .Where(x => x.DataId == preferredRootId && !x.IsDeleted)
            .Select(x => x.FullName)
            .FirstOrDefaultAsync(ct) ?? preferredRootId.ToString();
        if (allowIds != null && !allowIds.Contains(preferredRootId)) return roots;
        // 主谱下拉不插入未入主谱的个人根（改走 scope=personal）
        var inMain = await _db.FtPersons.AsNoTracking()
            .AnyAsync(x => x.DataId == preferredRootId && !x.IsDeleted && x.InMainGenealogy && x.SameAsPersonId == null, ct);
        if (!inMain) return roots;
        roots.Insert(0, (preferredRootId, name + "（我的）"));
        return roots;
    }

    /// <summary>
    /// 按用户解析树范围：未入主谱→个人小树；已链入/纳入或管理岗→主谱（可见集）。
    /// </summary>
    public async Task<(List<(int Id, string Name)> Roots, HashSet<int>? AllowIds, bool MainMode)> ResolveScopeAsync(
        int userId, FtPersonService persons, CancellationToken ct)
    {
        if (await persons.IsStaffAsync(userId, ct))
        {
            var clanFilter = await _clans.GetUserClanIdAsync(userId, ct);
            if (clanFilter == null)
            {
                var mine = await persons.OwnedOrBoundPersonsStrictAsync(userId, ct);
                return (RootsFromPersons(mine, requireMain: false), mine.Select(x => x.DataId).ToHashSet(), false);
            }
            return (await CurrentRootsAsync(ct, clanFilter), null, true);
        }

        if (await persons.UserHasJoinedMainAsync(userId, ct))
        {
            var visible = await persons.VisiblePersonsAsync(userId, ct);
            var allow = visible.Select(x => x.DataId).ToHashSet();
            var roots = RootsFromPersons(visible, requireMain: true);
            return (roots, allow, true);
        }

        var mineOnly = await persons.OwnedOrBoundPersonsAsync(userId, ct);
        var mineIds = mineOnly.Select(x => x.DataId).ToHashSet();
        return (RootsFromPersons(mineOnly, requireMain: false), mineIds, false);
    }
}

public sealed class FtPersonMarryService
{
    private readonly FrameworkDbContext _db;
    private readonly FtPersonService _persons;
    public FtPersonMarryService(FrameworkDbContext db, FtPersonService persons)
    {
        _db = db;
        _persons = persons;
    }

    public static List<(string Value, string Text)> MarryTypes() =>
        [("原配", "原配"), ("续弦", "续弦"), ("侧室", "侧室"), ("再婚", "再婚"), ("离异", "离异"), ("入赘", "入赘")];

    public void Normalize(FtMarryFormVm m)
    {
        m.SpouseName = FtText.Clip(m.SpouseName, 64);
        m.SpouseBirth = FtText.Clip(m.SpouseBirth, 32);
        m.MarryType = FtText.ClipReq(m.MarryType, 32, "原配");
        if (!MarryTypes().Exists(t => t.Value == m.MarryType))
            m.MarryType = "原配";
        if (m.HouseSeq <= 0) m.HouseSeq = 99;
        if (m.SpousePersonId.GetValueOrDefault() <= 0) m.SpousePersonId = null;
        m.Remark = FtText.Clip(m.Remark, 512);
    }

    public async Task<(IReadOnlyList<FtMarryListRowVm> page, int total, int pages, int pageOut)> GetIndexPageAsync(
        int userId, int? personId, string? marryType1, string searchField, string searchContent,
        string sortField, string sortArrow, int page, int pageSize, CancellationToken ct)
    {
        var visibleList = await _persons.OwnedOrBoundPersonsAsync(userId, ct);
        var visible = visibleList.ToDictionary(x => x.DataId);
        var editable = await _persons.EditableIdsAsync(userId, visibleList, ct);
        var rows = await _db.FtPersonMarrys.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(ct);
        rows = rows.Where(x => visible.ContainsKey(x.PersonId)).ToList();
        if (personId.GetValueOrDefault() > 0)
        {
            var filterPid = personId!.Value;
            // 普通族人即使带 personId，也不得查看别人录入的人物的配偶
            if (!visible.ContainsKey(filterPid))
                rows = new List<FtPersonMarry>();
            else
                rows = rows.Where(x => x.PersonId == filterPid).ToList();
        }
        var mt = (marryType1 ?? "").Trim();
        if (mt.Length > 0)
            rows = rows.Where(x => string.Equals(x.MarryType, mt, StringComparison.OrdinalIgnoreCase)).ToList();
        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            rows = (searchField ?? "") switch
            {
                "PersonName" => rows.Where(x => visible[x.PersonId].FullName.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "Remark" => rows.Where(x => (x.Remark ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows.Where(x => (x.SpouseName ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList()
            };
        }
        rows = (sortField, sortArrow) switch
        {
            ("SpouseName", "1") => rows.OrderByDescending(x => x.SpouseName).ToList(),
            ("SpouseName", _) => rows.OrderBy(x => x.SpouseName).ToList(),
            ("PersonName", "1") => rows.OrderByDescending(x => visible[x.PersonId].FullName).ToList(),
            ("PersonName", _) => rows.OrderBy(x => visible[x.PersonId].FullName).ToList(),
            ("AmendDate", "1") => rows.OrderBy(x => x.AmendDate).ToList(),
            ("AmendDate", _) => rows.OrderByDescending(x => x.AmendDate).ToList(),
            ("HouseSeq", "1") => rows.OrderByDescending(x => x.HouseSeq).ToList(),
            _ => rows.OrderBy(x => x.HouseSeq).ThenBy(x => x.DataId).ToList()
        };
        var (slice, total, pages, p) = FtPaging.Page(rows, page, pageSize);
        var vm = slice.Select(x => new FtMarryListRowVm
        {
            DataId = x.DataId,
            PersonId = x.PersonId,
            PersonName = visible[x.PersonId].FullName,
            SpouseName = x.SpouseName,
            SpouseBirth = x.SpouseBirth,
            MarryType = x.MarryType,
            HouseSeq = x.HouseSeq,
            SpousePersonId = x.SpousePersonId,
            AmendDate = x.AmendDate,
            CanEdit = editable.Contains(x.PersonId)
        }).ToList();
        return (vm, total, pages, p);
    }

    public async Task<FtPersonMarry?> GetAsync(int id, CancellationToken ct) =>
        await _db.FtPersonMarrys.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);

    public FtMarryFormVm ToForm(FtPersonMarry row, string? personName = null) => new()
    {
        DataId = row.DataId,
        PersonId = row.PersonId,
        PersonName = personName,
        SpouseName = row.SpouseName,
        SpouseBirth = row.SpouseBirth,
        MarryType = row.MarryType,
        HouseSeq = row.HouseSeq,
        SpousePersonId = row.SpousePersonId,
        Remark = row.Remark
    };

    public async Task<List<(int Id, string Name)>> EditablePersonOptionsAsync(int userId, CancellationToken ct)
    {
        var all = await _persons.OwnedOrBoundPersonsAsync(userId, ct);
        var editIds = await _persons.EditableIdsAsync(userId, all, ct);
        return all.Where(p => editIds.Contains(p.DataId))
            .OrderBy(x => x.FullName)
            .Take(500)
            .Select(p => (Id: p.DataId, Name: p.FullName + " #" + p.DataId))
            .ToList();
    }

    public async Task SaveAsync(FtMarryFormVm m, int userId, string op, CancellationToken ct)
    {
        Normalize(m);
        var now = DateTime.Now;
        FtPersonMarry row;
        if (m.DataId > 0)
        {
            row = await _db.FtPersonMarrys.FirstOrDefaultAsync(x => x.DataId == m.DataId && !x.IsDeleted, ct)
                ?? throw new InvalidOperationException("记录不存在。");
            m.PersonId = row.PersonId;
        }
        else
        {
            row = new FtPersonMarry { PersonId = m.PersonId, CreateDate = now };
            _db.FtPersonMarrys.Add(row);
        }
        var person = await _persons.GetAsync(m.PersonId, ct) ?? throw new InvalidOperationException("人物不存在。");
        if (!await _persons.CanEditAsync(userId, person, ct))
            throw new InvalidOperationException("无权维护配偶信息。");
        if (m.SpousePersonId.HasValue)
        {
            var spouse = await _persons.GetAsync(m.SpousePersonId.Value, ct);
            if (spouse == null)
                throw new InvalidOperationException("库内配偶编号不存在。");
        }
        row.SpouseName = m.SpouseName;
        row.SpouseBirth = m.SpouseBirth;
        row.MarryType = m.MarryType;
        row.HouseSeq = m.HouseSeq;
        row.SpousePersonId = m.SpousePersonId;
        row.Remark = m.Remark;
        row.AmendDate = now;
        row.OperatorName = FtText.ClipReq(op, 30);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, int userId, CancellationToken ct)
    {
        var row = await _db.FtPersonMarrys.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return;
        var person = await _persons.GetAsync(row.PersonId, ct);
        if (person == null || !await _persons.CanEditAsync(userId, person, ct))
            throw new InvalidOperationException("无权删除。");
        row.IsDeleted = true;
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }
}
