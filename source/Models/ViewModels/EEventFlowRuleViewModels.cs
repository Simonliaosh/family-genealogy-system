using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EEventFlowRuleFormVm
{
    public int DataId { get; set; }

    [Required(ErrorMessage = "规则编码不能为空")]
    [StringLength(100)]
    public string RuleCode { get; set; } = "";

    [Required(ErrorMessage = "规则名称不能为空")]
    [StringLength(100)]
    public string RuleName { get; set; } = "";

    [Required(ErrorMessage = "应用编码不能为空")]
    [StringLength(50)]
    public string AppCode { get; set; } = "FRAME";

    [Required(ErrorMessage = "请选择当前事件")]
    [StringLength(100)]
    public string CurrentEvent { get; set; } = "";

    [StringLength(100)]
    public string? NextEvent { get; set; }

    [StringLength(1000)]
    public string? ConditionExpr { get; set; }

    [Required(ErrorMessage = "动作类型不能为空")]
    [StringLength(30)]
    public string ActionType { get; set; } = "CREATE_TODO";

    [StringLength(50)]
    public string? TargetResolveType { get; set; }

    public int? TargetDutyId { get; set; }
    public int? TargetPosId { get; set; }
    public int? TargetDeptId { get; set; }
    public int? TargetUserId { get; set; }

    [Required(ErrorMessage = "处理模式不能为空")]
    [StringLength(30)]
    public string HandleMode { get; set; } = "SINGLE";

    [Range(0, 99999, ErrorMessage = "超时时限须≥0")]
    public int? TimeoutMinutes { get; set; }

    [StringLength(100)]
    public string? EscalateEventCode { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "显示顺序须≥0")]
    public int DispSeq { get; set; } = 99;

    [Required(ErrorMessage = "状态不能为空")]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    public string? Remark { get; set; }
}

public sealed class EEventFlowRuleListRowVm
{
    public int DataId { get; set; }
    public int DispSeq { get; set; }
    public string RuleCodeDisplay { get; set; } = "";
    public string RuleNameDisplay { get; set; } = "";
    public string AppCode { get; set; } = "";
    public string CurrentEventDisplay { get; set; } = "";
    public string NextEventDisplay { get; set; } = "";
    public string ActionTypeText { get; set; } = "";
    public string ConditionSummary { get; set; } = "";
    public string TargetSummary { get; set; } = "";
    public string BStatusText { get; set; } = "";
}
