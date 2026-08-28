using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_DictItem")]
public class EDictItem
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("DictTypeCode")]
    [StringLength(50)]
    public string DictTypeCode { get; set; } = "";

    [Column("ItemCode")]
    [StringLength(50)]
    public string ItemCode { get; set; } = "";

    [Column("ItemName")]
    [StringLength(100)]
    public string ItemName { get; set; } = "";

    [Column("ItemNameEn")]
    [StringLength(100)]
    public string? ItemNameEn { get; set; }

    [Column("ParentItemCode")]
    [StringLength(50)]
    public string? ParentItemCode { get; set; }

    [Column("DispSeq")]
    public int DispSeq { get; set; } = 99;

    [Column("ExtJson")]
    [StringLength(500)]
    public string? ExtJson { get; set; }

    [Column("IsSystem")]
    public bool IsSystem { get; set; }

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
}
