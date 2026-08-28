namespace FamilyTree.Models.ViewModels;

public sealed class ELoginLogListRowVm
{
    public long DataId { get; set; }
    public DateTime LoginTime { get; set; }
    public string LoginId { get; set; } = "";
    public string LoginStatusText { get; set; } = "";
    public string FailReasonDisplay { get; set; } = "";
    public string IpDisplay { get; set; } = "";
    public string UserDisplay { get; set; } = "";
}

public sealed class ELoginLogDetailVm
{
    public long DataId { get; set; }
    public DateTime LoginTime { get; set; }
    public string LoginId { get; set; } = "";
    public int? UserId { get; set; }
    public string UserDisplay { get; set; } = "";
    public string LoginStatus { get; set; } = "";
    public string LoginStatusText { get; set; } = "";
    public string? FailReason { get; set; }
    public string? IPAddress { get; set; }
}
