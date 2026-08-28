using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EUserHandoverFormVm
{
    [Required(ErrorMessage = "请选择交出方")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择交出方")]
    public int SourceUserId { get; set; }

    [Required(ErrorMessage = "请选择接收方")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择接收方")]
    public int TargetUserId { get; set; }

    [Required(ErrorMessage = "请选择交接类型")]
    [StringLength(30)]
    public string HandoverType { get; set; } = "";

    public string? Remark { get; set; }
}

public sealed class EUserHandoverListRowVm
{
    public int DataId { get; set; }
    public DateTime HandoverTime { get; set; }
    public string SourceDisplay { get; set; } = "";
    public string TargetDisplay { get; set; } = "";
    public string HandoverTypeText { get; set; } = "";
    public string OperatorDisplay { get; set; } = "";
    public string RemarkShort { get; set; } = "";
}

public sealed class EUserHandoverDetailVm
{
    public int DataId { get; set; }
    public DateTime HandoverTime { get; set; }
    public int SourceUserId { get; set; }
    public string SourceDisplay { get; set; } = "";
    public int TargetUserId { get; set; }
    public string TargetDisplay { get; set; } = "";
    public string HandoverType { get; set; } = "";
    public string HandoverTypeText { get; set; } = "";
    public int OperatorUserId { get; set; }
    public string OperatorDisplay { get; set; } = "";
    public string? Remark { get; set; }
}
