using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EResourceFormVm
{
    public int DataId { get; set; }

    [Required(ErrorMessage = "应用编码不能为空")]
    [StringLength(50, ErrorMessage = "应用编码最长50字符")]
    public string AppCode { get; set; } = "FRAME";

    [Required(ErrorMessage = "资源编码不能为空")]
    [StringLength(100, ErrorMessage = "资源编码最长100字符")]
    public string ResourceId { get; set; } = "";

    [Required(ErrorMessage = "资源名称不能为空")]
    [StringLength(100, ErrorMessage = "资源名称最长100字符")]
    public string ResourceName { get; set; } = "";

    [StringLength(30, ErrorMessage = "资源类型最长30字符")]
    public string? ResourceType { get; set; }

    [StringLength(300, ErrorMessage = "菜单路径最长300字符")]
    public string? MenuPath { get; set; }

    [StringLength(50, ErrorMessage = "菜单组编码最长50字符")]
    public string? MenuGroupCode { get; set; }

    [Required(ErrorMessage = "状态不能为空")]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    [StringLength(4000, ErrorMessage = "备注过长")]
    public string? Remark { get; set; }
}

public sealed class EResourceListRowVm
{
    public int DataId { get; set; }
    public string AppCode { get; set; } = "";
    public string ResourceId { get; set; } = "";
    public string ResourceName { get; set; } = "";
    public string ResourceTypeText { get; set; } = "-";
    public string MenuPath { get; set; } = "-";
    public string BStatusText { get; set; } = "";
}
