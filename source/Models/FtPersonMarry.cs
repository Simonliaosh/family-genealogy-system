using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("FamilyTree_PersonMarry")]
public class FtPersonMarry
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("PersonId")]
    public int PersonId { get; set; }

    [Column("SpouseName")]
    [StringLength(64)]
    public string? SpouseName { get; set; }

    [Column("SpouseBirth")]
    [StringLength(32)]
    public string? SpouseBirth { get; set; }

    [Column("MarryType")]
    [StringLength(32)]
    public string MarryType { get; set; } = "原配";

    [Column("HouseSeq")]
    public int HouseSeq { get; set; } = 99;

    [Column("SpousePersonId")]
    public int? SpousePersonId { get; set; }

    [Column("BStatus")]
    [StringLength(1)]
    public string BStatus { get; set; } = "1";

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("Remark")]
    [StringLength(512)]
    public string? Remark { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("Operator")]
    [StringLength(30)]
    public string OperatorName { get; set; } = "";
}
