using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class DashPosIndicatorPermService
{
    private readonly FrameworkDbContext _db;
    private readonly DashIndicatorService _indicatorService;

    public DashPosIndicatorPermService(FrameworkDbContext db, DashIndicatorService indicatorService)
    {
        _db = db;
        _indicatorService = indicatorService;
    }

    public async Task<DashPosPermIndexVm> GetIndexAsync(int posId, CancellationToken ct)
    {
        var posOptions = await _db.EPositions.AsNoTracking()
            .Where(x => !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.DispSeq).ThenBy(x => x.PostCode)
            .Select(x => new ValueTuple<int, string>(x.DataId, x.PostCName ?? x.PostCode ?? ""))
            .ToListAsync(ct);

        if (posId <= 0 && posOptions.Count > 0)
            posId = posOptions[0].Item1;

        var indicators = await _db.DashIndicators.AsNoTracking()
            .Where(x => !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.DispSeq).ThenBy(x => x.IndicatorCode)
            .ToListAsync(ct);

        var grantedIds = posId > 0
            ? (await _db.DashPosIndicatorPerms.AsNoTracking()
                .Where(x => x.PosId == posId).WhereActiveBStatus(x => x.BStatus)
                .Select(x => x.IndicatorId)
                .ToListAsync(ct)).ToHashSet()
            : [];

        return new DashPosPermIndexVm
        {
            PosId = posId,
            PosOptions = posOptions,
            GrantedIndicatorIds = grantedIds,
            Indicators = indicators.Select(x => new DashIndicatorListRowVm
            {
                DataId = x.DataId,
                IndicatorCode = x.IndicatorCode ?? "",
                IndicatorName = x.IndicatorName ?? "",
                AppCode = x.AppCode ?? "",
                ChartType = x.ChartType,
                ChartTypeName = DashChartTypes.GetName(x.ChartType),
                DataSource = x.DataSource ?? "",
                BStatus = _indicatorService.ToStatusDisplay(x.BStatus),
                DispSeq = x.DispSeq,
                AmendDate = x.AmendDate
            }).ToList()
        };
    }

    public async Task SaveAsync(DashPosPermSaveVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var existing = await _db.DashPosIndicatorPerms
            .Where(x => x.PosId == model.PosId)
            .ToListAsync(ct);

        var targetIds = model.IndicatorIds?.Distinct().ToHashSet() ?? [];

        foreach (var row in existing)
        {
            if (targetIds.Contains(row.IndicatorId))
            {
                if (!EBStatusHelper.IsActiveBStatus(row.BStatus))
                {
                    row.BStatus = "1";
                    row.AmendDate = now;
                    row.OperatorName = operatorId;
                }
            }
            else if (EBStatusHelper.IsActiveBStatus(row.BStatus))
            {
                row.BStatus = "2";
                row.AmendDate = now;
                row.OperatorName = operatorId;
            }
        }

        var existingActiveIds = existing.Where(x => EBStatusHelper.IsActiveBStatus(x.BStatus)).Select(x => x.IndicatorId).ToHashSet();
        foreach (var indicatorId in targetIds)
        {
            if (existingActiveIds.Contains(indicatorId)) continue;
            var inactive = existing.FirstOrDefault(x => x.IndicatorId == indicatorId);
            if (inactive != null)
            {
                inactive.BStatus = "1";
                inactive.AmendDate = now;
                inactive.OperatorName = operatorId;
            }
            else
            {
                _db.DashPosIndicatorPerms.Add(new DashPosIndicatorPerm
                {
                    PosId = model.PosId,
                    IndicatorId = indicatorId,
                    BStatus = "1",
                    CreateDate = now,
                    AmendDate = now,
                    OperatorName = operatorId
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> IsIndicatorAllowedForUserAsync(int userId, int indicatorId, CancellationToken ct)
    {
        var posIds = await _db.EUserPositions.AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
            .Select(x => x.PosId)
            .Distinct()
            .ToListAsync(ct);
        if (posIds.Count == 0) return false;

        return await _db.DashPosIndicatorPerms.AsNoTracking()
            .Where(x => posIds.Contains(x.PosId) && x.IndicatorId == indicatorId)
            .WhereActiveBStatus(x => x.BStatus)
            .AnyAsync(ct);
    }

    public async Task<HashSet<int>> GetAllowedIndicatorIdsForUserAsync(int userId, CancellationToken ct)
    {
        var posIds = await _db.EUserPositions.AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
            .Select(x => x.PosId)
            .Distinct()
            .ToListAsync(ct);
        if (posIds.Count == 0) return [];

        var ids = await _db.DashPosIndicatorPerms.AsNoTracking()
            .Where(x => posIds.Contains(x.PosId)).WhereActiveBStatus(x => x.BStatus)
            .Select(x => x.IndicatorId)
            .Distinct()
            .ToListAsync(ct);
        return ids.ToHashSet();
    }
}
