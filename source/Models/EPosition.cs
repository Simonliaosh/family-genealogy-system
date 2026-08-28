using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_Position")]
public class EPosition
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("PostCode")]
    [StringLength(30)]
    public string PostCode { get; set; } = "";

    [Column("PostCName")]
    [StringLength(100)]
    public string PostCName { get; set; } = "";

    [Column("PostEName")]
    [StringLength(100)]
    public string? PostEName { get; set; }

    [Column("PositionType")]
    [StringLength(30)]
    public string? PositionType { get; set; }

    [Column("DataScope")]
    [StringLength(30)]
    public string DataScope { get; set; } = "SELF";

    [Column("DispSeq")]
    public int DispSeq { get; set; } = 99;

    [Column("BStatus")]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("DDescription")]
    public string? DDescription { get; set; }

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
