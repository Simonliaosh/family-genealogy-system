using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EMenuGroupFormVm
{
    [Required(ErrorMessage = "应用编码不能为空")]
    [StringLength(50, ErrorMessage = "应用编码最长50字符")]
    public string AppCode { get; set; } = "FRAME";

    [Required(ErrorMessage = "菜单组编码不能为空")]
    [StringLength(50, ErrorMessage = "菜单组编码最长50字符")]
    [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "菜单组编码仅允许字母、数字、下划线、中横线")]
    public string MenuGroupCode { get; set; } = "";

    [Required(ErrorMessage = "菜单组名称不能为空")]
    [StringLength(100, ErrorMessage = "菜单组名称最长100字符")]
    public string MenuGroupName { get; set; } = "";

    [Range(0, int.MaxValue, ErrorMessage = "显示顺序必须大于等于0")]
    public int DispSeq { get; set; } = 99;

    [Required(ErrorMessage = "状态不能为空")]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    public string? Remark { get; set; }
}

public sealed class EMenuGroupListRowVm
{
    public string AppCode { get; set; } = "";
    public string MenuGroupCode { get; set; } = "";
    public string MenuGroupName { get; set; } = "";
    public int DispSeq { get; set; }
    public string BStatusText { get; set; } = "";
    public DateTime AmendDate { get; set; }
    public string OperatorName { get; set; } = "";
}
