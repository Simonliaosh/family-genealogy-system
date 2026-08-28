using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_PositionDuty")]
public class EPositionDuty
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("PosID")]
    public int PosId { get; set; }

    [Column("DutyID")]
    public int DutyId { get; set; }

    [Column("DeptID")]
    public int? DeptId { get; set; }

    [Column("BusinessLimit")]
    [StringLength(50)]
    public string? BusinessLimit { get; set; }

    [Column("DispSeq")]
    public int DispSeq { get; set; } = 99;

    [Column("BStatus")]
    [StringLength(10)]
    public string BStatus { get; set; } = "启用";

    [Column("Remark")]
    public string? Remark { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("Operator")]
    [StringLength(8)]
    public string? OperatorName { get; set; }
}
