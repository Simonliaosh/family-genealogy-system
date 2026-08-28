using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class ESubscriptionFormVm
{
    public int DataId { get; set; }

    [Required(ErrorMessage = "请选择职责")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择职责")]
    public int DutyId { get; set; }

    [Required(ErrorMessage = "应用编码不能为空")]
    [StringLength(50, ErrorMessage = "应用编码最长50字符")]
    public string AppCode { get; set; } = "FRAME";

    [Required(ErrorMessage = "订阅类型不能为空")]
    [StringLength(20)]
    public string SubType { get; set; } = "EVENT";

    [StringLength(100, ErrorMessage = "事件编码最长100字符")]
    public string? EventCode { get; set; }

    [StringLength(100, ErrorMessage = "资源编码最长100字符")]
    public string? ResourceId { get; set; }

    public int? DeptId { get; set; }

    public bool IsPrimary { get; set; } = true;

    [StringLength(50, ErrorMessage = "功能限制串最长50字符")]
    public string? FunctionLimit { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "显示顺序必须大于等于0")]
    public int DispSeq { get; set; } = 99;

    [Required(ErrorMessage = "状态不能为空")]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    public string? Remark { get; set; }
}

public sealed class ESubscriptionListRowVm
{
    public int DataId { get; set; }
    public int DutyId { get; set; }
    public string DutyDisplay { get; set; } = "-";
    public string AppCode { get; set; } = "";
    public string SubType { get; set; } = "";
    public string SubTypeText { get; set; } = "";
    public string EventCode { get; set; } = "-";
    public string ResourceId { get; set; } = "-";
    public int? DeptId { get; set; }
    public string DeptDisplay { get; set; } = "-";
    public string BStatusText { get; set; } = "";
}
