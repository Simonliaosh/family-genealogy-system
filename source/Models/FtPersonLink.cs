using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("FamilyTree_PersonLink")]
public class FtPersonLink
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("SourcePersonId")]
    public int SourcePersonId { get; set; }

    [Column("TargetMainPersonId")]
    public int TargetMainPersonId { get; set; }

    [Column("ApplyUserId")]
    public int ApplyUserId { get; set; }

    [Column("AuditSuperAdminId")]
    public int? AuditSuperAdminId { get; set; }

    [Column("LinkStatus")]
    [StringLength(16)]
    public string LinkStatus { get; set; } = "PENDING";

    [Column("MatchLevel")]
    public byte? MatchLevel { get; set; }

    [Column("UnlinkTime")]
    public DateTime? UnlinkTime { get; set; }

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

    /// <summary>
    /// 乐观并发令牌。没有它 EF 发的是无条件 UPDATE ... WHERE DataID=@id，后写静默覆盖先写。
    /// 对应 scripts/29-CreateTbl_FamilyTree_Core.sql 里的 rowversion 列。
    /// </summary>
    [Timestamp]
    [Column("RowVersion")]
    public byte[]? RowVersion { get; set; }
}
