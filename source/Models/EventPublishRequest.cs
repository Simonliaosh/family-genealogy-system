namespace FamilyTree.Models;

/// <summary>业务侧发布事件请求（由 EventPublisherService 处理）。</summary>
public sealed class EventPublishRequest
{
    public string AppCode { get; set; } = "FRAME";
    public string EventCode { get; set; } = "";
    public string ObjectType { get; set; } = "";
    public string ObjectKey { get; set; } = "";
    public string? ObjectCode { get; set; }
    public string? ObjectTitle { get; set; }
    public string? ObjectUrl { get; set; }
    public int? TriggerUserId { get; set; }
    public int? TriggerDeptId { get; set; }
    public int? TriggerPosId { get; set; }
    public string? PayloadJson { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime? OccurredTime { get; set; }
}

public sealed class EventPublishResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public long? EventInstanceId { get; init; }
    public int TodoCreatedCount { get; init; }
    public int ReceiverCount { get; init; }
    public bool IdempotentHit { get; init; }

    public static EventPublishResult Ok(long instanceId, int receivers, int todos, string? msg = null) =>
        new() { Success = true, EventInstanceId = instanceId, ReceiverCount = receivers, TodoCreatedCount = todos, Message = msg ?? "OK" };

    public static EventPublishResult Fail(string msg) =>
        new() { Success = false, Message = msg };
}
