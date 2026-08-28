namespace FamilyTree.Models.ViewModels;

public sealed class EEventInstanceListRowVm
{
    public long DataId { get; set; }
    public string AppCode { get; set; } = "";
    public string EventCode { get; set; } = "";
    public string EventStatus { get; set; } = "";
    public string ObjectRefDisplay { get; set; } = "-";
    public DateTime OccurredTime { get; set; }
    public DateTime? ProcessTime { get; set; }
    public int ReceiverCount { get; set; }
    public int TodoCount { get; set; }
}

public sealed class EEventInstanceDetailVm
{
    public long DataId { get; set; }
    public string AppCode { get; set; } = "";
    public string EventCode { get; set; } = "";
    public string EventName { get; set; } = "";
    public string EventStatus { get; set; } = "";
    public string ObjectType { get; set; } = "";
    public string ObjectKey { get; set; } = "";
    public string ObjectRefDisplay { get; set; } = "-";
    public string? ObjectCode { get; set; }
    public string? ObjectTitle { get; set; }
    public string? ObjectUrl { get; set; }
    public string? PayloadJson { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? LastError { get; set; }
    public int RetryCount { get; set; }
    public string TriggerUserDisplay { get; set; } = "-";
    public DateTime OccurredTime { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? ProcessTime { get; set; }
    public int ReceiverCount { get; set; }
    public int TodoCount { get; set; }
    public int LogCount { get; set; }
    public IReadOnlyList<EEventInstanceReceiverRowVm> Receivers { get; set; } = Array.Empty<EEventInstanceReceiverRowVm>();
    public IReadOnlyList<EEventInstanceLogRowVm> RecentLogs { get; set; } = Array.Empty<EEventInstanceLogRowVm>();
}

public sealed class EEventInstanceReceiverRowVm
{
    public long DataId { get; set; }
    public string UserDisplay { get; set; } = "";
    public string ResolveType { get; set; } = "";
    public string? ResolveReason { get; set; }
}

public sealed class EEventInstanceLogRowVm
{
    public long DataId { get; set; }
    public DateTime CreateTime { get; set; }
    public string ActionType { get; set; } = "";
    public string HandleResult { get; set; } = "";
    public string? Remark { get; set; }
}
