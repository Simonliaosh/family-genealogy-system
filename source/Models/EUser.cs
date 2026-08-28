using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_Users")]
public class EUser
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("LoginId")]
    [StringLength(50)]
    public string LoginId { get; set; } = "";

    [Column("RealName")]
    [StringLength(50)]
    public string RealName { get; set; } = "";

    [Column("PwdHash")]
    [StringLength(200)]
    public string PwdHash { get; set; } = "";

    [Column("PwdSalt")]
    [StringLength(100)]
    public string? PwdSalt { get; set; }

    [Column("PasswordAlgo")]
    [StringLength(30)]
    public string PasswordAlgo { get; set; } = "MD5_16";

    [Column("PasswordVersion")]
    public int PasswordVersion { get; set; } = 1;

    [Column("UserType")]
    [StringLength(30)]
    public string UserType { get; set; } = "EMPLOYEE";

    [Column("LoginCount")]
    public int LoginCount { get; set; }

    [Column("MaxLoginCount")]
    public int MaxLoginCount { get; set; }

    [Column("PwdErrorCount")]
    public int PwdErrorCount { get; set; }

    [Column("MaxPwdErrorCount")]
    public int MaxPwdErrorCount { get; set; }

    [Column("IsLocked")]
    public bool IsLocked { get; set; }

    [Column("IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    [Column("LastLoginTime")]
    public DateTime? LastLoginTime { get; set; }

    [Column("LastPwdErrorTime")]
    public DateTime? LastPwdErrorTime { get; set; }

    [Column("LastPwdChangedTime")]
    public DateTime? LastPwdChangedTime { get; set; }

    [Column("ExternalRefType")]
    [StringLength(30)]
    public string? ExternalRefType { get; set; }

    [Column("ExternalRefID")]
    [StringLength(100)]
    public string? ExternalRefId { get; set; }

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

    [Column("Operator")]
    [StringLength(30)]
    public string? OperatorName { get; set; }

    [NotMapped]
    public string? ShareHolderCode { get; set; }
}
