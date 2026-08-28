using FamilyTree.Configuration;
using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FamilyTree.Controllers;

/// <summary>事件发布联调 Demo（页面 + JSON API）。</summary>
[Authorize]
public class EventDemoController : Controller
{
    private readonly EventPublisherService _publisher;
    private readonly EventFlowOptions _eventFlow;
    private readonly ILogger<EventDemoController> _logger;

    public EventDemoController(
        EventPublisherService publisher,
        IOptions<EventFlowOptions> eventFlow,
        ILogger<EventDemoController> logger)
    {
        _publisher = publisher;
        _eventFlow = eventFlow.Value;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (!CanView()) return Forbid();
        return View(new EventDemoPublishFormVm
        {
            ObjectKey = DateTime.Now.Ticks.ToString()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(EventDemoPublishFormVm model, CancellationToken ct = default)
    {
        if (!CanView()) return Forbid();
        if (!ModelState.IsValid) return View(nameof(Index), model);

        var userId = ResolveCurrentUserId();
        var result = await PublishInternalAsync(model, userId, ct);
        ViewBag.Result = ToResultVm(result);
        return View(nameof(Index), model);
    }

    /// <summary>POST /EventDemo/ApiCallback — CALL_API 规则回调联调（匿名，可配密钥头）。</summary>
    [HttpPost]
    [AllowAnonymous]
    public IActionResult ApiCallback([FromBody] EventFlowCallApiPayload? body)
    {
        var secret = _eventFlow.CallbackSecret;
        if (!string.IsNullOrWhiteSpace(secret))
        {
            var header = Request.Headers["X-EventFlow-Secret"].FirstOrDefault();
            if (!string.Equals(header, secret, StringComparison.Ordinal))
                return Unauthorized(new { success = false, message = "X-EventFlow-Secret 无效。" });
        }

        _logger.LogInformation(
            "ApiCallback 收到 EventInstanceId={Id} Rule={Rule} {App}.{Event} {Object}",
            body?.EventInstanceId, body?.RuleCode, body?.AppCode, body?.EventCode,
            body == null ? "-" : $"{body.ObjectType}/{body.ObjectKey}");

        return Ok(new
        {
            success = true,
            received = body != null,
            eventInstanceId = body?.EventInstanceId,
            ruleCode = body?.RuleCode
        });
    }

    /// <summary>POST /EventDemo/ApiPublish — JSON 联调入口。</summary>
    [HttpPost]
    public async Task<IActionResult> ApiPublish([FromBody] EventDemoPublishFormVm model, CancellationToken ct = default)
    {
        if (!CanView()) return Forbid();
        if (model == null)
            return BadRequest(new { success = false, message = "请求体不能为空。" });

        var userId = ResolveCurrentUserId();
        var result = await PublishInternalAsync(model, userId, ct);
        return Json(new
        {
            success = result.Success,
            message = result.Message,
            eventInstanceId = result.EventInstanceId,
            receiverCount = result.ReceiverCount,
            todoCreatedCount = result.TodoCreatedCount,
            idempotentHit = result.IdempotentHit
        });
    }

    private async Task<EventPublishResult> PublishInternalAsync(
        EventDemoPublishFormVm model,
        int? userId,
        CancellationToken ct)
    {
        var request = new EventPublishRequest
        {
            AppCode = (model.AppCode ?? "FRAME").Trim(),
            EventCode = (model.EventCode ?? "").Trim(),
            ObjectType = (model.ObjectType ?? "").Trim(),
            ObjectKey = (model.ObjectKey ?? "").Trim(),
            ObjectTitle = model.ObjectTitle?.Trim(),
            PayloadJson = string.IsNullOrWhiteSpace(model.PayloadJson) ? null : model.PayloadJson.Trim(),
            IdempotencyKey = string.IsNullOrWhiteSpace(model.IdempotencyKey) ? null : model.IdempotencyKey.Trim(),
            TriggerUserId = userId,
            OccurredTime = DateTime.Now
        };
        return await _publisher.PublishAsync(request, ct);
    }

    private int? ResolveCurrentUserId()
    {
        var raw = User.FindFirst(FrameworkClaimTypes.EUserId)?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }

    private static EventDemoPublishResultVm ToResultVm(EventPublishResult r) => new()
    {
        Success = r.Success,
        Message = r.Message,
        EventInstanceId = r.EventInstanceId,
        ReceiverCount = r.ReceiverCount,
        TodoCreatedCount = r.TodoCreatedCount,
        IdempotentHit = r.IdempotentHit
    };

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
