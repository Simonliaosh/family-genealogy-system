using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

/// <summary>待办任务（Tbl_E_TodoTask，v1 + 可选 v2 分组字段）。</summary>
[Table("Tbl_E_TodoTask")]
public class ETodoTask
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("EventInstanceID")]
    public long? EventInstanceId { get; set; }

    [Column("EventLogID")]
    public long? EventLogId { get; set; }

    [Column("AppCode")]
    [StringLength(50)]
    public string AppCode { get; set; } = "FRAME";

    [Column("EventCode")]
    [StringLength(100)]
    public string? EventCode { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

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

    [Column("Status")]
    public byte Status { get; set; }

    [Column("TodoTitle")]
    [StringLength(200)]
    public string TodoTitle { get; set; } = "";

    [Column("TodoContent")]
    public string? TodoContent { get; set; }

    [Column("HandlerDeptID")]
    public int? HandlerDeptId { get; set; }

    [Column("HandlerPosID")]
    public int? HandlerPosId { get; set; }

    [Column("DueTime")]
    public DateTime? DueTime { get; set; }

    [Column("Priority")]
    [StringLength(20)]
    public string Priority { get; set; } = "NORMAL";

    [Column("CreateTime")]
    public DateTime CreateTime { get; set; }

    [Column("HandleTime")]
    public DateTime? HandleTime { get; set; }

    [Column("TodoGroupID")]
    public int? TodoGroupId { get; set; }

    [Column("OriginalUserID")]
    public int? OriginalUserId { get; set; }

    [Column("IsDelegate")]
    public bool IsDelegate { get; set; }

    [Column("DelegateSourceUserID")]
    public int? DelegateSourceUserId { get; set; }

    [Column("IsCosign")]
    public bool IsCosign { get; set; }

    [Column("CosignParentTaskID")]
    public int? CosignParentTaskId { get; set; }

    [Column("ClaimTime")]
    public DateTime? ClaimTime { get; set; }

    [NotMapped]
    public int BusinessId => int.TryParse(ObjectKey, out var id) ? id : 0;
}
