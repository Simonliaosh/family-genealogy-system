using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_DictType")]
public class EDictType
{
    [Key]
    [Column("DictTypeCode")]
    [StringLength(50)]
    public string DictTypeCode { get; set; } = "";

    [Column("DictTypeName")]
    [StringLength(100)]
    public string DictTypeName { get; set; } = "";

    [Column("AppCode")]
    [StringLength(50)]
    public string AppCode { get; set; } = "FRAME";

    [Column("IsSystem")]
    public bool IsSystem { get; set; }

    [Column("IsEditable")]
    public bool IsEditable { get; set; } = true;

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
