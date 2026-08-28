using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

/// <summary>职责主数据（Tbl_E_Duty）。</summary>
[Table("Tbl_E_Duty")]
public class EEDuty
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("DutyCode")]
    [StringLength(30)]
    public string DutyCode { get; set; } = "";

    [Column("DutyCName")]
    [StringLength(100)]
    public string DutyCName { get; set; } = "";

    [Column("DutyEName")]
    [StringLength(100)]
    public string? DutyEName { get; set; }

    [Column("DutyCategory")]
    [StringLength(50)]
    public string? DutyCategory { get; set; }

    [Column("DutyDispSeq")]
    public int DutyDispSeq { get; set; } = 99;

    [Column("DDescription")]
    public string? DDescription { get; set; }

    [Column("DutyFlow")]
    public string? DutyFlow { get; set; }

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
