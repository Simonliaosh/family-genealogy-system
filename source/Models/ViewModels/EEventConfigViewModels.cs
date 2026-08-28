using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EEventConfigFormVm
{
    public int DataId { get; set; }

    [Required(ErrorMessage = "应用编码不能为空")]
    [StringLength(50)]
    public string AppCode { get; set; } = "FRAME";

    [Required(ErrorMessage = "事件编码不能为空")]
    [StringLength(100, ErrorMessage = "事件编码最长100字符")]
    public string EventCode { get; set; } = "";

    [Required(ErrorMessage = "事件名称不能为空")]
    [StringLength(100, ErrorMessage = "事件名称最长100字符")]
    public string EventName { get; set; } = "";

    [StringLength(300, ErrorMessage = "页面地址最长300字符")]
    public string? PageUrl { get; set; }

    [StringLength(50, ErrorMessage = "菜单组编码最长50字符")]
    public string? MenuGroupCode { get; set; }

    [Required(ErrorMessage = "执行类型不能为空")]
    [StringLength(20)]
    public string ExecType { get; set; } = "ASYNC";

    [StringLength(50, ErrorMessage = "事件分类最长50字符")]
    public string? EventType { get; set; }

    public bool IsGenerateTodo { get; set; } = true;

    [StringLength(200, ErrorMessage = "待办标题最长200字符")]
    public string? TodoTitle { get; set; }

    [Required(ErrorMessage = "处理模式不能为空")]
    [StringLength(30)]
    public string HandleMode { get; set; } = "SINGLE";

    [Range(0, 99999, ErrorMessage = "默认时限须≥0")]
    public int? DefaultDueMinutes { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "显示顺序必须大于等于0")]
    public int DispSeq { get; set; } = 99;

    [Required(ErrorMessage = "状态不能为空")]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    public string? Remark { get; set; }
}

public sealed class EEventConfigListRowVm
{
    public int DataId { get; set; }
    public string AppCode { get; set; } = "";
    public string EventCode { get; set; } = "";
    public string EventName { get; set; } = "";
    public string PageUrl { get; set; } = "-";
    public string ExecTypeText { get; set; } = "";
    public string EventTypeText { get; set; } = "-";
    public string IsGenerateTodoText { get; set; } = "";
    public string HandleModeText { get; set; } = "";
    public int DispSeq { get; set; }
    public string BStatusText { get; set; } = "";
}
