using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_MenuGroup")]
public class EMenuGroup
{
    [Key]
    [Column("MenuGroupCode")]
    [StringLength(50)]
    public string MenuGroupCode { get; set; } = "";

    [Column("AppCode")]
    [StringLength(50)]
    public string AppCode { get; set; } = "FRAME";

    [Column("MenuGroupName")]
    [StringLength(100)]
    public string MenuGroupName { get; set; } = "";

    [Column("Icon")]
    [StringLength(100)]
    public string? Icon { get; set; }

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
