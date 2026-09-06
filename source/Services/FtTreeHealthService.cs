using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>谱系体检：生卒/年龄矛盾、断边、自环等清单。</summary>
public sealed class FtTreeHealthService
{
    private readonly FrameworkDbContext _db;
    private readonly FtDutyAccess _duty;

    private sealed record PersonSnap(
        int DataId,
        string FullName,
        string? BirthDate,
        int? BirthYear,
        string? DeathInfo,
        int? FatherPersonId,
        int? MotherPersonId);

    public FtTreeHealthService(FrameworkDbContext db, FtDutyAccess duty)
    {
        _db = db;
        _duty = duty;
    }

    public async Task<bool> CanViewAsync(int userId, CancellationToken ct) =>
        await _duty.IsSuperAsync(userId, ct) || await _duty.IsBranchAdminAsync(userId, ct);

    public async Task<List<FtTreeHealthIssueVm>> ScanAsync(CancellationToken ct)
    {
        var people = await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted && x.SameAsPersonId == null)
            .Select(x => new PersonSnap(
                x.DataId,
                x.FullName,
                x.BirthDate,
                x.BirthYear,
                x.DeathInfo,
                x.FatherPersonId,
                x.MotherPersonId))
            .ToListAsync(ct);

        var byId = people.ToDictionary(x => x.DataId);
        var issues = new List<FtTreeHealthIssueVm>();

        foreach (var p in people)
        {
            var by = p.BirthYear ?? FtText.ParseBirthYear(p.BirthDate);
            var dy = FtText.ParseBirthYear(p.DeathInfo);

            if (by.HasValue && dy.HasValue && dy.Value < by.Value)
            {
                issues.Add(new FtTreeHealthIssueVm
                {
                    IssueType = "DEATH_BEFORE_BIRTH",
                    Severity = "ERROR",
                    Title = "卒年早于生年",
                    Detail = $"生 {by} / 卒约 {dy}（DeathInfo={p.DeathInfo}）",
                    PersonId = p.DataId,
                    PersonName = p.FullName
                });
            }

            if (p.FatherPersonId == p.DataId || p.MotherPersonId == p.DataId)
            {
                issues.Add(new FtTreeHealthIssueVm
                {
                    IssueType = "SELF_PARENT",
                    Severity = "ERROR",
                    Title = "父母指向自己",
                    Detail = "父子/母子边形成自环",
                    PersonId = p.DataId,
                    PersonName = p.FullName
                });
            }

            CheckParentEdge(issues, p, by, p.FatherPersonId, "父", byId);
            CheckParentEdge(issues, p, by, p.MotherPersonId, "母", byId);
        }

        AddCycleIssues(issues, people, byId);

        return issues
            .OrderBy(x => x.Severity == "ERROR" ? 0 : 1)
            .ThenBy(x => x.IssueType)
            .ThenBy(x => x.PersonName)
            .ToList();
    }

    /// <summary>
    /// 多节点环检测。原先只检 SELF_PARENT（自环），而按姓名批量改父边的路径
    /// （HealParentEdgesAsync / AttachAncestorsIfNoneAsync）都能写出 A→B→C→A 这种环，
    /// 结果是环写得进去、体检工具看不见。这里做一次迭代加深的祖先追溯。
    /// </summary>
    private static void AddCycleIssues(
        List<FtTreeHealthIssueVm> issues,
        IReadOnlyList<PersonSnap> people,
        IReadOnlyDictionary<int, PersonSnap> byId)
    {
        // 0=未访问 1=在当前追溯路径上 2=已确认无环
        var state = new Dictionary<int, byte>(people.Count);
        var reported = new HashSet<int>();

        foreach (var start in people)
        {
            if (state.GetValueOrDefault(start.DataId) != 0) continue;

            var path = new List<int>();
            var onPath = new HashSet<int>();
            var cur = start.DataId;

            while (true)
            {
                if (state.GetValueOrDefault(cur) == 2) break;      // 走到已判定的安全区
                if (!onPath.Add(cur))                              // 回到路径上的某点 = 成环
                {
                    var from = path.IndexOf(cur);
                    var ring = path.Skip(from).ToList();
                    var anchor = ring.Min();
                    if (reported.Add(anchor))
                    {
                        var names = ring.Select(id => byId.TryGetValue(id, out var q) ? q.FullName : "#" + id);
                        issues.Add(new FtTreeHealthIssueVm
                        {
                            IssueType = "PARENT_CYCLE",
                            Severity = "ERROR",
                            Title = "父母关系成环",
                            Detail = "环路：" + string.Join(" → ", names) + " → …",
                            PersonId = anchor,
                            PersonName = byId.TryGetValue(anchor, out var a) ? a.FullName : "#" + anchor
                        });
                    }
                    break;
                }

                path.Add(cur);
                if (!byId.TryGetValue(cur, out var node)) break;
                var next = node.FatherPersonId ?? node.MotherPersonId;
                if (next == null || next == cur) break;
                cur = next.Value;
            }

            foreach (var id in path)
                state[id] = 2;
        }
    }

    private static void CheckParentEdge(
        List<FtTreeHealthIssueVm> issues,
        PersonSnap child,
        int? childYear,
        int? parentId,
        string role,
        IReadOnlyDictionary<int, PersonSnap> byId)
    {
        if (!parentId.HasValue) return;

        if (!byId.TryGetValue(parentId.Value, out var parent))
        {
            issues.Add(new FtTreeHealthIssueVm
            {
                IssueType = "BROKEN_EDGE",
                Severity = "ERROR",
                Title = $"{role}边断链",
                Detail = $"{role} PersonId={parentId} 不存在或已删除",
                PersonId = child.DataId,
                PersonName = child.FullName,
                RelatedPersonId = parentId
            });
            return;
        }

        var py = parent.BirthYear ?? FtText.ParseBirthYear(parent.BirthDate);
        if (!childYear.HasValue || !py.HasValue) return;

        var gap = childYear.Value - py.Value;
        if (gap < 0)
        {
            issues.Add(new FtTreeHealthIssueVm
            {
                IssueType = "CHILD_OLDER",
                Severity = "ERROR",
                Title = $"子女早于{role}亲出生",
                Detail = $"子 {childYear}，{role} {py}（差 {gap} 年）",
                PersonId = child.DataId,
                PersonName = child.FullName,
                RelatedPersonId = parent.DataId,
                RelatedPersonName = parent.FullName
            });
        }
        else if (gap < 12)
        {
            issues.Add(new FtTreeHealthIssueVm
            {
                IssueType = "PARENT_TOO_YOUNG",
                Severity = "WARN",
                Title = $"{role}亲育龄过小",
                Detail = $"子 {childYear}，{role} {py}（仅差 {gap} 年）",
                PersonId = child.DataId,
                PersonName = child.FullName,
                RelatedPersonId = parent.DataId,
                RelatedPersonName = parent.FullName
            });
        }
        else if (gap > 70)
        {
            issues.Add(new FtTreeHealthIssueVm
            {
                IssueType = "PARENT_TOO_OLD",
                Severity = "WARN",
                Title = $"{role}亲育龄过大",
                Detail = $"子 {childYear}，{role} {py}（差 {gap} 年）",
                PersonId = child.DataId,
                PersonName = child.FullName,
                RelatedPersonId = parent.DataId,
                RelatedPersonName = parent.FullName
            });
        }
    }
}
