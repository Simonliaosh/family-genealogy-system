using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EPositionFormVm
{
    public int DataId { get; set; }

    [Required(ErrorMessage = "岗位代码不能为空")]
    [StringLength(30, ErrorMessage = "岗位代码最长30字符")]
    public string PostCode { get; set; } = "";

    [Required(ErrorMessage = "岗位中文名不能为空")]
    [StringLength(100, ErrorMessage = "岗位中文名最长100字符")]
    public string PostCName { get; set; } = "";

    [StringLength(100, ErrorMessage = "岗位英文名最长100字符")]
    public string? PostEName { get; set; }

    [StringLength(30, ErrorMessage = "岗位类型最长30字符")]
    public string? PositionType { get; set; }

    [Required(ErrorMessage = "数据范围不能为空")]
    [StringLength(30)]
    public string DataScope { get; set; } = "SELF";

    [Range(0, int.MaxValue, ErrorMessage = "显示顺序必须大于等于0")]
    public int DispSeq { get; set; } = 99;

    [Required(ErrorMessage = "状态不能为空")]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    public string? DDescription { get; set; }
    public string? Remark { get; set; }
}

public sealed class EPositionListRowVm
{
    public int DataId { get; set; }
    public string PostCode { get; set; } = "";
    public string PostCName { get; set; } = "";
    public string DataScopeText { get; set; } = "";
    public int DispSeq { get; set; }
    public string BStatus { get; set; } = "";
}
