using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("FamilyTree_BranchAdminApply")]
public class FtBranchAdminApply
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("ApplyUserId")]
    public int ApplyUserId { get; set; }

    [Column("ApplyReason")]
    [StringLength(512)]
    public string? ApplyReason { get; set; }

    [Column("ApplyStatus")]
    [StringLength(16)]
    public string ApplyStatus { get; set; } = "PENDING";

    [Column("AuditUserId")]
    public int? AuditUserId { get; set; }

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
