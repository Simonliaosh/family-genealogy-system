using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EUserPositionFormVm
{
    public int DataId { get; set; }

    [Required(ErrorMessage = "请选择用户")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择用户")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "请选择部门")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择部门")]
    public int DeptId { get; set; }

    [Required(ErrorMessage = "请选择岗位")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择岗位")]
    public int PosId { get; set; }

    [Required(ErrorMessage = "状态不能为空")]
    [StringLength(10)]
    public string BStatus { get; set; } = "1";
}

public sealed class EUserPositionListRowVm
{
    public int DataId { get; set; }
    public int UserId { get; set; }
    public string UserDisplay { get; set; } = "-";
    public int DeptId { get; set; }
    public string DeptDisplay { get; set; } = "-";
    public int PosId { get; set; }
    public string PosDisplay { get; set; } = "-";
    public string BStatus { get; set; } = "";
}
