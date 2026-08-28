using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>
/// 事件流转规则（v1 表结构）。
/// 1) CurrentEvent 必填；NextEvent 可空；二者均非空时禁止自环。
/// 2) 条件仅 ConditionExpr（最长 1000）。
/// 3) RuleCode 全表唯一；业务键：AppCode + CurrentEvent + NextEvent + 目标解析字段 + ConditionExpr + ActionType。
/// 4) 事件须在 Tbl_E_EventConfig 中存在且启用（按 AppCode 匹配）。
/// 5) 列表排序：DispSeq 升序，DataID 升序。
/// </summary>
public class EEventFlowRuleService
{
    private readonly FrameworkDbContext _db;
    private readonly DictService _dict;

    public EEventFlowRuleService(FrameworkDbContext db, DictService dict)
    {
        _db = db;
        _dict = dict;
    }

    public string Normalize(string? value, int maxLen, string fallback = "")
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) v = fallback;
        return v.Length <= maxLen ? v : v[..maxLen];
    }

    public static IReadOnlyDictionary<string, string> ActionTypeMap { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["CREATE_TODO"] = "生成待办",
        ["SEND_NOTICE"] = "发送通知",
        ["TRIGGER_EVENT"] = "触发下一事件",
        ["CALL_API"] = "调用接口"
    };

    public static IReadOnlyDictionary<string, string> TargetResolveTypeMap { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["DUTY"] = "按职责",
        ["POSITION"] = "按岗位",
        ["DEPT_MANAGER"] = "部门负责人",
        ["OWNER_MANAGER"] = "发起人上级",
        ["FIXED_USER"] = "固定用户"
    };

    public static IReadOnlyDictionary<string, string> HandleModeMap { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["SINGLE"] = "单人",
        ["ALL"] = "会签(全部)",
        ["ANY"] = "任一",
        ["CLAIM"] = "抢单"
    };

    public static List<(string Value, string Text)> BuildBStatusFilterOptions() =>
    [
        ("", "全部"),
        ("1", "启用"),
        ("2", "停用")
    ];

    public static List<(string Value, string Text)> BuildBStatusFormOptions() =>
    [
        ("1", "启用"),
        ("2", "停用")
    ];

    private static List<(string Value, string Text)> FallbackActionTypeFormOptions() =>
        ActionTypeMap.Select(x => (x.Key, x.Value)).ToList();

    private static List<(string Value, string Text)> FallbackTargetResolveFormOptions() =>
        TargetResolveTypeMap.Select(x => (x.Key, x.Value)).ToList();

    private static List<(string Value, string Text)> FallbackHandleModeFormOptions() =>
        HandleModeMap.Select(x => (x.Key, x.Value)).ToList();

    /// <summary>流转动作类型（字典未配置 FLOW_ACTION 时仅用内置码表）。</summary>
    public Task<List<(string Value, string Text)>> GetActionTypeFormOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.FlowAction, forFilter: false, formEmptyLabel: null,
            FallbackActionTypeFormOptions(), ct: ct);

    public Task<List<(string Value, string Text)>> GetTargetResolveFormOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.TargetResolve, forFilter: false, formEmptyLabel: "（不指定）",
            FallbackTargetResolveFormOptions(), ct: ct);

    public Task<List<(string Value, string Text)>> GetHandleModeFormOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.HandleMode, forFilter: false, formEmptyLabel: null,
            FallbackHandleModeFormOptions(), ct: ct);

    public Task<Dictionary<string, string>> GetActionTypeMapAsync(CancellationToken ct) =>
        _dict.GetItemMapMergedAsync(DictCodes.FlowAction, ActionTypeMap, ct: ct);

    public Task<Dictionary<string, string>> GetTargetResolveMapAsync(CancellationToken ct) =>
        _dict.GetItemMapMergedAsync(DictCodes.TargetResolve, TargetResolveTypeMap, ct: ct);

    public Task<Dictionary<string, string>> GetHandleModeMapAsync(CancellationToken ct) =>
        _dict.GetItemMapMergedAsync(DictCodes.HandleMode, HandleModeMap, ct: ct);

    public static List<(string Value, string Text)> BuildAppCodeFormOptions() =>
    [
        ("FRAME", "FRAME 框架"),
        ("CRM", "CRM"),
        ("OA", "OA")
    ];

    public string NormalizeBStatus(string? value) => EBStatusHelper.NormalizeBStatusForSave(value);

    public static string ToStatusDisplay(string? value)
    {
        var v = (value ?? "").Trim();
        return v switch
        {
            "1" or "启用" => "启用",
            "2" or "停用" => "停用",
            _ => v
        };
    }

    private static bool IsEventConfigActive(string? bStatus) => EBStatusHelper.IsActiveBStatus(bStatus);

    public async Task<List<(string Code, string Display)>> LoadActiveEventOptionsAsync(string? appCode, CancellationToken ct)
    {
        var ac = (appCode ?? "").Trim();
        var q = _db.EEventConfigs.AsNoTracking().Where(x => !x.IsDeleted);
        if (ac.Length > 0)
            q = q.Where(x => x.AppCode == ac);

        var rows = await q.OrderBy(x => x.EventCode).ToListAsync(ct);
        return rows
            .Where(x => IsEventConfigActive(x.BStatus))
            .Select(x => (x.EventCode, $"{x.EventCode} - {(x.EventName ?? "")}".Trim()))
            .ToList();
    }

    public async Task<List<(int Id, string Display)>> LoadActiveDepartmentOptionsAsync(CancellationToken ct)
    {
        return await _db.EDepartments.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.BStatus == "1" || x.BStatus == "启用"))
            .OrderBy(x => x.DeptCode)
            .Select(x => new ValueTuple<int, string>(x.DataId, $"{x.DeptCode} - {x.DeptCName}".Trim()))
            .ToListAsync(ct);
    }

    public async Task<List<(int Id, string Display)>> LoadActivePositionOptionsAsync(CancellationToken ct)
    {
        return await _db.EPositions.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.BStatus == "1" || x.BStatus == "启用"))
            .OrderBy(x => x.PostCode)
            .Select(x => new ValueTuple<int, string>(x.DataId, $"{x.PostCode} - {x.PostCName}".Trim()))
            .ToListAsync(ct);
    }

    public async Task<List<(int Id, string Display)>> LoadActiveDutyOptionsAsync(CancellationToken ct)
    {
        return await _db.EEDuties.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.BStatus == "1" || x.BStatus == "启用"))
            .OrderBy(x => x.DutyCode)
            .Select(x => new ValueTuple<int, string>(x.DataId, $"{x.DutyCode} - {x.DutyCName}".Trim()))
            .ToListAsync(ct);
    }

    public async Task<List<(int Id, string Display)>> LoadActiveUserOptionsAsync(CancellationToken ct)
    {
        return await _db.EUsers.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsEnabled && (x.BStatus == "1" || x.BStatus == "启用"))
            .OrderBy(x => x.LoginId)
            .Select(x => new ValueTuple<int, string>(x.DataId, $"{x.LoginId} - {x.RealName}".Trim()))
            .ToListAsync(ct);
    }

    public EEventFlowRuleFormVm ToForm(EEventFlowRule row) => new()
    {
        DataId = row.DataId,
        RuleCode = row.RuleCode ?? "",
        RuleName = row.RuleName ?? "",
        AppCode = string.IsNullOrWhiteSpace(row.AppCode) ? "FRAME" : row.AppCode,
        CurrentEvent = row.CurrentEvent ?? "",
        NextEvent = row.NextEvent,
        ConditionExpr = row.ConditionExpr,
        ActionType = row.ActionType ?? "CREATE_TODO",
        TargetResolveType = row.TargetResolveType,
        TargetDutyId = row.TargetDutyId,
        TargetPosId = row.TargetPosId,
        TargetDeptId = row.TargetDeptId,
        TargetUserId = row.TargetUserId,
        HandleMode = string.IsNullOrWhiteSpace(row.HandleMode) ? "SINGLE" : row.HandleMode,
        TimeoutMinutes = row.TimeoutMinutes,
        EscalateEventCode = row.EscalateEventCode,
        DispSeq = row.DispSeq < 0 ? 0 : row.DispSeq,
        BStatus = NormalizeBStatus(row.BStatus),
        Remark = row.Remark
    };

    public void NormalizeFormForSave(EEventFlowRuleFormVm model)
    {
        model.RuleCode = Normalize(model.RuleCode, 100);
        model.RuleName = Normalize(model.RuleName, 100);
        model.AppCode = Normalize(model.AppCode, 50, "FRAME");
        model.CurrentEvent = Normalize(model.CurrentEvent, 100);
        var ne = (model.NextEvent ?? "").Trim();
        model.NextEvent = ne.Length == 0 ? null : (ne.Length <= 100 ? ne : ne[..100]);
        var ce = (model.ConditionExpr ?? "").Trim();
        model.ConditionExpr = ce.Length == 0 ? null : (ce.Length <= 1000 ? ce : ce[..1000]);
        model.ActionType = Normalize(model.ActionType, 30, "CREATE_TODO").ToUpperInvariant();
        var rt = (model.TargetResolveType ?? "").Trim();
        model.TargetResolveType = rt.Length == 0 ? null : rt.ToUpperInvariant();
        model.HandleMode = Normalize(model.HandleMode, 30, "SINGLE").ToUpperInvariant();
        var esc = (model.EscalateEventCode ?? "").Trim();
        model.EscalateEventCode = esc.Length == 0 ? null : (esc.Length <= 100 ? esc : esc[..100]);
        model.BStatus = NormalizeBStatus(model.BStatus);
        model.Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark.Trim();
        if (model.DispSeq < 0) model.DispSeq = 0;
    }

    public static string BuildConditionSummary(string? expr) =>
        string.IsNullOrWhiteSpace(expr) ? "-" : ((expr ?? "").Trim().Length <= 48 ? expr!.Trim() : expr!.Trim()[..48] + "…");

    public static string BuildTargetSummary(
        string? resolveType,
        int? dutyId,
        int? posId,
        int? deptId,
        int? userId,
        IReadOnlyDictionary<string, string>? resolveMap = null)
    {
        var map = resolveMap ?? TargetResolveTypeMap;
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(resolveType))
            parts.Add(map.TryGetValue(resolveType.Trim(), out var t) ? t : resolveType);
        if (dutyId.HasValue) parts.Add($"职责#{dutyId}");
        if (posId.HasValue) parts.Add($"岗位#{posId}");
        if (deptId.HasValue) parts.Add($"部门#{deptId}");
        if (userId.HasValue) parts.Add($"用户#{userId}");
        return parts.Count == 0 ? "-" : string.Join(" / ", parts);
    }

    public async Task<(IReadOnlyList<EEventFlowRuleListRowVm> pageRows, int totalRecords, int totalPages, int page)> GetIndexPageAsync(
        string? bStatus1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EEventFlowRules.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(ct);
        rows = rows.Where(x => MatchBStatusFilter(x.BStatus, bStatus1)).ToList();

        var allEvents = await _db.EEventConfigs.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(ct);
        var eventMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in allEvents)
        {
            var key = $"{e.AppCode}|{e.EventCode}".Trim();
            var name = (e.EventName ?? "").Trim();
            eventMap[key] = name.Length == 0 ? e.EventCode : $"{e.EventCode} - {name}";
            eventMap[e.EventCode] = eventMap[key];
        }

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            bool Match(string? s) => (s ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase);
            rows = (searchField ?? "RuleCode") switch
            {
                "RuleCode" => rows.Where(x => Match(x.RuleCode)).ToList(),
                "RuleName" => rows.Where(x => Match(x.RuleName)).ToList(),
                "CurrentEvent" => rows.Where(x => Match(x.CurrentEvent)).ToList(),
                "NextEvent" => rows.Where(x => Match(x.NextEvent)).ToList(),
                "ConditionExpr" => rows.Where(x => Match(x.ConditionExpr)).ToList(),
                "AppCode" => rows.Where(x => Match(x.AppCode)).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow);

        var actionMap = await GetActionTypeMapAsync(ct);
        var resolveMap = await GetTargetResolveMapAsync(ct);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EEventFlowRuleListRowVm
        {
            DataId = x.DataId,
            DispSeq = x.DispSeq,
            RuleCodeDisplay = x.RuleCode,
            RuleNameDisplay = x.RuleName,
            AppCode = x.AppCode,
            CurrentEventDisplay = eventMap.TryGetValue($"{x.AppCode}|{x.CurrentEvent}".Trim(), out var cd)
                ? cd
                : (eventMap.TryGetValue(x.CurrentEvent, out var c2) ? c2 : x.CurrentEvent),
            NextEventDisplay = string.IsNullOrWhiteSpace(x.NextEvent)
                ? "-"
                : (eventMap.TryGetValue($"{x.AppCode}|{x.NextEvent}".Trim(), out var nd)
                    ? nd
                    : (eventMap.TryGetValue(x.NextEvent!, out var n2) ? n2 : x.NextEvent!)),
            ActionTypeText = actionMap.TryGetValue(x.ActionType, out var at) ? at : x.ActionType,
            ConditionSummary = BuildConditionSummary(x.ConditionExpr),
            TargetSummary = BuildTargetSummary(x.TargetResolveType, x.TargetDutyId, x.TargetPosId, x.TargetDeptId, x.TargetUserId, resolveMap),
            BStatusText = ToStatusDisplay(x.BStatus)
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(
        EEventFlowRuleFormVm model,
        bool isEdit,
        CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();

        var actionMap = await GetActionTypeMapAsync(ct);
        var handleMap = await GetHandleModeMapAsync(ct);
        var resolveMap = await GetTargetResolveMapAsync(ct);

        if (!actionMap.ContainsKey(model.ActionType))
            errors.Add((nameof(model.ActionType), "动作类型无效。"));
        if (!handleMap.ContainsKey(model.HandleMode))
            errors.Add((nameof(model.HandleMode), "处理模式无效。"));
        if (!string.IsNullOrWhiteSpace(model.TargetResolveType)
            && !resolveMap.ContainsKey(model.TargetResolveType))
            errors.Add((nameof(model.TargetResolveType), "处理人解析方式无效。"));

        var next = (model.NextEvent ?? "").Trim();
        if (next.Length > 0
            && string.Equals(model.CurrentEvent, next, StringComparison.OrdinalIgnoreCase))
            errors.Add((nameof(model.NextEvent), "禁止自环：当前事件与下一事件不能相同。"));

        if (model.TargetDeptId.HasValue
            && !await _db.EDepartments.AsNoTracking().AnyAsync(x => x.DataId == model.TargetDeptId.Value && !x.IsDeleted, ct))
            errors.Add((nameof(model.TargetDeptId), "目标部门不存在。"));
        if (model.TargetPosId.HasValue
            && !await _db.EPositions.AsNoTracking().AnyAsync(x => x.DataId == model.TargetPosId.Value && !x.IsDeleted, ct))
            errors.Add((nameof(model.TargetPosId), "目标岗位不存在。"));
        if (model.TargetDutyId.HasValue
            && !await _db.EEDuties.AsNoTracking().AnyAsync(x => x.DataId == model.TargetDutyId.Value && !x.IsDeleted, ct))
            errors.Add((nameof(model.TargetDutyId), "目标职责不存在。"));
        if (model.TargetUserId.HasValue
            && !await _db.EUsers.AsNoTracking().AnyAsync(x => x.DataId == model.TargetUserId.Value && !x.IsDeleted, ct))
            errors.Add((nameof(model.TargetUserId), "目标用户不存在。"));

        var curRow = await _db.EEventConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AppCode == model.AppCode && x.EventCode == model.CurrentEvent && !x.IsDeleted, ct);
        if (curRow is null || !IsEventConfigActive(curRow.BStatus))
            errors.Add((nameof(model.CurrentEvent), "当前事件在事件配置中不存在或未启用（请核对 AppCode）。"));

        if (next.Length > 0)
        {
            var nextRow = await _db.EEventConfigs.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppCode == model.AppCode && x.EventCode == next && !x.IsDeleted, ct);
            if (nextRow is null || !IsEventConfigActive(nextRow.BStatus))
                errors.Add((nameof(model.NextEvent), "下一事件在事件配置中不存在或未启用。"));
        }

        if (!string.IsNullOrWhiteSpace(model.EscalateEventCode))
        {
            var esc = await _db.EEventConfigs.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppCode == model.AppCode && x.EventCode == model.EscalateEventCode && !x.IsDeleted, ct);
            if (esc is null || !IsEventConfigActive(esc.BStatus))
                errors.Add((nameof(model.EscalateEventCode), "升级事件在事件配置中不存在或未启用。"));
        }

        var dupCode = await _db.EEventFlowRules.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted
                             && x.RuleCode == model.RuleCode
                             && (!isEdit || x.DataId != model.DataId), ct);
        if (dupCode)
            errors.Add((nameof(model.RuleCode), "规则编码已存在。"));

        var others = await _db.EEventFlowRules.AsNoTracking()
            .Where(x => !x.IsDeleted && (!isEdit || x.DataId != model.DataId))
            .ToListAsync(ct);
        if (others.Any(x => SameRuleKey(x, model)))
            errors.Add((nameof(model.RuleCode), "已存在相同应用、事件与目标条件的规则。"));

        return errors;
    }

    private static bool SameRuleKey(EEventFlowRule row, EEventFlowRuleFormVm m)
    {
        if (!string.Equals(row.AppCode.Trim(), m.AppCode.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(row.CurrentEvent.Trim(), m.CurrentEvent.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals((row.NextEvent ?? "").Trim(), (m.NextEvent ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(row.ActionType.Trim(), m.ActionType.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals((row.ConditionExpr ?? "").Trim(), (m.ConditionExpr ?? "").Trim(), StringComparison.Ordinal)) return false;
        if (!string.Equals((row.TargetResolveType ?? "").Trim(), (m.TargetResolveType ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        if (row.TargetDutyId != m.TargetDutyId) return false;
        if (row.TargetPosId != m.TargetPosId) return false;
        if (row.TargetDeptId != m.TargetDeptId) return false;
        if (row.TargetUserId != m.TargetUserId) return false;
        if (!string.Equals(row.HandleMode.Trim(), m.HandleMode.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    public async Task CreateAsync(EEventFlowRuleFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var entity = MapToEntity(model, operatorId, now);
        _db.EEventFlowRules.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> TryUpdateAsync(int id, EEventFlowRuleFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.EEventFlowRules.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return false;

        ApplyFormToRow(row, model);
        row.AmendDate = DateTime.Now;
        row.OperatorName = operatorId;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.EEventFlowRules.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return;
        row.IsDeleted = true;
        row.BStatus = "2";
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return;
        var rows = await _db.EEventFlowRules.Where(x => ids.Contains(x.DataId) && !x.IsDeleted).ToListAsync(ct);
        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.BStatus = "2";
            row.AmendDate = DateTime.Now;
        }
        if (rows.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    public async Task<EEventFlowRule?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.EEventFlowRules.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);

    private static EEventFlowRule MapToEntity(EEventFlowRuleFormVm model, string operatorId, DateTime now) =>
        new()
        {
            RuleCode = model.RuleCode,
            RuleName = model.RuleName,
            AppCode = model.AppCode,
            CurrentEvent = model.CurrentEvent,
            NextEvent = model.NextEvent,
            ConditionExpr = model.ConditionExpr,
            ActionType = model.ActionType,
            TargetResolveType = model.TargetResolveType,
            TargetDutyId = model.TargetDutyId,
            TargetPosId = model.TargetPosId,
            TargetDeptId = model.TargetDeptId,
            TargetUserId = model.TargetUserId,
            HandleMode = model.HandleMode,
            TimeoutMinutes = model.TimeoutMinutes,
            EscalateEventCode = model.EscalateEventCode,
            DispSeq = model.DispSeq,
            BStatus = model.BStatus,
            Remark = model.Remark,
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        };

    private static void ApplyFormToRow(EEventFlowRule row, EEventFlowRuleFormVm model)
    {
        row.RuleCode = model.RuleCode;
        row.RuleName = model.RuleName;
        row.AppCode = model.AppCode;
        row.CurrentEvent = model.CurrentEvent;
        row.NextEvent = model.NextEvent;
        row.ConditionExpr = model.ConditionExpr;
        row.ActionType = model.ActionType;
        row.TargetResolveType = model.TargetResolveType;
        row.TargetDutyId = model.TargetDutyId;
        row.TargetPosId = model.TargetPosId;
        row.TargetDeptId = model.TargetDeptId;
        row.TargetUserId = model.TargetUserId;
        row.HandleMode = model.HandleMode;
        row.TimeoutMinutes = model.TimeoutMinutes;
        row.EscalateEventCode = model.EscalateEventCode;
        row.DispSeq = model.DispSeq;
        row.BStatus = model.BStatus;
        row.Remark = model.Remark;
    }

    private static List<EEventFlowRule> SortRows(List<EEventFlowRule> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        IEnumerable<EEventFlowRule> ordered = field switch
        {
            "DispSeq" => desc ? rows.OrderByDescending(x => x.DispSeq) : rows.OrderBy(x => x.DispSeq),
            "DataID" => desc ? rows.OrderByDescending(x => x.DataId) : rows.OrderBy(x => x.DataId),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus) : rows.OrderBy(x => x.BStatus),
            "RuleCode" => desc ? rows.OrderByDescending(x => x.RuleCode) : rows.OrderBy(x => x.RuleCode),
            "CurrentEvent" => desc ? rows.OrderByDescending(x => x.CurrentEvent) : rows.OrderBy(x => x.CurrentEvent),
            "NextEvent" => desc ? rows.OrderByDescending(x => x.NextEvent) : rows.OrderBy(x => x.NextEvent),
            _ => rows.OrderBy(x => x.DispSeq).ThenBy(x => x.DataId)
        };
        return ordered.ToList();
    }

    private static bool MatchBStatusFilter(string? value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        var v = (value ?? "").Trim();
        return selected switch
        {
            "1" => v == "1" || v == "启用",
            "2" => v == "2" || v == "停用",
            _ => true
        };
    }
}
