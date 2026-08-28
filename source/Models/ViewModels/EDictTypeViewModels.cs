using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EDictTypeFormVm
{
    [Required(ErrorMessage = "应用编码不能为空")]
    [StringLength(50)]
    public string AppCode { get; set; } = "FRAME";

    [Required(ErrorMessage = "字典类型编码不能为空")]
    [StringLength(50)]
    [RegularExpression(@"^[A-Za-z0-9_.]+$", ErrorMessage = "类型编码仅允许字母、数字、下划线、点")]
    public string DictTypeCode { get; set; } = "";

    [Required(ErrorMessage = "字典类型名称不能为空")]
    [StringLength(100)]
    public string DictTypeName { get; set; } = "";

    public bool IsEditable { get; set; } = true;

    [Required]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    public string? Remark { get; set; }
}

public sealed class EDictTypeManageListRowVm
{
    public string DictTypeCode { get; set; } = "";
    public string DictTypeName { get; set; } = "";
    public string AppCode { get; set; } = "";
    public string BStatusText { get; set; } = "";
    public bool IsSystem { get; set; }
    public bool IsEditable { get; set; }
    public int ItemCount { get; set; }
    public DateTime AmendDate { get; set; }
}
