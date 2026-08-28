using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EDutyFormVm
{
    public int DataId { get; set; }

    [Required(ErrorMessage = "职责代码不能为空")]
    [StringLength(30, ErrorMessage = "职责代码最长30字符")]
    public string DutyCode { get; set; } = "";

    [Required(ErrorMessage = "职责中文名不能为空")]
    [StringLength(100, ErrorMessage = "职责中文名最长100字符")]
    public string DutyCName { get; set; } = "";

    [StringLength(100, ErrorMessage = "职责英文名最长100字符")]
    public string? DutyEName { get; set; }

    [StringLength(50, ErrorMessage = "职责分类最长50字符")]
    public string? DutyCategory { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "显示顺序必须大于等于0")]
    public int DutyDispSeq { get; set; } = 99;

    [Required(ErrorMessage = "状态不能为空")]
    [RegularExpression("^(1|2)$", ErrorMessage = "状态须为启用或停用")]
    [StringLength(10)]
    public string BStatus { get; set; } = "1";

    public string? DDescription { get; set; }
    public string? DutyFlow { get; set; }
    public string? Remark { get; set; }
}

public sealed class EDutyListRowVm
{
    public int DataId { get; set; }
    public string DutyCode { get; set; } = "";
    public string DutyCName { get; set; } = "";
    public string DutyEName { get; set; } = "";
    public string BStatus { get; set; } = "";
}
