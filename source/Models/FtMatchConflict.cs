using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("FamilyTree_MatchConflict")]
public class FtMatchConflict
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("SourcePersonId")]
    public int? SourcePersonId { get; set; }

    [Column("TargetPersonId")]
    public int? TargetPersonId { get; set; }

    [Column("ConflictType")]
    [StringLength(32)]
    public string ConflictType { get; set; } = "";

    [Column("ConflictDetail")]
    public string? ConflictDetail { get; set; }

    [Column("ResolveStatus")]
    [StringLength(16)]
    public string ResolveStatus { get; set; } = "OPEN";

    [Column("ResolveUserId")]
    public int? ResolveUserId { get; set; }

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
