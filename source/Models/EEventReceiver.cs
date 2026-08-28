using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_EventReceiver")]
public class EEventReceiver
{
    [Key]
    [Column("DataID")]
    public long DataId { get; set; }

    [Column("EventInstanceID")]
    public long EventInstanceId { get; set; }

    [Column("ReceiverUserID")]
    public int ReceiverUserId { get; set; }

    [Column("ReceiverMemberID")]
    public int? ReceiverMemberId { get; set; }

    [Column("ReceiverDeptID")]
    public int? ReceiverDeptId { get; set; }

    [Column("ReceiverPosID")]
    public int? ReceiverPosId { get; set; }

    [Column("ReceiverDutyID")]
    public int? ReceiverDutyId { get; set; }

    [Column("SubscriptionID")]
    public int? SubscriptionId { get; set; }

    [Column("ResolveType")]
    [StringLength(50)]
    public string ResolveType { get; set; } = "DUTY";

    [Column("ResolveReason")]
    [StringLength(500)]
    public string? ResolveReason { get; set; }

    [Column("CreateTime")]
    public DateTime CreateTime { get; set; }
}
