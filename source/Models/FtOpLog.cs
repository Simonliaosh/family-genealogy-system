using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("FamilyTree_OpLog")]
public class FtOpLog
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("OpType")]
    [StringLength(32)]
    public string OpType { get; set; } = "";

    [Column("ObjectType")]
    [StringLength(32)]
    public string ObjectType { get; set; } = "";

    [Column("ObjectKey")]
    [StringLength(64)]
    public string ObjectKey { get; set; } = "";

    [Column("BeforeJson")]
    public string? BeforeJson { get; set; }

    [Column("AfterJson")]
    public string? AfterJson { get; set; }

    [Column("OpUserId")]
    public int OpUserId { get; set; }

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
