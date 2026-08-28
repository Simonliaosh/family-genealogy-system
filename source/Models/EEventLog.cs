using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

/// <summary>事件处理日志（Tbl_E_EventLog，v1）。</summary>
[Table("Tbl_E_EventLog")]
public class EEventLog
{
    [Key]
    [Column("DataID")]
    public long DataId { get; set; }

    [Column("EventInstanceID")]
    public long? EventInstanceId { get; set; }

    [Column("AppCode")]
    [StringLength(50)]
    public string AppCode { get; set; } = "FRAME";

    [Column("EventCode")]
    [StringLength(100)]
    public string EventCode { get; set; } = "";

    [Column("ObjectType")]
    [StringLength(50)]
    public string? ObjectType { get; set; }

    [Column("ObjectKey")]
    [StringLength(100)]
    public string? ObjectKey { get; set; }

    [Column("UserID")]
    public int? UserId { get; set; }

    [Column("ActionType")]
    [StringLength(50)]
    public string ActionType { get; set; } = "";

    [Column("HandleResult")]
    [StringLength(50)]
    public string HandleResult { get; set; } = "";

    [Column("Remark")]
    public string? Remark { get; set; }

    [Column("CreateTime")]
    public DateTime CreateTime { get; set; }

    [NotMapped]
    public int BusinessId => int.TryParse(ObjectKey, out var id) ? id : 0;
}
