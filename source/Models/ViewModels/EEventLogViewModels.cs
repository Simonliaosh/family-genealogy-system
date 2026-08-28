namespace FamilyTree.Models.ViewModels;

public sealed class EEventLogListRowVm
{
    public long DataId { get; set; }
    public DateTime CreateTime { get; set; }
    public string EventDisplay { get; set; } = "";
    public string ActionTypeText { get; set; } = "";
    public string HandleResultText { get; set; } = "";
    public string UserDisplay { get; set; } = "";
    public string ObjectType { get; set; } = "";
    public string ObjectKey { get; set; } = "";
    public string ObjectRefDisplay { get; set; } = "-";
    public string RemarkShort { get; set; } = "";
}

public sealed class EEventLogDetailVm
{
    public long DataId { get; set; }
    public DateTime CreateTime { get; set; }
    public string EventCode { get; set; } = "";
    public string EventName { get; set; } = "";
    public string AppCode { get; set; } = "";
    public string ObjectType { get; set; } = "";
    public string ObjectKey { get; set; } = "";
    public string ObjectRefDisplay { get; set; } = "-";
    public int UserId { get; set; }
    public string UserDisplay { get; set; } = "";
    public string ActionType { get; set; } = "";
    public string ActionTypeText { get; set; } = "";
    public string HandleResult { get; set; } = "";
    public string HandleResultText { get; set; } = "";
    public string? Remark { get; set; }
}
