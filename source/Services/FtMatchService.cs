using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public sealed class FtMatchService
{
    private readonly FrameworkDbContext _db;
    private readonly EventPublisherService _publisher;

    public FtMatchService(FrameworkDbContext db, EventPublisherService publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<List<FtMatchHintVm>> MatchPersonAsync(int personId, CancellationToken ct)
    {
        var src = await _db.FtPersons.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == personId && !x.IsDeleted, ct);
        if (src == null) return new List<FtMatchHintVm>();
        return await MatchCoreAsync(src, ct);
    }

    public async Task<List<FtMatchHintVm>> MatchCoreAsync(FtPerson src, CancellationToken ct)
    {
        var name = FtText.NormName(src.FullName);
        if (name.Length == 0)
            return new List<FtMatchHintVm>();

        var father = FtText.NormName(src.FatherName);
        var mother = FtText.NormName(src.MotherName);
        var birthSrc = (src.BirthDate ?? "").Trim();
        var yearSrc = src.BirthYear ?? FtText.ParseBirthYear(src.BirthDate);

        var all = await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted && x.DataId != src.DataId && x.SameAsPersonId == null)
            .ToListAsync(ct);
        // 只在同一家族内匹配（无家族则仅对无家族对象）
        if (src.ClanId is int sid && sid > 0)
            all = all.Where(x => x.ClanId == sid).ToList();
        else
            all = all.Where(x => x.ClanId == null || x.ClanId == 0).ToList();

        var excluded = await LoadExcludedPartnerIdsAsync(src.DataId, ct);

        var hints = new List<FtMatchHintVm>();
        foreach (var t in all)
        {
            if (excluded.Contains(t.DataId)) continue;

            var tn = FtText.NormName(t.FullName);
            if (tn.Length == 0) continue;
            if (!string.Equals(name, tn, StringComparison.OrdinalIgnoreCase)) continue;

            var tf = FtText.NormName(t.FatherName);
            var tm = FtText.NormName(t.MotherName);
            var birthT = (t.BirthDate ?? "").Trim();
            var yearT = t.BirthYear ?? FtText.ParseBirthYear(t.BirthDate);

            // 父名双方都有且不同 → 不是同一人
            if (father.Length > 0 && tf.Length > 0 &&
                !string.Equals(father, tf, StringComparison.OrdinalIgnoreCase))
                continue;
            // 母名双方都有且不同 → 不是同一人
            if (mother.Length > 0 && tm.Length > 0 &&
                !string.Equals(mother, tm, StringComparison.OrdinalIgnoreCase))
                continue;

            var level = 0;
            var reason = "";

            var fatherOk = father.Length > 0 && tf.Length > 0 &&
                           string.Equals(father, tf, StringComparison.OrdinalIgnoreCase);
            var motherOk = mother.Length > 0 && tm.Length > 0 &&
                           string.Equals(mother, tm, StringComparison.OrdinalIgnoreCase);
            var yearOk = yearSrc.HasValue && yearT.HasValue && yearSrc.Value == yearT.Value;
            var birthFullOk = birthSrc.Length > 0 && birthT.Length > 0 &&
                              string.Equals(birthSrc, birthT, StringComparison.OrdinalIgnoreCase);

            if (fatherOk && motherOk)
            {
                if (birthFullOk)
                {
                    level = 1;
                    reason = "姓名+父母+出生全文";
                }
                else if (yearOk || !yearSrc.HasValue || !yearT.HasValue)
                {
                    if (yearSrc.HasValue && yearT.HasValue && Math.Abs(yearSrc.Value - yearT.Value) > 15)
                    {
                        await OpenConflictAsync(src.DataId, t.DataId, "BIRTH",
                            $"三姓名相同但生日矛盾：{birthSrc} vs {birthT}", "system", 0, ct);
                        continue;
                    }
                    level = 2;
                    reason = yearOk ? "姓名+父母+出生年" : "姓名+父母（出生一方空）";
                }
                else
                {
                    level = 2;
                    reason = "姓名+父母";
                }
            }
            else if (fatherOk && (mother.Length == 0 || tm.Length == 0))
            {
                level = 3;
                reason = "姓名+父名（母名一方空）";
            }
            else if (yearOk)
            {
                // 姓名+出生年一致：两链根父母文字一致时也常走这里（父名已在上方校验不冲突）
                if (fatherOk || motherOk)
                {
                    level = 2;
                    reason = fatherOk ? "姓名+父名+出生年" : "姓名+母名+出生年";
                }
                else if (father.Length == 0 && tf.Length == 0)
                {
                    level = 4;
                    reason = "姓名+出生年（父名皆空，请人工核对）";
                }
                else
                {
                    // 一方有父名、另一方空，但出生年相同
                    level = 4;
                    reason = "姓名+出生年（父名不全，请人工核对）";
                }
            }
            else
                continue;

            if (level == 0) continue;
            hints.Add(new FtMatchHintVm
            {
                PersonId = t.DataId,
                FullName = t.FullName,
                FatherName = t.FatherName,
                MotherName = t.MotherName,
                BirthDate = t.BirthDate,
                BirthYear = yearT,
                InMain = t.InMainGenealogy,
                Level = level,
                Reason = reason
            });
        }

        return hints.OrderBy(x => x.Level).ThenByDescending(x => x.InMain).ToList();
    }

    /// <summary>扫描指定人物列表相对主谱的可链入命中（目标须已在主谱）。</summary>
    public async Task<List<FtMatchScanRowVm>> ScanForMainLinksAsync(IReadOnlyList<FtPerson> sources, CancellationToken ct)
    {
        var rows = new List<FtMatchScanRowVm>();
        foreach (var src in sources)
        {
            if (src.SameAsPersonId.HasValue) continue;
            var hints = (await MatchCoreAsync(src, ct)).Where(h => h.InMain).ToList();
            if (hints.Count == 0) continue;
            rows.Add(new FtMatchScanRowVm
            {
                SourceId = src.DataId,
                SourceName = src.FullName,
                SourceBirth = src.BirthDate,
                SourceFather = src.FatherName,
                SourceMother = src.MotherName,
                Hints = hints
            });
        }
        return rows;
    }

    public async Task<(bool Conflict, string? Detail)> CheckAncestorConflictAsync(int sourceId, int targetId, CancellationToken ct)
    {
        var src = await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == sourceId && !x.IsDeleted, ct);
        var tgt = await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == targetId && !x.IsDeleted, ct);
        if (src == null || tgt == null) return (true, "人物不存在");

        var walk = async (FtPerson start) =>
        {
            var names = new List<(string Father, string Mother)>();
            var cur = start;
            var guard = 0;
            while (cur.FatherPersonId.HasValue && guard++ < 80)
            {
                var p = await _db.FtPersons.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.DataId == cur.FatherPersonId.Value && !x.IsDeleted, ct);
                if (p == null) break;
                names.Add((FtText.NormName(p.FullName), FtText.NormName(p.MotherName)));
                cur = p;
            }
            return names;
        };

        var a = await walk(src);
        var b = await walk(tgt);
        var n = Math.Min(a.Count, b.Count);
        for (var i = 0; i < n; i++)
        {
            if (a[i].Father.Length == 0 || b[i].Father.Length == 0) continue;
            if (!string.Equals(a[i].Father, b[i].Father, StringComparison.OrdinalIgnoreCase))
                return (true, $"向上第 {i + 1} 代父名不一致：{a[i].Father} / {b[i].Father}");
            if (a[i].Mother.Length > 0 && b[i].Mother.Length > 0 &&
                !string.Equals(a[i].Mother, b[i].Mother, StringComparison.OrdinalIgnoreCase))
                return (true, $"向上第 {i + 1} 代母名不一致：{a[i].Mother} / {b[i].Mother}");
        }
        return (false, null);
    }

    /// <summary>标记两人不是同一人；之后匹配/扫描/Suggest 均跳过该对。</summary>
    public async Task MarkNotMatchAsync(int personA, int personB, int userId, string op, string? remark, CancellationToken ct)
    {
        if (personA <= 0 || personB <= 0 || personA == personB)
            throw new InvalidOperationException("人物无效。");
        var lo = Math.Min(personA, personB);
        var hi = Math.Max(personA, personB);
        var a = await _db.FtPersons.AsNoTracking().AnyAsync(x => x.DataId == lo && !x.IsDeleted, ct);
        var b = await _db.FtPersons.AsNoTracking().AnyAsync(x => x.DataId == hi && !x.IsDeleted, ct);
        if (!a || !b) throw new InvalidOperationException("人物不存在。");

        var now = DateTime.Now;
        var existing = await _db.FtMatchExcludes
            .FirstOrDefaultAsync(x => x.PersonLoId == lo && x.PersonHiId == hi, ct);
        if (existing != null)
        {
            if (!existing.IsDeleted) return;
            existing.IsDeleted = false;
            existing.BStatus = "1";
            existing.MarkUserId = userId;
            existing.Remark = FtText.Clip(remark, 256);
            existing.AmendDate = now;
            existing.OperatorName = FtText.ClipReq(op, 30);
        }
        else
        {
            _db.FtMatchExcludes.Add(new FtMatchExclude
            {
                PersonLoId = lo,
                PersonHiId = hi,
                MarkUserId = userId,
                Remark = FtText.Clip(remark, 256),
                BStatus = "1",
                CreateDate = now,
                AmendDate = now,
                OperatorName = FtText.ClipReq(op, 30)
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<HashSet<int>> LoadExcludedPartnerIdsAsync(int personId, CancellationToken ct)
    {
        var rows = await _db.FtMatchExcludes.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.PersonLoId == personId || x.PersonHiId == personId))
            .Select(x => new { x.PersonLoId, x.PersonHiId })
            .ToListAsync(ct);
        var set = new HashSet<int>();
        foreach (var r in rows)
            set.Add(r.PersonLoId == personId ? r.PersonHiId : r.PersonLoId);
        return set;
    }

    public async Task OpenConflictAsync(int? sourceId, int? targetId, string type, string detail,
        string op, int userId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var row = new FtMatchConflict
        {
            SourcePersonId = sourceId,
            TargetPersonId = targetId,
            ConflictType = type.Length > 32 ? type[..32] : type,
            ConflictDetail = detail,
            ResolveStatus = "OPEN",
            BStatus = "1",
            CreateDate = now,
            AmendDate = now,
            OperatorName = FtText.ClipReq(op, 30)
        };
        _db.FtMatchConflicts.Add(row);
        await _db.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new EventPublishRequest
        {
            AppCode = "FamilyTree",
            EventCode = "FT.CONFLICT.OPEN",
            ObjectType = "MatchConflict",
            ObjectKey = row.DataId.ToString(),
            ObjectTitle = type,
            ObjectUrl = $"/FtConflict/Index",
            TriggerUserId = userId == 0 ? null : userId,
            IdempotencyKey = $"FT-CONFLICT-{row.DataId}",
            OccurredTime = now
        }, ct);
    }
}
