namespace FamilyTree.Models.ViewModels;

public sealed class EAppModuleListRowVm
{
    public int DataId { get; set; }
    public string AppCode { get; set; } = "";
    public string AppName { get; set; } = "";
    public string AppType { get; set; } = "";
    public string AppTypeText { get; set; } = "";
    public int DispSeq { get; set; }
    public string BStatusText { get; set; } = "";
}

public sealed class EAppModuleDetailVm
{
    public int DataId { get; set; }
    public string AppCode { get; set; } = "";
    public string AppName { get; set; } = "";
    public string AppType { get; set; } = "";
    public string AppTypeText { get; set; } = "";
    public string? BaseUrl { get; set; }
    public string? Icon { get; set; }
    public int DispSeq { get; set; }
    public string BStatusText { get; set; } = "";
    public string? Remark { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime AmendDate { get; set; }
    public string OperatorName { get; set; } = "";
}
