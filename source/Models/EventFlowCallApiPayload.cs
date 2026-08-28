namespace FamilyTree.Models;

/// <summary>CALL_API 规则 POST 回调体。</summary>
public sealed class EventFlowCallApiPayload
{
    public long EventInstanceId { get; set; }
    public string AppCode { get; set; } = "";
    public string EventCode { get; set; } = "";
    public string RuleCode { get; set; } = "";
    public string ObjectType { get; set; } = "";
    public string ObjectKey { get; set; } = "";
    public string? ObjectTitle { get; set; }
    public string? PayloadJson { get; set; }
    public int? TriggerUserId { get; set; }
    public DateTime OccurredTime { get; set; }
}
