namespace FamilyTree.Models.ViewModels;

public sealed class EDictTypeListRowVm
{
    public string DictTypeCode { get; set; } = "";
    public string DictTypeName { get; set; } = "";
    public string AppCode { get; set; } = "";
    public string BStatusText { get; set; } = "";
    public bool IsSystem { get; set; }
    public bool IsEditable { get; set; }
    public int ItemCount { get; set; }
}

public sealed class EDictItemListRowVm
{
    public int DataId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string? ItemNameEn { get; set; }
    public string? ParentItemCode { get; set; }
    public int DispSeq { get; set; }
    public string BStatusText { get; set; } = "";
    public bool IsSystem { get; set; }
}
