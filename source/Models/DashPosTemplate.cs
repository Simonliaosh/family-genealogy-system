using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_Dash_PosTemplate")]
public class DashPosTemplate
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("PosID")]
    public int PosId { get; set; }

    [Column("IndicatorID")]
    public int IndicatorId { get; set; }

    [Column("LayoutRow")]
    public int LayoutRow { get; set; }

    [Column("LayoutCol")]
    public int LayoutCol { get; set; }

    [Column("ColSpan")]
    public byte ColSpan { get; set; } = 2;

    [Column("IsLock")]
    public bool IsLock { get; set; }

    [Column("DefaultFilterJson")]
    public string? DefaultFilterJson { get; set; }

    [Column("CardTitle")]
    [StringLength(100)]
    public string CardTitle { get; set; } = "";

    [Column("DispSeq")]
    public int DispSeq { get; set; } = 99;

    [Column("BStatus")]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("CreateUserID")]
    public int? CreateUserId { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("AmendUserID")]
    public int? AmendUserId { get; set; }

    [Column("Operator")]
    [StringLength(30)]
    public string? OperatorName { get; set; }

    [Column("RowVersion")]
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}
