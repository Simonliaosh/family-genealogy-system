using System.Net.Http.Json;
using FamilyTree.Configuration;
using FamilyTree.Helpers;
using FamilyTree.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FamilyTree.Services;

/// <summary>
/// 事件发布执行引擎：实例化 → 订阅/流转规则解析接收人 → 写接收人表 → 按需生成待办 → 可选链式触发下一事件。
/// </summary>
public sealed class EventPublisherService
{
    private const int MaxEventChainDepth = 5;

    private readonly FrameworkDbContext _db;
    private readonly ILogger<EventPublisherService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EventFlowOptions _eventFlow;

    public EventPublisherService(
        FrameworkDbContext db,
        ILogger<EventPublisherService> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<EventFlowOptions> eventFlow)
    {
        _db = db;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _eventFlow = eventFlow.Value;
    }

    public Task<EventPublishResult> PublishAsync(EventPublishRequest request, CancellationToken ct = default) =>
        PublishCoreAsync(request, chainDepth: 0, ct);

    private async Task<EventPublishResult> PublishCoreAsync(
        EventPublishRequest request,
        int chainDepth,
        CancellationToken ct)
    {
        var appCode = (request.AppCode ?? "FRAME").Trim();
        var eventCode = (request.EventCode ?? "").Trim();
        var objectType = (request.ObjectType ?? "").Trim();
        var objectKey = (request.ObjectKey ?? "").Trim();

        if (eventCode.Length == 0 || objectType.Length == 0 || objectKey.Length == 0)
            return EventPublishResult.Fail("EventCode、ObjectType、ObjectKey 不能为空。");

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _db.EEventInstances.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey.Trim(), ct);
            if (existing != null)
            {
                var existingTodoCount = await _db.ETodoTasks.CountAsync(t => t.EventInstanceId == existing.DataId, ct);
                var recvCount = await _db.EEventReceivers.CountAsync(r => r.EventInstanceId == existing.DataId, ct);
                return new EventPublishResult
                {
                    Success = true,
                    IdempotentHit = true,
                    EventInstanceId = existing.DataId,
                    ReceiverCount = recvCount,
                    TodoCreatedCount = existingTodoCount,
                    Message = "幂等命中，返回已有实例。"
                };
            }
        }

        var cfg = await _db.EEventConfigs.AsNoTracking()
            .Where(x => x.AppCode == appCode && x.EventCode == eventCode && !x.IsDeleted)
            .WhereActiveBStatus(x => x.BStatus)
            .FirstOrDefaultAsync(ct);
        if (cfg == null)
            return EventPublishResult.Fail($"未找到启用的事件配置：{appCode}.{eventCode}");

        var now = DateTime.Now;
        var instance = new EEventInstance
        {
            AppCode = appCode,
            EventCode = eventCode,
            ObjectType = objectType,
            ObjectKey = objectKey,
            ObjectCode = request.ObjectCode?.Trim(),
            ObjectTitle = request.ObjectTitle?.Trim(),
            ObjectUrl = request.ObjectUrl?.Trim(),
            TriggerUserId = request.TriggerUserId,
            TriggerDeptId = request.TriggerDeptId,
            TriggerPosId = request.TriggerPosId,
            PayloadJson = request.PayloadJson,
            IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim(),
            EventStatus = "PROCESSING",
            OccurredTime = request.OccurredTime ?? now,
            CreateTime = now,
            RetryCount = 0
        };
        _db.EEventInstances.Add(instance);
        await _db.SaveChangesAsync(ct);

        await AppendEventLogAsync(instance.DataId, appCode, eventCode, objectType, objectKey,
            request.TriggerUserId, "RAISE", "SUCCESS", "事件已接收", ct);

        List<ResolvedReceiver> receivers;
        string? todoHandleModeOverride = null;
        try
        {
            var fromSubs = await ResolveReceiversFromSubscriptionsAsync(appCode, eventCode, request, ct);
            var (fromRules, handleOverride, rulesMatched) =
                await ResolveReceiversFromFlowRulesAsync(appCode, eventCode, request, ct);
            receivers = MergeReceivers(fromSubs, fromRules);
            todoHandleModeOverride = handleOverride;
            if (rulesMatched > 0)
            {
                await AppendEventLogAsync(instance.DataId, appCode, eventCode, objectType, objectKey,
                    request.TriggerUserId, "FLOW", "SUCCESS", $"匹配流转规则 {rulesMatched} 条", ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析接收人失败 {App}.{Event}", appCode, eventCode);
            instance.EventStatus = "FAILED";
            instance.LastError = ex.Message;
            instance.ProcessTime = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            await AppendEventLogAsync(instance.DataId, appCode, eventCode, objectType, objectKey,
                request.TriggerUserId, "RESOLVE", "FAIL", ex.Message, ct);
            return EventPublishResult.Fail("解析接收人失败：" + ex.Message);
        }

        foreach (var r in receivers)
        {
            _db.EEventReceivers.Add(new EEventReceiver
            {
                EventInstanceId = instance.DataId,
                ReceiverUserId = r.UserId,
                ReceiverMemberId = r.MemberId,
                ReceiverDeptId = r.DeptId,
                ReceiverPosId = r.PosId,
                ReceiverDutyId = r.DutyId,
                SubscriptionId = r.SubscriptionId,
                ResolveType = r.ResolveType,
                ResolveReason = r.ResolveReason,
                CreateTime = now
            });
        }
        await _db.SaveChangesAsync(ct);

        await AppendEventLogAsync(instance.DataId, appCode, eventCode, objectType, objectKey,
            request.TriggerUserId, "RESOLVE", "SUCCESS", $"解析 {receivers.Count} 人", ct);

        var todoCount = 0;
        if (cfg.IsGenerateTodo && receivers.Count > 0 && ShouldCreateTodo(cfg, todoHandleModeOverride))
        {
            todoCount = await CreateTodosAsync(instance, cfg, receivers, request, todoHandleModeOverride, ct);
            await AppendEventLogAsync(instance.DataId, appCode, eventCode, objectType, objectKey,
                request.TriggerUserId, "DELIVER", "SUCCESS", $"生成待办 {todoCount} 条", ct);
        }

        instance.EventStatus = "DONE";
        instance.ProcessTime = DateTime.Now;
        await _db.SaveChangesAsync(ct);

        await ProcessNonTodoFlowRulesAsync(instance, appCode, eventCode, request, chainDepth, ct);
        await ProcessTriggerEventRulesAsync(instance, appCode, eventCode, request, chainDepth, ct);

        return EventPublishResult.Ok(instance.DataId, receivers.Count, todoCount);
    }

    private static bool ShouldCreateTodo(EEventConfig cfg, string? handleModeOverride)
    {
        var mode = (handleModeOverride ?? cfg.HandleMode ?? "SINGLE").Trim().ToUpperInvariant();
        return mode is "SINGLE" or "ALL" or "ANY" or "CLAIM";
    }

    private async Task<int> CreateTodosAsync(
        EEventInstance instance,
        EEventConfig cfg,
        IReadOnlyList<ResolvedReceiver> receivers,
        EventPublishRequest request,
        string? handleModeOverride,
        CancellationToken ct)
    {
        var handleMode = NormalizeHandleMode(handleModeOverride ?? cfg.HandleMode);
        var title = BuildTodoTitle(cfg, request);
        var due = cfg.DefaultDueMinutes.HasValue && cfg.DefaultDueMinutes.Value > 0
            ? DateTime.Now.AddMinutes(cfg.DefaultDueMinutes.Value)
            : (DateTime?)null;
        var groupCode = $"{instance.DataId}:{handleMode}";
        var now = DateTime.Now;

        ETodoGroup? group = null;
        if (handleMode is "ALL" or "ANY")
        {
            group = new ETodoGroup
            {
                TenantId = 0,
                GroupCode = groupCode,
                EventInstanceId = instance.DataId,
                AppCode = instance.AppCode,
                EventCode = instance.EventCode,
                ObjectType = instance.ObjectType,
                ObjectKey = instance.ObjectKey,
                HandleMode = handleMode,
                TotalCount = receivers.Count,
                DoneCount = 0,
                GroupStatus = "PENDING",
                DueTime = due,
                CreateTime = now,
                UpdateTime = now
            };
            _db.ETodoGroups.Add(group);
            await _db.SaveChangesAsync(ct);
        }

        var distinctUsers = receivers
            .GroupBy(r => r.UserId)
            .Select(g => g.First())
            .ToList();

        foreach (var r in distinctUsers)
        {
            _db.ETodoTasks.Add(new ETodoTask
            {
                EventInstanceId = instance.DataId,
                AppCode = instance.AppCode,
                EventCode = instance.EventCode,
                UserId = r.UserId,
                ObjectType = instance.ObjectType,
                ObjectKey = instance.ObjectKey,
                ObjectCode = instance.ObjectCode,
                ObjectTitle = instance.ObjectTitle,
                ObjectUrl = instance.ObjectUrl ?? cfg.PageUrl,
                Status = 0,
                TodoTitle = title,
                HandlerDeptId = r.DeptId,
                HandlerPosId = r.PosId,
                DueTime = due,
                Priority = "NORMAL",
                CreateTime = now,
                TodoGroupId = group?.DataId,
                OriginalUserId = r.UserId
            });
        }

        await _db.SaveChangesAsync(ct);
        return distinctUsers.Count;
    }

    private static string BuildTodoTitle(EEventConfig cfg, EventPublishRequest request)
    {
        var t = (cfg.TodoTitle ?? "").Trim();
        if (t.Length > 0) return t;
        var obj = (request.ObjectTitle ?? request.ObjectKey ?? "").Trim();
        return string.IsNullOrEmpty(obj) ? cfg.EventName : $"{cfg.EventName} - {obj}";
    }

    private static string NormalizeHandleMode(string? mode)
    {
        var m = (mode ?? "SINGLE").Trim().ToUpperInvariant();
        return m switch
        {
            "ALL" or "ANY" or "SINGLE" or "CLAIM" => m,
            "Single" => "SINGLE",
            _ => "SINGLE"
        };
    }

    private async Task<List<ResolvedReceiver>> ResolveReceiversFromSubscriptionsAsync(
        string appCode,
        string eventCode,
        EventPublishRequest request,
        CancellationToken ct)
    {
        var subs = (await _db.ESubscriptions.AsNoTracking()
            .Where(s => s.EventCode == eventCode && !s.IsDeleted)
            .WhereActiveBStatus(s => s.BStatus)
            .ToListAsync(ct))
            .Where(s => IsEventOrMixed(s.SubType))
            .Where(s => string.IsNullOrWhiteSpace(s.AppCode)
                        || string.Equals(s.AppCode.Trim(), appCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var result = new List<ResolvedReceiver>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sub in subs)
        {
            var dutyId = sub.DutyId;
            var activePd = await _db.EPositionDuties.AsNoTracking()
                .Where(pd => pd.DutyId == dutyId)
                .WhereActiveBStatus(pd => pd.BStatus)
                .ToListAsync(ct);
            if (activePd.Count == 0) continue;

            var posIds = activePd.Select(x => x.PosId).Distinct().ToList();
            var userPos = await _db.EUserPositions.AsNoTracking()
                .Where(up => posIds.Contains(up.PosId))
                .WhereActiveBStatus(up => up.BStatus)
                .ToListAsync(ct);

            foreach (var up in userPos)
            {
                if (sub.DeptId.HasValue && up.DeptId != sub.DeptId.Value)
                    continue;

                var pd = activePd.FirstOrDefault(x => x.PosId == up.PosId
                    && (x.DeptId == null || x.DeptId == up.DeptId));
                if (pd == null) continue;

                await TryAddReceiverAsync(result, seen, up.UserId, up.DeptId, up.PosId, dutyId,
                    sub.DataId, null, "DUTY", $"职责订阅 DutyID={dutyId}", ct);
            }
        }

        return result;
    }

    private async Task<(List<ResolvedReceiver> Receivers, string? HandleModeOverride, int RulesMatched)>
        ResolveReceiversFromFlowRulesAsync(
            string appCode,
            string eventCode,
            EventPublishRequest request,
            CancellationToken ct)
    {
        var rules = (await _db.EEventFlowRules.AsNoTracking()
            .Where(r => !r.IsDeleted && r.AppCode == appCode && r.CurrentEvent == eventCode)
            .WhereActiveBStatus(r => r.BStatus)
            .OrderBy(r => r.DispSeq)
            .ThenBy(r => r.DataId)
            .ToListAsync(ct))
            .Where(r => EventFlowConditionHelper.Matches(r.ConditionExpr, request.PayloadJson))
            .ToList();

        var result = new List<ResolvedReceiver>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? handleOverride = null;
        var matched = 0;

        foreach (var rule in rules)
        {
            matched++;
            var action = (rule.ActionType ?? "").Trim().ToUpperInvariant();
            if (action != "CREATE_TODO")
                continue;

            if (!string.IsNullOrWhiteSpace(rule.HandleMode))
                handleOverride = rule.HandleMode;

            var resolveType = (rule.TargetResolveType ?? "").Trim().ToUpperInvariant();
            if (resolveType.Length == 0)
                continue;

            var added = await ResolveReceiversForFlowRuleAsync(rule, resolveType, request, result, seen, ct);
            if (added == 0)
            {
                _logger.LogWarning("流转规则 {Rule} 未解析到接收人 TargetResolveType={Type}",
                    rule.RuleCode, resolveType);
            }
        }

        return (result, handleOverride, matched);
    }

    private async Task<int> ResolveReceiversForFlowRuleAsync(
        EEventFlowRule rule,
        string resolveType,
        EventPublishRequest request,
        List<ResolvedReceiver> result,
        HashSet<string> seen,
        CancellationToken ct)
    {
        var before = result.Count;
        switch (resolveType)
        {
            case "DUTY":
                if (rule.TargetDutyId.HasValue)
                    await ResolveByDutyIdAsync(rule.TargetDutyId.Value, rule.DataId, result, seen,
                        $"流转规则 {rule.RuleCode}", ct);
                break;
            case "POSITION":
                if (rule.TargetPosId.HasValue)
                    await ResolveByPositionIdAsync(rule.TargetPosId.Value, rule.TargetDeptId, rule.DataId,
                        result, seen, $"流转规则 {rule.RuleCode}", ct);
                break;
            case "FIXED_USER":
                if (rule.TargetUserId.HasValue)
                    await TryAddReceiverAsync(result, seen, rule.TargetUserId.Value,
                        request.TriggerDeptId ?? 0, request.TriggerPosId ?? 0, null,
                        null, rule.DataId, "FIXED_USER", $"流转规则 {rule.RuleCode}", ct);
                break;
            case "DEPT_MANAGER":
                {
                    var deptId = rule.TargetDeptId ?? request.TriggerDeptId;
                    if (deptId.HasValue)
                    {
                        var dept = await _db.EDepartments.AsNoTracking()
                            .FirstOrDefaultAsync(d => d.DataId == deptId.Value && !d.IsDeleted, ct);
                        if (dept?.LeaderUserId is > 0)
                            await TryAddReceiverAsync(result, seen, dept.LeaderUserId.Value,
                                deptId.Value, request.TriggerPosId ?? 0, null,
                                null, rule.DataId, "DEPT_MANAGER", $"部门负责人 DeptID={deptId}", ct);
                    }
                }
                break;
            case "OWNER_MANAGER":
                if (request.TriggerUserId.HasValue)
                {
                    var active = await _db.EManagerSubordinates.AsNoTracking()
                        .Where(m => m.SubUserId == request.TriggerUserId.Value)
                        .WhereActiveBStatus(m => m.BStatus)
                        .ToListAsync(ct);
                    foreach (var m in active)
                    {
                        await TryAddReceiverAsync(result, seen, m.ManagerUserId,
                            m.DeptId ?? request.TriggerDeptId ?? 0,
                            m.ManagerPostId ?? request.TriggerPosId ?? 0, null,
                            null, rule.DataId, "OWNER_MANAGER", $"上级 UserID={m.ManagerUserId}", ct);
                    }
                }
                break;
            case "TRIGGER_USER":
                if (request.TriggerUserId.HasValue)
                    await TryAddReceiverAsync(result, seen, request.TriggerUserId.Value,
                        request.TriggerDeptId ?? 0, request.TriggerPosId ?? 0, null,
                        null, rule.DataId, "TRIGGER_USER", "发布人 TriggerUserId", ct);
                break;
        }
        return result.Count - before;
    }

    private async Task ResolveByDutyIdAsync(
        int dutyId,
        int flowRuleId,
        List<ResolvedReceiver> result,
        HashSet<string> seen,
        string reasonPrefix,
        CancellationToken ct)
    {
        var activePd = await _db.EPositionDuties.AsNoTracking()
            .Where(pd => pd.DutyId == dutyId)
            .WhereActiveBStatus(pd => pd.BStatus)
            .ToListAsync(ct);
        var posIds = activePd.Select(x => x.PosId).Distinct().ToList();
        var userPos = await _db.EUserPositions.AsNoTracking()
            .Where(up => posIds.Contains(up.PosId))
            .WhereActiveBStatus(up => up.BStatus)
            .ToListAsync(ct);

        foreach (var up in userPos)
        {
            var pd = activePd.FirstOrDefault(x => x.PosId == up.PosId
                && (x.DeptId == null || x.DeptId == up.DeptId));
            if (pd == null) continue;
            await TryAddReceiverAsync(result, seen, up.UserId, up.DeptId, up.PosId, dutyId,
                null, flowRuleId, "DUTY", $"{reasonPrefix} DutyID={dutyId}", ct);
        }
    }

    private async Task ResolveByPositionIdAsync(
        int posId,
        int? deptIdFilter,
        int flowRuleId,
        List<ResolvedReceiver> result,
        HashSet<string> seen,
        string reasonPrefix,
        CancellationToken ct)
    {
        var userPos = await _db.EUserPositions.AsNoTracking()
            .Where(up => up.PosId == posId)
            .WhereActiveBStatus(up => up.BStatus)
            .ToListAsync(ct);

        foreach (var up in userPos)
        {
            if (deptIdFilter.HasValue && up.DeptId != deptIdFilter.Value)
                continue;
            await TryAddReceiverAsync(result, seen, up.UserId, up.DeptId, up.PosId, null,
                null, flowRuleId, "POSITION", $"{reasonPrefix} PosID={posId}", ct);
        }
    }

    private async Task TryAddReceiverAsync(
        List<ResolvedReceiver> result,
        HashSet<string> seen,
        int userId,
        int deptId,
        int posId,
        int? dutyId,
        int? subscriptionId,
        int? flowRuleId,
        string resolveType,
        string resolveReason,
        CancellationToken ct)
    {
        var user = await _db.EUsers.AsNoTracking().FirstOrDefaultAsync(u => u.DataId == userId, ct);
        if (user == null || user.IsDeleted || !user.IsEnabled
            || !EBStatusHelper.IsActiveBStatus(user.BStatus))
            return;

        var key = $"{userId}:{subscriptionId}:{flowRuleId}";
        if (!seen.Add(key)) return;

        int? memberId = null;
        var member = await _db.EMembers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MemberId == user.LoginId, ct);
        if (member != null) memberId = member.DataId;

        result.Add(new ResolvedReceiver(
            userId, memberId, deptId, posId, dutyId,
            subscriptionId, flowRuleId, resolveType, resolveReason));
    }

    private static List<ResolvedReceiver> MergeReceivers(
        List<ResolvedReceiver> primary,
        List<ResolvedReceiver> extra)
    {
        var map = new Dictionary<int, ResolvedReceiver>();
        foreach (var r in primary)
            map[r.UserId] = r;
        foreach (var r in extra)
        {
            if (!map.ContainsKey(r.UserId))
                map[r.UserId] = r;
        }
        return map.Values.ToList();
    }

    private async Task ProcessNonTodoFlowRulesAsync(
        EEventInstance instance,
        string appCode,
        string eventCode,
        EventPublishRequest request,
        int chainDepth,
        CancellationToken ct)
    {
        var rules = await LoadMatchedFlowRulesAsync(appCode, eventCode, request, ct);
        foreach (var rule in rules)
        {
            var action = (rule.ActionType ?? "").Trim().ToUpperInvariant();
            switch (action)
            {
                case "SEND_NOTICE":
                    {
                        var noticeList = new List<ResolvedReceiver>();
                        var noticeSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var noticeType = (rule.TargetResolveType ?? "").Trim().ToUpperInvariant();
                        if (noticeType.Length > 0)
                        {
                            await ResolveReceiversForFlowRuleAsync(rule, noticeType, request,
                                noticeList, noticeSeen, ct);
                        }
                        await AppendEventLogAsync(instance.DataId, appCode, eventCode,
                            instance.ObjectType, instance.ObjectKey, request.TriggerUserId,
                            "FLOW", "SUCCESS",
                            $"SEND_NOTICE 规则 {rule.RuleCode}，通知对象 {noticeList.Count} 人", ct);
                    }
                    break;
                case "CALL_API":
                    await ExecuteCallApiAsync(instance, appCode, eventCode, request, rule, ct);
                    break;
            }
        }
    }

    private async Task ProcessTriggerEventRulesAsync(
        EEventInstance instance,
        string appCode,
        string eventCode,
        EventPublishRequest request,
        int chainDepth,
        CancellationToken ct)
    {
        if (chainDepth >= MaxEventChainDepth)
        {
            _logger.LogWarning("事件链深度已达上限 {Depth}，跳过 TRIGGER_EVENT", chainDepth);
            return;
        }

        var rules = await LoadMatchedFlowRulesAsync(appCode, eventCode, request, ct);
        foreach (var rule in rules)
        {
            if (!string.Equals((rule.ActionType ?? "").Trim(), "TRIGGER_EVENT", StringComparison.OrdinalIgnoreCase))
                continue;
            var next = (rule.NextEvent ?? "").Trim();
            if (next.Length == 0)
                continue;

            var nextCfg = await _db.EEventConfigs.AsNoTracking()
                .Where(x => x.AppCode == appCode && x.EventCode == next && !x.IsDeleted)
                .WhereActiveBStatus(x => x.BStatus)
                .FirstOrDefaultAsync(ct);
            if (nextCfg == null)
            {
                await AppendEventLogAsync(instance.DataId, appCode, eventCode,
                    instance.ObjectType, instance.ObjectKey, request.TriggerUserId,
                    "FLOW", "FAIL", $"下一事件未配置或未启用：{appCode}.{next}", ct);
                continue;
            }

            await AppendEventLogAsync(instance.DataId, appCode, eventCode,
                instance.ObjectType, instance.ObjectKey, request.TriggerUserId,
                "FLOW", "SUCCESS", $"触发下一事件 {next}（规则 {rule.RuleCode}）", ct);

            var childRequest = new EventPublishRequest
            {
                AppCode = appCode,
                EventCode = next,
                ObjectType = request.ObjectType,
                ObjectKey = request.ObjectKey,
                ObjectCode = request.ObjectCode,
                ObjectTitle = request.ObjectTitle,
                ObjectUrl = request.ObjectUrl,
                TriggerUserId = request.TriggerUserId,
                TriggerDeptId = request.TriggerDeptId,
                TriggerPosId = request.TriggerPosId,
                PayloadJson = request.PayloadJson,
                OccurredTime = request.OccurredTime,
                IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                    ? null
                    : $"{request.IdempotencyKey.Trim()}:{rule.RuleCode}->{next}"
            };

            var childResult = await PublishCoreAsync(childRequest, chainDepth + 1, ct);
            if (!childResult.Success)
            {
                _logger.LogWarning("链式事件 {Next} 发布失败：{Msg}", next, childResult.Message);
            }
        }
    }

    private async Task ExecuteCallApiAsync(
        EEventInstance instance,
        string appCode,
        string eventCode,
        EventPublishRequest request,
        EEventFlowRule rule,
        CancellationToken ct)
    {
        var url = ResolveCallApiUrl(rule);
        if (string.IsNullOrWhiteSpace(url))
        {
            await AppendEventLogAsync(instance.DataId, appCode, eventCode,
                instance.ObjectType, instance.ObjectKey, request.TriggerUserId,
                "FLOW", "SKIP",
                $"CALL_API 规则 {rule.RuleCode}：未配置 URL（Remark 路径或 EventFlow:PublicBaseUrl / CallApiUrls）", ct);
            return;
        }

        var payload = new EventFlowCallApiPayload
        {
            EventInstanceId = instance.DataId,
            AppCode = appCode,
            EventCode = eventCode,
            RuleCode = rule.RuleCode,
            ObjectType = instance.ObjectType,
            ObjectKey = instance.ObjectKey,
            ObjectTitle = instance.ObjectTitle,
            PayloadJson = request.PayloadJson,
            TriggerUserId = request.TriggerUserId,
            OccurredTime = instance.OccurredTime
        };

        try
        {
            var client = _httpClientFactory.CreateClient("EventFlowCallApi");
            var timeoutSec = _eventFlow.CallApiTimeoutSeconds;
            if (timeoutSec < 1) timeoutSec = 15;
            client.Timeout = TimeSpan.FromSeconds(timeoutSec);

            if (!string.IsNullOrWhiteSpace(_eventFlow.CallbackSecret))
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-EventFlow-Secret", _eventFlow.CallbackSecret);

            using var response = await client.PostAsJsonAsync(url, payload, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            var bodyShort = body.Length > 200 ? body[..200] + "..." : body;

            if (response.IsSuccessStatusCode)
            {
                await AppendEventLogAsync(instance.DataId, appCode, eventCode,
                    instance.ObjectType, instance.ObjectKey, request.TriggerUserId,
                    "FLOW", "SUCCESS",
                    $"CALL_API {rule.RuleCode} → {(int)response.StatusCode} {url} {bodyShort}", ct);
            }
            else
            {
                await AppendEventLogAsync(instance.DataId, appCode, eventCode,
                    instance.ObjectType, instance.ObjectKey, request.TriggerUserId,
                    "FLOW", "FAIL",
                    $"CALL_API {rule.RuleCode} → {(int)response.StatusCode} {bodyShort}", ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CALL_API 失败 {Rule} {Url}", rule.RuleCode, url);
            await AppendEventLogAsync(instance.DataId, appCode, eventCode,
                instance.ObjectType, instance.ObjectKey, request.TriggerUserId,
                "FLOW", "FAIL", $"CALL_API {rule.RuleCode} 异常：{ex.Message}", ct);
        }
    }

    private string? ResolveCallApiUrl(EEventFlowRule rule)
    {
        if (_eventFlow.CallApiUrls.TryGetValue(rule.RuleCode, out var configured)
            && !string.IsNullOrWhiteSpace(configured))
            return ToAbsoluteCallApiUrl(configured.Trim());

        var remark = (rule.Remark ?? "").Trim();
        if (remark.Length > 0)
            return ToAbsoluteCallApiUrl(remark);

        return null;
    }

    private string? ToAbsoluteCallApiUrl(string urlOrPath)
    {
        if (urlOrPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || urlOrPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return urlOrPath;

        var path = urlOrPath.StartsWith('/') ? urlOrPath : "/" + urlOrPath;
        var baseUrl = (_eventFlow.PublicBaseUrl ?? "").Trim().TrimEnd('/');
        return baseUrl.Length == 0 ? null : baseUrl + path;
    }

    private async Task<List<EEventFlowRule>> LoadMatchedFlowRulesAsync(
        string appCode,
        string eventCode,
        EventPublishRequest request,
        CancellationToken ct)
    {
        var rules = (await _db.EEventFlowRules.AsNoTracking()
            .Where(r => !r.IsDeleted && r.AppCode == appCode && r.CurrentEvent == eventCode)
            .WhereActiveBStatus(r => r.BStatus)
            .OrderBy(r => r.DispSeq)
            .ThenBy(r => r.DataId)
            .ToListAsync(ct))
            .Where(r => EventFlowConditionHelper.Matches(r.ConditionExpr, request.PayloadJson))
            .ToList();
        return rules;
    }

    private static bool IsEventOrMixed(string? subType)
    {
        var t = (subType ?? "").Trim();
        return t.Equals("EVENT", StringComparison.OrdinalIgnoreCase)
               || t.Equals("MIXED", StringComparison.OrdinalIgnoreCase);
    }

    private async Task AppendEventLogAsync(
        long instanceId,
        string appCode,
        string eventCode,
        string objectType,
        string objectKey,
        int? userId,
        string actionType,
        string handleResult,
        string? remark,
        CancellationToken ct)
    {
        _db.EEventLogs.Add(new EEventLog
        {
            EventInstanceId = instanceId,
            AppCode = appCode,
            EventCode = eventCode,
            ObjectType = objectType,
            ObjectKey = objectKey,
            UserId = userId,
            ActionType = actionType,
            HandleResult = handleResult,
            Remark = remark,
            CreateTime = DateTime.Now
        });
        await _db.SaveChangesAsync(ct);
    }

    private sealed record ResolvedReceiver(
        int UserId,
        int? MemberId,
        int DeptId,
        int PosId,
        int? DutyId,
        int? SubscriptionId,
        int? FlowRuleId,
        string ResolveType,
        string ResolveReason);
}
