using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_Dash_PosIndicatorPerm")]
public class DashPosIndicatorPerm
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("PosID")]
    public int PosId { get; set; }

    [Column("IndicatorID")]
    public int IndicatorId { get; set; }

    [Column("BStatus")]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("Operator")]
    [StringLength(30)]
    public string? OperatorName { get; set; }
}
