using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EDictItemFormVm
{
    [Required]
    [StringLength(50)]
    public string DictTypeCode { get; set; } = "";

    public int DataId { get; set; }

    [Required(ErrorMessage = "条目编码不能为空")]
    [StringLength(50)]
    [RegularExpression(@"^[A-Za-z0-9_.-]+$", ErrorMessage = "条目编码仅允许字母、数字、下划线、点、中横线")]
    public string ItemCode { get; set; } = "";

    [Required(ErrorMessage = "条目名称不能为空")]
    [StringLength(100)]
    public string ItemName { get; set; } = "";

    [StringLength(100)]
    public string? ItemNameEn { get; set; }

    [StringLength(50)]
    public string? ParentItemCode { get; set; }

    [Range(0, int.MaxValue)]
    public int DispSeq { get; set; } = 99;

    [StringLength(500)]
    public string? ExtJson { get; set; }

    [Required]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    public string? Remark { get; set; }
}

public sealed class EDictItemManageListRowVm
{
    public int DataId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string? ItemNameEn { get; set; }
    public string? ParentItemCode { get; set; }
    public int DispSeq { get; set; }
    public string BStatusText { get; set; } = "";
    public bool IsSystem { get; set; }
    public DateTime AmendDate { get; set; }
}
