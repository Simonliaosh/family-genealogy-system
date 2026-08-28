using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("FamilyTree_Person")]
public class FtPerson
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("BranchId")]
    public int? BranchId { get; set; }

    /// <summary>所属家族；同家族内可见，一人一家。</summary>
    [Column("ClanId")]
    public int? ClanId { get; set; }

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

    [Column("BirthYear")]
    public int? BirthYear { get; set; }

    [Column("FatherPersonId")]
    public int? FatherPersonId { get; set; }

    [Column("MotherPersonId")]
    public int? MotherPersonId { get; set; }

    [Column("InMainGenealogy")]
    public bool InMainGenealogy { get; set; }

    [Column("SameAsPersonId")]
    public int? SameAsPersonId { get; set; }

    [Column("DeathInfo")]
    [StringLength(128)]
    public string? DeathInfo { get; set; }

    [Column("NickName")]
    [StringLength(128)]
    public string? NickName { get; set; }

    [Column("SelfIntro")]
    public string? SelfIntro { get; set; }

    [Column("WechatId")]
    [StringLength(128)]
    public string? WechatId { get; set; }

    [Column("PhotoListJson")]
    public string? PhotoListJson { get; set; }

    [Column("Gender")]
    public byte Gender { get; set; } = 1;

    [Column("GenerationNo")]
    public int GenerationNo { get; set; }

    [Column("WordOfGeneration")]
    [StringLength(32)]
    public string? WordOfGeneration { get; set; }

    [Column("OwnerUserId")]
    public int OwnerUserId { get; set; }

    [Column("BindUserId")]
    public int? BindUserId { get; set; }

    [Column("EditLock")]
    public bool EditLock { get; set; }

    [Column("LinkLock")]
    public bool LinkLock { get; set; }

    [Column("PrivacyLevel")]
    public byte PrivacyLevel { get; set; } = 1;

    [Column("ShowPhoto")]
    public bool ShowPhoto { get; set; }

    [Column("ShowWechat")]
    public bool ShowWechat { get; set; }

    [Column("ShowSelfIntro")]
    public bool ShowSelfIntro { get; set; }

    [Column("ShowBirthDetail")]
    public bool ShowBirthDetail { get; set; }

    [Column("ShowResume")]
    public bool ShowResume { get; set; }

    [Column("PrintAllow")]
    public bool PrintAllow { get; set; } = true;

    [Column("IsDead")]
    public bool IsDead { get; set; }

    [Column("IsCertified")]
    public bool IsCertified { get; set; }

    [Column("CertCode")]
    [StringLength(16)]
    public string? CertCode { get; set; }

    [Column("AuditStatus")]
    [StringLength(16)]
    public string AuditStatus { get; set; } = "PASS";

    [Column("KeyLocked")]
    public bool KeyLocked { get; set; }

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
