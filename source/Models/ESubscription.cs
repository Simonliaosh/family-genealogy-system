using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_Subscription")]
public class ESubscription
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("DutyID")]
    public int DutyId { get; set; }

    [Column("AppCode")]
    [StringLength(50)]
    public string? AppCode { get; set; }

    [Column("SubType")]
    [StringLength(20)]
    public string SubType { get; set; } = "EVENT";

    [Column("EventCode")]
    [StringLength(100)]
    public string? EventCode { get; set; }

    [Column("ResourceID")]
    [StringLength(100)]
    public string? ResourceId { get; set; }

    [Column("DeptID")]
    public int? DeptId { get; set; }

    [Column("IsPrimary")]
    public bool IsPrimary { get; set; } = true;

    [Column("FunctionLimit")]
    [StringLength(50)]
    public string? FunctionLimit { get; set; }

    [Column("ConditionExpr")]
    [StringLength(500)]
    public string? ConditionExpr { get; set; }

    [Column("NotifyMode")]
    [StringLength(100)]
    public string? NotifyMode { get; set; }

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
