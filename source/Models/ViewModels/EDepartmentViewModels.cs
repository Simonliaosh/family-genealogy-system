using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EDepartmentFormVm
{
    public int DataId { get; set; }

    [Required(ErrorMessage = "部门代码不能为空")]
    [StringLength(30, ErrorMessage = "部门代码最长30字符")]
    public string DeptCode { get; set; } = "";

    [Required(ErrorMessage = "部门中文名不能为空")]
    [StringLength(100, ErrorMessage = "部门中文名最长100字符")]
    public string DeptCName { get; set; } = "";

    [StringLength(100, ErrorMessage = "部门英文名最长100字符")]
    public string? DeptEName { get; set; }

    public int? ParentDeptId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "层级必须大于等于1")]
    public int DeptLevel { get; set; } = 1;

    [Range(0, int.MaxValue, ErrorMessage = "显示顺序必须大于等于0")]
    public int DispSeq { get; set; } = 99;

    [Required(ErrorMessage = "状态不能为空")]
    [StringLength(10)]
    public string BStatus { get; set; } = "1";

    public string? DDescription { get; set; }
    public string? Remark { get; set; }
}

public sealed class EDepartmentListRowVm
{
    public int DataId { get; set; }
    public string DeptCode { get; set; } = "";
    public string DeptCName { get; set; } = "";
    public int? ParentDeptId { get; set; }
    public string ParentDeptName { get; set; } = "-";
    public string BStatus { get; set; } = "";
}

