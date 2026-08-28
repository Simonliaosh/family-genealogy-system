using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class DashPosTemplateService
{
    private readonly FrameworkDbContext _db;
    private readonly DashIndicatorService _indicatorService;

    public DashPosTemplateService(FrameworkDbContext db, DashIndicatorService indicatorService)
    {
        _db = db;
        _indicatorService = indicatorService;
    }

    public async Task<DashPosTemplateIndexVm> GetIndexAsync(int posId, CancellationToken ct)
    {
        var posOptions = await _db.EPositions.AsNoTracking()
            .Where(x => !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.DispSeq).ThenBy(x => x.PostCode)
            .Select(x => new ValueTuple<int, string>(x.DataId, x.PostCName ?? x.PostCode ?? ""))
            .ToListAsync(ct);

        if (posId <= 0 && posOptions.Count > 0)
            posId = posOptions[0].Item1;

        if (posId > 0)
        {
            var hasCards = await _db.DashPosTemplates.AnyAsync(
                x => x.PosId == posId && !x.IsDeleted, ct);
            if (!hasCards)
                await ApplyDefaultTemplateAsync(posId, "system", ct);
        }

        var cards = posId > 0
            ? await LoadTemplateCardsAsync(posId, ct)
            : [];

        var availableIndicators = await GetAvailableIndicatorsForPosAsync(posId, ct);
        EnrichCardsWithIndicatorMeta(cards, availableIndicators);

        return new DashPosTemplateIndexVm
        {
            PosId = posId,
            PosOptions = posOptions,
            Cards = cards,
            AvailableIndicators = availableIndicators
        };
    }

    public async Task<DashApiResult<object>> ResetToDefaultAsync(int posId, string operatorId, CancellationToken ct)
    {
        if (posId <= 0)
            return new DashApiResult<object> { Code = 422, Msg = "请选择岗位" };

        var pos = await _db.EPositions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == posId && !x.IsDeleted, ct);
        if (pos == null)
            return new DashApiResult<object> { Code = 404, Msg = "岗位不存在" };

        var applied = await ApplyDefaultTemplateAsync(posId, operatorId, ct);
        if (applied == 0)
            return new DashApiResult<object> { Code = 422, Msg = "该岗位未授权任何 FRAME 演示指标，请先在「岗位指标授权」中配置。" };

        return new DashApiResult<object> { Code = 200, Msg = $"已恢复默认模板（{applied} 张卡片）" };
    }

    private async Task<List<DashPosTemplateCardVm>> LoadTemplateCardsAsync(int posId, CancellationToken ct) =>
        await _db.DashPosTemplates.AsNoTracking()
            .Where(x => x.PosId == posId && !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.LayoutRow).ThenBy(x => x.LayoutCol).ThenBy(x => x.DispSeq)
            .Select(x => new DashPosTemplateCardVm
            {
                DataId = x.DataId,
                IndicatorId = x.IndicatorId,
                LayoutRow = x.LayoutRow,
                LayoutCol = x.LayoutCol,
                ColSpan = x.ColSpan,
                CardTitle = x.CardTitle ?? "",
                IsLock = x.IsLock,
                DefaultFilterJson = x.DefaultFilterJson
            })
            .ToListAsync(ct);

    private static void EnrichCardsWithIndicatorMeta(
        IReadOnlyList<DashPosTemplateCardVm> cards,
        IReadOnlyList<DashIndicatorListRowVm> indicators)
    {
        var map = indicators.ToDictionary(x => x.DataId);
        foreach (var card in cards)
        {
            if (!map.TryGetValue(card.IndicatorId, out var ind)) continue;
            card.IndicatorCode = ind.IndicatorCode;
            card.ChartType = ind.ChartType;
            card.ChartTypeName = ind.ChartTypeName;
        }
    }

    /// <returns>写入的卡片数</returns>
    private async Task<int> ApplyDefaultTemplateAsync(int posId, string operatorId, CancellationToken ct)
    {
        var postCode = await _db.EPositions.AsNoTracking()
            .Where(x => x.DataId == posId)
            .Select(x => x.PostCode)
            .FirstOrDefaultAsync(ct);

        var preset = DashPosTemplateDefaults.ResolvePreset(postCode);
        var allowedIds = (await _db.DashPosIndicatorPerms.AsNoTracking()
            .Where(x => x.PosId == posId).WhereActiveBStatus(x => x.BStatus)
            .Select(x => x.IndicatorId)
            .ToListAsync(ct)).ToHashSet();

        var codes = preset.Select(x => x.IndicatorCode).ToList();
        var indicators = await _db.DashIndicators.AsNoTracking()
            .Where(x => codes.Contains(x.IndicatorCode!) && !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
            .ToDictionaryAsync(x => x.IndicatorCode!, x => x.DataId, ct);

        var now = DateTime.Now;
        var existing = await _db.DashPosTemplates
            .Where(x => x.PosId == posId && !x.IsDeleted)
            .ToListAsync(ct);
        foreach (var row in existing)
        {
            row.IsDeleted = true;
            row.BStatus = "2";
            row.AmendDate = now;
            row.OperatorName = operatorId;
        }

        var dispSeq = 1;
        var count = 0;
        foreach (var item in preset)
        {
            if (!indicators.TryGetValue(item.IndicatorCode, out var indicatorId)) continue;
            if (!allowedIds.Contains(indicatorId)) continue;

            _db.DashPosTemplates.Add(new DashPosTemplate
            {
                PosId = posId,
                IndicatorId = indicatorId,
                LayoutRow = item.LayoutRow,
                LayoutCol = item.LayoutCol,
                ColSpan = item.ColSpan,
                IsLock = item.IsLock,
                DefaultFilterJson = "{}",
                CardTitle = item.CardTitle,
                DispSeq = dispSeq++,
                BStatus = "1",
                CreateDate = now,
                AmendDate = now,
                OperatorName = operatorId
            });
            count++;
        }

        if (count > 0)
            await _db.SaveChangesAsync(ct);
        else if (existing.Count > 0)
            await _db.SaveChangesAsync(ct);

        return count;
    }

    /// <summary>岗位无模板时写入默认 9 类图表演示布局。</summary>
    public async Task EnsureDefaultTemplateAsync(int posId, CancellationToken ct)
    {
        if (posId <= 0) return;
        var has = await _db.DashPosTemplates.AnyAsync(x => x.PosId == posId && !x.IsDeleted, ct);
        if (!has)
            await ApplyDefaultTemplateAsync(posId, "system", ct);
    }

    public async Task<DashApiResult<object>> SaveBatchAsync(DashPosTemplateSaveBatchVm model, string operatorId, CancellationToken ct)
    {
        if (model.PosId <= 0)
            return new DashApiResult<object> { Code = 422, Msg = "请选择岗位" };

        var allowedIds = (await _db.DashPosIndicatorPerms.AsNoTracking()
            .Where(x => x.PosId == model.PosId).WhereActiveBStatus(x => x.BStatus)
            .Select(x => x.IndicatorId)
            .ToListAsync(ct)).ToHashSet();

        foreach (var card in model.Cards)
        {
            if (!allowedIds.Contains(card.IndicatorId))
                return new DashApiResult<object> { Code = 403, Msg = $"指标 {card.IndicatorId} 未授权给该岗位" };
        }

        var now = DateTime.Now;
        var existing = await _db.DashPosTemplates
            .Where(x => x.PosId == model.PosId && !x.IsDeleted)
            .ToListAsync(ct);

        var submittedIds = model.Cards.Where(x => x.DataId > 0).Select(x => x.DataId).ToHashSet();

        foreach (var row in existing)
        {
            if (!submittedIds.Contains(row.DataId))
            {
                row.IsDeleted = true;
                row.BStatus = "2";
                row.AmendDate = now;
                row.OperatorName = operatorId;
            }
        }

        var dispSeq = 1;
        foreach (var card in model.Cards)
        {
            if (card.DataId > 0)
            {
                var row = existing.FirstOrDefault(x => x.DataId == card.DataId);
                if (row == null) continue;
                row.IndicatorId = card.IndicatorId;
                row.LayoutRow = card.LayoutRow;
                row.LayoutCol = card.LayoutCol;
                row.ColSpan = card.ColSpan;
                row.CardTitle = card.CardTitle;
                row.IsLock = card.IsLock;
                row.DefaultFilterJson = card.DefaultFilterJson;
                row.DispSeq = dispSeq++;
                row.AmendDate = now;
                row.OperatorName = operatorId;
            }
            else
            {
                _db.DashPosTemplates.Add(new DashPosTemplate
                {
                    PosId = model.PosId,
                    IndicatorId = card.IndicatorId,
                    LayoutRow = card.LayoutRow,
                    LayoutCol = card.LayoutCol,
                    ColSpan = card.ColSpan,
                    CardTitle = card.CardTitle,
                    IsLock = card.IsLock,
                    DefaultFilterJson = card.DefaultFilterJson,
                    DispSeq = dispSeq++,
                    BStatus = "1",
                    CreateDate = now,
                    AmendDate = now,
                    OperatorName = operatorId
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return new DashApiResult<object> { Code = 200, Msg = "保存成功" };
    }

    public async Task CopyToUserCardsAsync(int userId, int userPosId, int posId, int deptId, string op, CancellationToken ct)
    {
        var templates = await _db.DashPosTemplates.AsNoTracking()
            .Where(x => x.PosId == posId && !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.LayoutRow).ThenBy(x => x.LayoutCol).ThenBy(x => x.DispSeq)
            .ToListAsync(ct);

        var now = DateTime.Now;
        foreach (var tpl in templates)
        {
            _db.DashUserCards.Add(new DashUserCard
            {
                UserId = userId,
                UserPosId = userPosId,
                PosId = posId,
                DeptId = deptId,
                IndicatorId = tpl.IndicatorId,
                LayoutRow = tpl.LayoutRow,
                LayoutCol = tpl.LayoutCol,
                ColSpan = tpl.ColSpan,
                IsLock = tpl.IsLock,
                UserFilterJson = tpl.DefaultFilterJson,
                CardTitle = tpl.CardTitle ?? "",
                BStatus = "1",
                CreateDate = now,
                AmendDate = now,
                OperatorName = op
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyList<DashIndicatorListRowVm>> GetAvailableIndicatorsForPosAsync(int posId, CancellationToken ct)
    {
        if (posId <= 0) return [];

        var indicatorIds = await _db.DashPosIndicatorPerms.AsNoTracking()
            .Where(x => x.PosId == posId).WhereActiveBStatus(x => x.BStatus)
            .Select(x => x.IndicatorId)
            .ToListAsync(ct);
        if (indicatorIds.Count == 0) return [];

        var rows = await _db.DashIndicators.AsNoTracking()
            .Where(x => indicatorIds.Contains(x.DataId) && !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.DispSeq).ThenBy(x => x.IndicatorCode)
            .ToListAsync(ct);

        return rows.Select(x => new DashIndicatorListRowVm
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
        }).ToList();
    }
}
