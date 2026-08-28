using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_AppModule")]
public class EAppModule
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("AppCode")]
    [StringLength(50)]
    public string AppCode { get; set; } = "";

    [Column("AppName")]
    [StringLength(100)]
    public string AppName { get; set; } = "";

    [Column("AppType")]
    [StringLength(30)]
    public string AppType { get; set; } = "BUSINESS";

    [Column("BaseUrl")]
    [StringLength(300)]
    public string? BaseUrl { get; set; }

    [Column("Icon")]
    [StringLength(100)]
    public string? Icon { get; set; }

    [Column("DispSeq")]
    public int DispSeq { get; set; } = 99;

    [Column("Remark")]
    public string? Remark { get; set; }

    [Column("BStatus")]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("Operator")]
    [StringLength(30)]
    public string? OperatorName { get; set; }
}
