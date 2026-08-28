using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_TodoGroup")]
public class ETodoGroup
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("TenantID")]
    public int TenantId { get; set; }

    [Column("GroupCode")]
    [StringLength(100)]
    public string GroupCode { get; set; } = "";

    [Column("EventInstanceID")]
    public long? EventInstanceId { get; set; }

    [Column("AppCode")]
    [StringLength(50)]
    public string AppCode { get; set; } = "";

    [Column("EventCode")]
    [StringLength(100)]
    public string? EventCode { get; set; }

    [Column("ObjectType")]
    [StringLength(50)]
    public string ObjectType { get; set; } = "";

    [Column("ObjectKey")]
    [StringLength(100)]
    public string ObjectKey { get; set; } = "";

    [Column("HandleMode")]
    [StringLength(30)]
    public string HandleMode { get; set; } = "SINGLE";

    [Column("TotalCount")]
    public int TotalCount { get; set; }

    [Column("DoneCount")]
    public int DoneCount { get; set; }

    [Column("GroupStatus")]
    [StringLength(30)]
    public string GroupStatus { get; set; } = "PENDING";

    [Column("DueTime")]
    public DateTime? DueTime { get; set; }

    [Column("CreateTime")]
    public DateTime CreateTime { get; set; }

    [Column("UpdateTime")]
    public DateTime UpdateTime { get; set; }
}
