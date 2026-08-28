using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EPositionDutyFormVm
{
    public int DataId { get; set; }

    [Required(ErrorMessage = "请选择岗位")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择岗位")]
    public int PosId { get; set; }

    [Required(ErrorMessage = "请选择职责")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择职责")]
    public int DutyId { get; set; }

    public int? DeptId { get; set; }

    [StringLength(50, ErrorMessage = "业务限制最长50字符")]
    public string? BusinessLimit { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "显示顺序必须大于等于0")]
    public int DispSeq { get; set; } = 99;

    [Required(ErrorMessage = "状态不能为空")]
    [StringLength(10)]
    public string BStatus { get; set; } = "1";

    public string? Remark { get; set; }
}

public sealed class EPositionDutyListRowVm
{
    public int DataId { get; set; }
    public int PosId { get; set; }
    public string PosDisplay { get; set; } = "-";
    public int DutyId { get; set; }
    public string DutyDisplay { get; set; } = "-";
    public int? DeptId { get; set; }
    public string DeptDisplay { get; set; } = "-";
    public string BusinessLimit { get; set; } = "-";
    public int DispSeq { get; set; }
    public string BStatus { get; set; } = "";
    public string Remark { get; set; } = "-";
}
