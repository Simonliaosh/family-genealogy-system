using System.Text.Json;
using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class DashBoardService
{
    private readonly FrameworkDbContext _db;
    private readonly DashPosIndicatorPermService _permService;
    private readonly DashPosTemplateService _templateService;
    private readonly DashCalcRuleExecutor _calcExecutor;

    public DashBoardService(
        FrameworkDbContext db,
        DashPosIndicatorPermService permService,
        DashPosTemplateService templateService,
        DashCalcRuleExecutor calcExecutor)
    {
        _db = db;
        _permService = permService;
        _templateService = templateService;
        _calcExecutor = calcExecutor;
    }

    public async Task<DashApiResult<DashLayoutVm>> GetLayoutAsync(int userId, CancellationToken ct)
    {
        var setting = await EnsureUserSettingAsync(userId, ct);
        if (setting == null)
            return new DashApiResult<DashLayoutVm> { Code = 403, Msg = "无有效任岗" };

        var posList = await GetActiveUserPositionsAsync(userId, ct);
        if (posList.Count == 0)
            return new DashApiResult<DashLayoutVm> { Code = 403, Msg = "无有效任岗" };

        var current = posList.FirstOrDefault(x => x.UserPosId == setting.CurrentUserPosId) ?? posList[0];
        if (current.UserPosId != setting.CurrentUserPosId)
        {
            setting.CurrentUserPosId = current.UserPosId;
            setting.AmendDate = DateTime.Now;
            await _db.SaveChangesAsync(ct);
        }

        await _templateService.EnsureDefaultTemplateAsync(current.PosId, ct);

        var tplCount = await _db.DashPosTemplates.AsNoTracking()
            .CountAsync(x => x.PosId == current.PosId && !x.IsDeleted, ct);
        var userCardCount = await _db.DashUserCards
            .CountAsync(x => x.UserId == userId && x.UserPosId == current.UserPosId && !x.IsDeleted, ct);

        if (tplCount > 0 && userCardCount < tplCount)
        {
            var existing = await _db.DashUserCards
                .Where(x => x.UserId == userId && x.UserPosId == current.UserPosId && !x.IsDeleted)
                .ToListAsync(ct);
            var now = DateTime.Now;
            foreach (var card in existing)
            {
                card.IsDeleted = true;
                card.BStatus = "2";
                card.AmendDate = now;
                card.OperatorName = "system";
            }
            await _db.SaveChangesAsync(ct);
            await InitUserCardsFromTemplateAsync(userId, current.UserPosId, current.PosId, current.DeptId, "system", ct);
        }
        else if (userCardCount == 0)
        {
            await InitUserCardsFromTemplateAsync(userId, current.UserPosId, current.PosId, current.DeptId, "system", ct);
        }

        var cards = await BuildCardLayoutAsync(userId, current.UserPosId, ct);
        var globalFilter = ParseGlobalFilter(setting.GlobalFilterJson);

        return new DashApiResult<DashLayoutVm>
        {
            Code = 200,
            Msg = "成功",
            Data = new DashLayoutVm
            {
                PosList = posList.Select(x => new DashUserPosItemVm
                {
                    UserPosId = x.UserPosId,
                    PosId = x.PosId,
                    PosName = x.PosName,
                    DeptId = x.DeptId,
                    DeptName = x.DeptName,
                    IsPrimary = x.IsPrimary,
                    IsCurrent = x.UserPosId == current.UserPosId
                }).ToList(),
                CurrentUserPosId = current.UserPosId,
                GlobalFilter = globalFilter,
                Cards = cards
            }
        };
    }

    public async Task<DashApiResult<object>> GetIndicatorDataAsync(int userId, DashGetIndicatorDataRequest req, CancellationToken ct)
    {
        if (!await _permService.IsIndicatorAllowedForUserAsync(userId, req.IndicatorId, ct))
            return new DashApiResult<object> { Code = 403, Msg = "无指标权限" };

        var indicator = await _db.DashIndicators.AsNoTracking()
            .Where(x => x.DataId == req.IndicatorId && !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
            .FirstOrDefaultAsync(ct);
        if (indicator == null)
            return new DashApiResult<object> { Code = 404, Msg = "指标不存在" };

        var timeType = req.GlobalFilter?.TimeType ?? 3;
        var deptId = req.GlobalFilter?.DeptId ?? 0;
        if (deptId <= 0)
        {
            var setting = await _db.DashUserSettings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId, ct);
            if (setting != null)
                deptId = ParseGlobalFilter(setting.GlobalFilterJson).DeptId;

            if (deptId <= 0)
            {
                var up = await _db.EUserPositions.AsNoTracking()
                    .Where(x => x.UserId == userId && !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
                    .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.DataId)
                    .FirstOrDefaultAsync(ct);
                deptId = up?.DeptId ?? 0;
            }
        }

        var data = await _calcExecutor.ExecuteAsync(indicator, userId, deptId, timeType, ct);
        if (data == null)
            return new DashApiResult<object> { Code = 204, Msg = "暂无数据" };

        return new DashApiResult<object> { Code = 200, Msg = "成功", Data = data };
    }

    public async Task<DashApiResult<IReadOnlyList<DashIndicatorListRowVm>>> GetAvailableIndicatorsAsync(int userId, CancellationToken ct)
    {
        var allowedIds = await _permService.GetAllowedIndicatorIdsForUserAsync(userId, ct);
        if (allowedIds.Count == 0)
            return new DashApiResult<IReadOnlyList<DashIndicatorListRowVm>> { Code = 200, Msg = "成功", Data = [] };

        var rows = await _db.DashIndicators.AsNoTracking()
            .Where(x => allowedIds.Contains(x.DataId) && !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.DispSeq).ThenBy(x => x.IndicatorCode)
            .ToListAsync(ct);

        var data = rows.Select(x => new DashIndicatorListRowVm
        {
            DataId = x.DataId,
            IndicatorCode = x.IndicatorCode ?? "",
            IndicatorName = x.IndicatorName ?? "",
            AppCode = x.AppCode ?? "",
            ChartType = x.ChartType,
            ChartTypeName = DashChartTypes.GetName(x.ChartType),
            DataSource = x.DataSource ?? "",
            BStatus = x.BStatus ?? "",
            DispSeq = x.DispSeq,
            AmendDate = x.AmendDate
        }).ToList();

        return new DashApiResult<IReadOnlyList<DashIndicatorListRowVm>> { Code = 200, Msg = "成功", Data = data };
    }

    public async Task<DashApiResult<DashLayoutVm>> SwitchUserPosAsync(int userId, int userPosId, string op, CancellationToken ct)
    {
        var up = await _db.EUserPositions.AsNoTracking()
            .Where(x => x.DataId == userPosId && x.UserId == userId && !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
            .FirstOrDefaultAsync(ct);
        if (up == null)
            return new DashApiResult<DashLayoutVm> { Code = 403, Msg = "任岗无效" };

        var setting = await EnsureUserSettingAsync(userId, ct);
        if (setting == null)
            return new DashApiResult<DashLayoutVm> { Code = 403, Msg = "无有效任岗" };

        setting.CurrentUserPosId = userPosId;
        setting.AmendDate = DateTime.Now;
        setting.OperatorName = op;
        await _db.SaveChangesAsync(ct);

        var hasCards = await _db.DashUserCards.AnyAsync(
            x => x.UserId == userId && x.UserPosId == userPosId && !x.IsDeleted, ct);
        if (!hasCards)
            await InitUserCardsFromTemplateAsync(userId, userPosId, up.PosId, up.DeptId, op, ct);

        await WriteOperLogAsync(userId, up.PosId, DashOperTypes.SWITCH_POS, $"切换任岗 UserPosID={userPosId}", op, ct);
        return await GetLayoutAsync(userId, ct);
    }

    public async Task<DashApiResult<object>> SaveGlobalFilterAsync(int userId, DashGlobalFilterVm filter, string op, CancellationToken ct)
    {
        var setting = await EnsureUserSettingAsync(userId, ct);
        if (setting == null)
            return new DashApiResult<object> { Code = 403, Msg = "无有效任岗" };

        setting.GlobalFilterJson = JsonSerializer.Serialize(filter);
        setting.AmendDate = DateTime.Now;
        setting.OperatorName = op;
        await _db.SaveChangesAsync(ct);
        await WriteOperLogAsync(userId, null, DashOperTypes.FILTER, "保存全局筛选", op, ct);
        return new DashApiResult<object> { Code = 200, Msg = "成功" };
    }

    public async Task<DashApiResult<object>> SaveLayoutAsync(int userId, DashSaveLayoutRequest req, string op, CancellationToken ct)
    {
        var setting = await EnsureUserSettingAsync(userId, ct);
        if (setting == null)
            return new DashApiResult<object> { Code = 403, Msg = "无有效任岗" };

        var cards = await _db.DashUserCards
            .Where(x => x.UserId == userId && x.UserPosId == setting.CurrentUserPosId && !x.IsDeleted)
            .ToListAsync(ct);

        foreach (var item in req.Cards)
        {
            var card = cards.FirstOrDefault(x => x.DataId == item.CardId);
            if (card == null) continue;
            if (card.IsLock) continue;
            card.LayoutRow = item.LayoutRow;
            card.LayoutCol = item.LayoutCol;
            card.ColSpan = item.ColSpan;
            card.AmendDate = DateTime.Now;
            card.OperatorName = op;
        }

        await _db.SaveChangesAsync(ct);
        await WriteOperLogAsync(userId, null, DashOperTypes.LAYOUT, "保存布局", op, ct);
        return new DashApiResult<object> { Code = 200, Msg = "成功" };
    }

    public async Task<DashApiResult<object>> AddCardAsync(int userId, DashAddCardRequest req, string op, CancellationToken ct)
    {
        if (!await _permService.IsIndicatorAllowedForUserAsync(userId, req.IndicatorId, ct))
            return new DashApiResult<object> { Code = 403, Msg = "无指标权限" };

        var setting = await EnsureUserSettingAsync(userId, ct);
        if (setting == null)
            return new DashApiResult<object> { Code = 403, Msg = "无有效任岗" };

        var up = await _db.EUserPositions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == setting.CurrentUserPosId && x.UserId == userId, ct);
        if (up == null)
            return new DashApiResult<object> { Code = 403, Msg = "任岗无效" };

        var dup = await _db.DashUserCards.AnyAsync(
            x => x.UserId == userId && x.UserPosId == setting.CurrentUserPosId && x.IndicatorId == req.IndicatorId && !x.IsDeleted, ct);
        if (dup)
            return new DashApiResult<object> { Code = 409, Msg = "该指标卡片已存在" };

        var now = DateTime.Now;
        _db.DashUserCards.Add(new DashUserCard
        {
            UserId = userId,
            UserPosId = setting.CurrentUserPosId,
            PosId = up.PosId,
            DeptId = up.DeptId,
            IndicatorId = req.IndicatorId,
            LayoutRow = req.LayoutRow,
            LayoutCol = req.LayoutCol,
            ColSpan = req.ColSpan,
            CardTitle = req.CardTitle,
            BStatus = "1",
            CreateDate = now,
            AmendDate = now,
            OperatorName = op
        });
        await _db.SaveChangesAsync(ct);
        await WriteOperLogAsync(userId, up.PosId, DashOperTypes.ADD_CARD, $"新增卡片 IndicatorID={req.IndicatorId}", op, ct);
        return new DashApiResult<object> { Code = 200, Msg = "成功" };
    }

    public async Task<DashApiResult<object>> SetCardHideAsync(int userId, int cardId, bool isHide, string op, CancellationToken ct)
    {
        var card = await FindUserCardAsync(userId, cardId, ct);
        if (card == null)
            return new DashApiResult<object> { Code = 404, Msg = "卡片不存在" };
        if (card.IsLock)
            return new DashApiResult<object> { Code = 403, Msg = "卡片已锁定" };

        card.IsHide = isHide;
        card.AmendDate = DateTime.Now;
        card.OperatorName = op;
        await _db.SaveChangesAsync(ct);
        await WriteOperLogAsync(userId, card.PosId, DashOperTypes.HIDE, $"设置隐藏 CardID={cardId} IsHide={isHide}", op, ct);
        return new DashApiResult<object> { Code = 200, Msg = "成功" };
    }

    public async Task<DashApiResult<object>> UpdateCardTitleAsync(int userId, int cardId, string title, string op, CancellationToken ct)
    {
        var card = await FindUserCardAsync(userId, cardId, ct);
        if (card == null)
            return new DashApiResult<object> { Code = 404, Msg = "卡片不存在" };
        if (card.IsLock)
            return new DashApiResult<object> { Code = 403, Msg = "卡片已锁定" };

        card.CardTitle = title.Length <= 100 ? title : title[..100];
        card.AmendDate = DateTime.Now;
        card.OperatorName = op;
        await _db.SaveChangesAsync(ct);
        return new DashApiResult<object> { Code = 200, Msg = "成功" };
    }

    public async Task<DashApiResult<object>> DeleteCardAsync(int userId, int cardId, string op, CancellationToken ct)
    {
        var card = await FindUserCardAsync(userId, cardId, ct);
        if (card == null)
            return new DashApiResult<object> { Code = 404, Msg = "卡片不存在" };
        if (card.IsLock)
            return new DashApiResult<object> { Code = 403, Msg = "卡片已锁定" };

        card.IsDeleted = true;
        card.BStatus = "2";
        card.AmendDate = DateTime.Now;
        card.OperatorName = op;
        await _db.SaveChangesAsync(ct);
        return new DashApiResult<object> { Code = 200, Msg = "成功" };
    }

    public async Task<DashApiResult<DashLayoutVm>> ResetToPosDefaultAsync(int userId, string op, CancellationToken ct)
    {
        var setting = await EnsureUserSettingAsync(userId, ct);
        if (setting == null)
            return new DashApiResult<DashLayoutVm> { Code = 403, Msg = "无有效任岗" };

        var up = await _db.EUserPositions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == setting.CurrentUserPosId && x.UserId == userId, ct);
        if (up == null)
            return new DashApiResult<DashLayoutVm> { Code = 403, Msg = "任岗无效" };

        var existing = await _db.DashUserCards
            .Where(x => x.UserId == userId && x.UserPosId == setting.CurrentUserPosId && !x.IsDeleted)
            .ToListAsync(ct);
        foreach (var card in existing)
        {
            card.IsDeleted = true;
            card.BStatus = "2";
            card.AmendDate = DateTime.Now;
            card.OperatorName = op;
        }
        await _db.SaveChangesAsync(ct);

        await InitUserCardsFromTemplateAsync(userId, setting.CurrentUserPosId, up.PosId, up.DeptId, op, ct);
        await WriteOperLogAsync(userId, up.PosId, DashOperTypes.RESET, "重置为岗位默认", op, ct);
        return await GetLayoutAsync(userId, ct);
    }

    public async Task InitUserCardsFromTemplateAsync(int userId, int userPosId, int posId, int deptId, string op, CancellationToken ct)
    {
        await _templateService.CopyToUserCardsAsync(userId, userPosId, posId, deptId, op, ct);
        await WriteOperLogAsync(userId, posId, DashOperTypes.INIT, $"初始化 UserPosID={userPosId}", op, ct);
    }

    public async Task WriteOperLogAsync(int userId, int? posId, string operType, string content, string op, CancellationToken ct)
    {
        _db.DashUserOperLogs.Add(new DashUserOperLog
        {
            UserId = userId,
            PosId = posId,
            OperType = operType,
            OperContent = content.Length <= 500 ? content : content[..500],
            OperTime = DateTime.Now,
            OperatorName = op
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task<DashUserSetting?> EnsureUserSettingAsync(int userId, CancellationToken ct)
    {
        var posList = await GetActiveUserPositionsAsync(userId, ct);
        if (posList.Count == 0) return null;

        var setting = await _db.DashUserSettings.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (setting == null)
        {
            var defaultPos = posList.FirstOrDefault(x => x.IsPrimary) ?? posList[0];
            setting = new DashUserSetting
            {
                UserId = userId,
                CurrentUserPosId = defaultPos.UserPosId,
                GlobalFilterJson = JsonSerializer.Serialize(new DashGlobalFilterVm { TimeType = 3, DeptId = defaultPos.DeptId }),
                CreateDate = DateTime.Now,
                AmendDate = DateTime.Now
            };
            _db.DashUserSettings.Add(setting);
            await _db.SaveChangesAsync(ct);

            var hasCards = await _db.DashUserCards.AnyAsync(
                x => x.UserId == userId && x.UserPosId == defaultPos.UserPosId && !x.IsDeleted, ct);
            if (!hasCards)
                await InitUserCardsFromTemplateAsync(userId, defaultPos.UserPosId, defaultPos.PosId, defaultPos.DeptId, "system", ct);
        }
        else if (!posList.Any(x => x.UserPosId == setting.CurrentUserPosId))
        {
            var fallback = posList.FirstOrDefault(x => x.IsPrimary) ?? posList[0];
            setting.CurrentUserPosId = fallback.UserPosId;
            setting.AmendDate = DateTime.Now;
            await _db.SaveChangesAsync(ct);
        }

        return setting;
    }

    private async Task<IReadOnlyList<DashUserPosItemVm>> GetActiveUserPositionsAsync(int userId, CancellationToken ct)
    {
        var ups = await _db.EUserPositions.AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
            .ToListAsync(ct);
        if (ups.Count == 0) return [];

        var posIds = ups.Select(x => x.PosId).Distinct().ToList();
        var deptIds = ups.Select(x => x.DeptId).Distinct().ToList();
        var positions = await _db.EPositions.AsNoTracking()
            .Where(x => posIds.Contains(x.DataId))
            .ToDictionaryAsync(x => x.DataId, ct);
        var departments = await _db.EDepartments.AsNoTracking()
            .Where(x => deptIds.Contains(x.DataId))
            .ToDictionaryAsync(x => x.DataId, ct);

        return ups.Select(x =>
        {
            positions.TryGetValue(x.PosId, out var pos);
            departments.TryGetValue(x.DeptId, out var dept);
            return new DashUserPosItemVm
            {
                UserPosId = x.DataId,
                PosId = x.PosId,
                PosName = pos?.PostCName ?? pos?.PostCode ?? "",
                DeptId = x.DeptId,
                DeptName = dept?.DeptCName ?? dept?.DeptCode ?? "",
                IsPrimary = x.IsPrimary
            };
        }).ToList();
    }

    private async Task<IReadOnlyList<DashCardLayoutVm>> BuildCardLayoutAsync(int userId, int userPosId, CancellationToken ct)
    {
        var allowedIds = await _permService.GetAllowedIndicatorIdsForUserAsync(userId, ct);
        var cards = await _db.DashUserCards.AsNoTracking()
            .Where(x => x.UserId == userId && x.UserPosId == userPosId && !x.IsDeleted && !x.IsHide)
            .OrderBy(x => x.LayoutRow).ThenBy(x => x.LayoutCol)
            .ToListAsync(ct);
        if (cards.Count == 0) return [];

        var indicatorIds = cards.Select(x => x.IndicatorId).Distinct().ToList();
        var indicators = await _db.DashIndicators.AsNoTracking()
            .Where(x => indicatorIds.Contains(x.DataId))
            .ToDictionaryAsync(x => x.DataId, ct);

        return cards
            .Where(x => allowedIds.Contains(x.IndicatorId))
            .Select(x =>
            {
                indicators.TryGetValue(x.IndicatorId, out var ind);
                return new DashCardLayoutVm
                {
                    CardId = x.DataId,
                    IndicatorId = x.IndicatorId,
                    IndicatorCode = ind?.IndicatorCode ?? "",
                    CardTitle = x.CardTitle ?? "",
                    ChartType = ind?.ChartType ?? 1,
                    LayoutRow = x.LayoutRow,
                    LayoutCol = x.LayoutCol,
                    ColSpan = x.ColSpan,
                    IsHide = x.IsHide,
                    IsLock = x.IsLock,
                    UserFilterJson = x.UserFilterJson
                };
            }).ToList();
    }

    private async Task<DashUserCard?> FindUserCardAsync(int userId, int cardId, CancellationToken ct) =>
        await _db.DashUserCards.FirstOrDefaultAsync(x => x.DataId == cardId && x.UserId == userId && !x.IsDeleted, ct);

    private static DashGlobalFilterVm ParseGlobalFilter(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new DashGlobalFilterVm();
        try
        {
            return JsonSerializer.Deserialize<DashGlobalFilterVm>(json) ?? new DashGlobalFilterVm();
        }
        catch
        {
            return new DashGlobalFilterVm();
        }
    }
}
