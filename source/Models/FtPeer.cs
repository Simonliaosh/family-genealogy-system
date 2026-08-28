using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("FamilyTree_PeerInvite")]
public class FtPeerInvite
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("InviteCode")]
    [StringLength(32)]
    public string InviteCode { get; set; } = "";

    [Column("LocalPersonId")]
    public int LocalPersonId { get; set; }

    [Column("SiteId")]
    [StringLength(36)]
    public string SiteId { get; set; } = "";

    [Column("ExpireDate")]
    public DateTime ExpireDate { get; set; }

    [Column("InviteStatus")]
    [StringLength(16)]
    public string InviteStatus { get; set; } = "OPEN";

    [Column("CreateUserId")]
    public int CreateUserId { get; set; }

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

[Table("FamilyTree_PeerBridge")]
public class FtPeerBridge
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("LocalPersonId")]
    public int LocalPersonId { get; set; }

    [Column("PeerBaseUrl")]
    [StringLength(256)]
    public string PeerBaseUrl { get; set; } = "";

    [Column("PeerSiteId")]
    [StringLength(36)]
    public string PeerSiteId { get; set; } = "";

    [Column("PeerPersonId")]
    public int PeerPersonId { get; set; }

    [Column("PeerLabel")]
    [StringLength(64)]
    public string? PeerLabel { get; set; }

    [Column("BridgeStatus")]
    [StringLength(16)]
    public string BridgeStatus { get; set; } = "ACTIVE";

    [Column("OutTokenHash")]
    [StringLength(64)]
    public string OutTokenHash { get; set; } = "";

    [Column("InToken")]
    [StringLength(64)]
    public string InToken { get; set; } = "";

    [Column("InviteCode")]
    [StringLength(32)]
    public string? InviteCode { get; set; }

    [Column("CreateUserId")]
    public int CreateUserId { get; set; }

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
