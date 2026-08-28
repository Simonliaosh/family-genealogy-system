using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

/// <summary>事件流转规则（Tbl_E_EventFlowRule，v1）。</summary>
[Table("Tbl_E_EventFlowRule")]
public class EEventFlowRule
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("RuleCode")]
    [StringLength(100)]
    public string RuleCode { get; set; } = "";

    [Column("RuleName")]
    [StringLength(100)]
    public string RuleName { get; set; } = "";

    [Column("AppCode")]
    [StringLength(50)]
    public string AppCode { get; set; } = "FRAME";

    [Column("CurrentEvent")]
    [StringLength(100)]
    public string CurrentEvent { get; set; } = "";

    [Column("NextEvent")]
    [StringLength(100)]
    public string? NextEvent { get; set; }

    [Column("ConditionExpr")]
    [StringLength(1000)]
    public string? ConditionExpr { get; set; }

    [Column("ActionType")]
    [StringLength(30)]
    public string ActionType { get; set; } = "CREATE_TODO";

    [Column("TargetResolveType")]
    [StringLength(50)]
    public string? TargetResolveType { get; set; }

    [Column("TargetDutyID")]
    public int? TargetDutyId { get; set; }

    [Column("TargetPosID")]
    public int? TargetPosId { get; set; }

    [Column("TargetDeptID")]
    public int? TargetDeptId { get; set; }

    [Column("TargetUserID")]
    public int? TargetUserId { get; set; }

    [Column("HandleMode")]
    [StringLength(30)]
    public string HandleMode { get; set; } = "SINGLE";

    [Column("TimeoutMinutes")]
    public int? TimeoutMinutes { get; set; }

    [Column("EscalateEventCode")]
    [StringLength(100)]
    public string? EscalateEventCode { get; set; }

    [Column("DispSeq")]
    public int DispSeq { get; set; } = 99;

    [Column("BStatus")]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("Remark")]
    public string? Remark { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("Operator")]
    [StringLength(30)]
    public string? OperatorName { get; set; }
}
