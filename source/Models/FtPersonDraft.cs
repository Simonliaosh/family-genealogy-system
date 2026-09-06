using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("FamilyTree_PersonDraft")]
public class FtPersonDraft
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("BranchId")]
    public int? BranchId { get; set; }

    [Column("SubmitUserId")]
    public int SubmitUserId { get; set; }

    [Column("FullName")]
    [StringLength(64)]
    public string FullName { get; set; } = "";

    [Column("FatherName")]
    [StringLength(64)]
    public string FatherName { get; set; } = "";

    [Column("MotherName")]
    [StringLength(64)]
    public string? MotherName { get; set; }

    [Column("BirthDate")]
    [StringLength(32)]
    public string? BirthDate { get; set; }

    [Column("RelationType")]
    [StringLength(16)]
    public string RelationType { get; set; } = "SELF";

    [Column("DraftJson")]
    public string DraftJson { get; set; } = "{}";

    [Column("AuditStatus")]
    [StringLength(16)]
    public string AuditStatus { get; set; } = "DRAFT";

    [Column("ResultPersonId")]
    public int? ResultPersonId { get; set; }

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
