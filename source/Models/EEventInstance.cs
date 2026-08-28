using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_EventInstance")]
public class EEventInstance
{
    [Key]
    [Column("DataID")]
    public long DataId { get; set; }

    [Column("AppCode")]
    [StringLength(50)]
    public string AppCode { get; set; } = "";

    [Column("EventCode")]
    [StringLength(100)]
    public string EventCode { get; set; } = "";

    [Column("ObjectType")]
    [StringLength(50)]
    public string ObjectType { get; set; } = "";

    [Column("ObjectKey")]
    [StringLength(100)]
    public string ObjectKey { get; set; } = "";

    [Column("ObjectCode")]
    [StringLength(100)]
    public string? ObjectCode { get; set; }

    [Column("ObjectTitle")]
    [StringLength(300)]
    public string? ObjectTitle { get; set; }

    [Column("ObjectUrl")]
    [StringLength(500)]
    public string? ObjectUrl { get; set; }

    [Column("TriggerUserID")]
    public int? TriggerUserId { get; set; }

    [Column("TriggerDeptID")]
    public int? TriggerDeptId { get; set; }

    [Column("TriggerPosID")]
    public int? TriggerPosId { get; set; }

    [Column("PayloadJson")]
    public string? PayloadJson { get; set; }

    [Column("IdempotencyKey")]
    [StringLength(200)]
    public string? IdempotencyKey { get; set; }

    [Column("EventStatus")]
    [StringLength(30)]
    public string EventStatus { get; set; } = "NEW";

    [Column("OccurredTime")]
    public DateTime OccurredTime { get; set; }

    [Column("ProcessTime")]
    public DateTime? ProcessTime { get; set; }

    [Column("LastError")]
    public string? LastError { get; set; }

    [Column("RetryCount")]
    public int RetryCount { get; set; }

    [Column("CreateTime")]
    public DateTime CreateTime { get; set; }
}
