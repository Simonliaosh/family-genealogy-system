using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("FamilyTree_Clan")]
public class FtClan
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("ClanCode")]
    [StringLength(16)]
    public string ClanCode { get; set; } = "";

    [Column("ClanName")]
    [StringLength(64)]
    public string ClanName { get; set; } = "";

    [Column("OwnerUserId")]
    public int OwnerUserId { get; set; }

    [Column("Remark")]
    [StringLength(256)]
    public string? Remark { get; set; }

    [Column("BStatus")]
    [StringLength(1)]
    public string BStatus { get; set; } = "1";

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("OperatorName")]
    [StringLength(30)]
    public string OperatorName { get; set; } = "";
}

/// <summary>邀请创建新家族链（超管/族谱管理员发起；被邀请人凭码建族）。</summary>
[Table("FamilyTree_ClanCreateInvite")]
public class FtClanCreateInvite
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("InviteCode")]
    [StringLength(16)]
    public string InviteCode { get; set; } = "";

    [Column("CreatedByUserId")]
    public int CreatedByUserId { get; set; }

    [Column("ExpireAt")]
    public DateTime ExpireAt { get; set; }

    [Column("InviteStatus")]
    [StringLength(16)]
    public string InviteStatus { get; set; } = "OPEN";

    [Column("UsedByUserId")]
    public int? UsedByUserId { get; set; }

    [Column("UsedClanId")]
    public int? UsedClanId { get; set; }

    [Column("Remark")]
    [StringLength(128)]
    public string? Remark { get; set; }

    [Column("BStatus")]
    [StringLength(1)]
    public string BStatus { get; set; } = "1";

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("OperatorName")]
    [StringLength(30)]
    public string OperatorName { get; set; } = "";
}

[Table("FamilyTree_UserClan")]
public class FtUserClan
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("UserId")]
    public int UserId { get; set; }

    [Column("ClanId")]
    public int ClanId { get; set; }

    [Column("JoinDate")]
    public DateTime JoinDate { get; set; }

    [Column("BStatus")]
    [StringLength(1)]
    public string BStatus { get; set; } = "1";

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("OperatorName")]
    [StringLength(30)]
    public string OperatorName { get; set; } = "";
}
