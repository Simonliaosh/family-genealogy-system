using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_Dash_Indicator")]
public class DashIndicator
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("IndicatorCode")]
    [StringLength(50)]
    public string IndicatorCode { get; set; } = "";

    [Column("IndicatorName")]
    [StringLength(100)]
    public string IndicatorName { get; set; } = "";

    [Column("AppCode")]
    [StringLength(50)]
    public string AppCode { get; set; } = "FRAME";

    [Column("ChartType")]
    public byte ChartType { get; set; }

    [Column("DataSource")]
    [StringLength(100)]
    public string DataSource { get; set; } = "";

    [Column("CalcRule")]
    public string CalcRule { get; set; } = "";

    [Column("DefaultTimeScope")]
    public byte DefaultTimeScope { get; set; } = 3;

    [Column("IsLockCalc")]
    public bool IsLockCalc { get; set; } = true;

    [Column("DispSeq")]
    public int DispSeq { get; set; } = 99;

    [Column("Remark")]
    [StringLength(500)]
    public string? Remark { get; set; }

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
