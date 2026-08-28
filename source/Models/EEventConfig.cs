using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_EventConfig")]
public class EEventConfig
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("AppCode")]
    [StringLength(50)]
    public string AppCode { get; set; } = "FRAME";

    [Column("EventCode")]
    [StringLength(100)]
    public string EventCode { get; set; } = "";

    [Column("EventName")]
    [StringLength(100)]
    public string EventName { get; set; } = "";

    [Column("PageUrl")]
    [StringLength(300)]
    public string? PageUrl { get; set; }

    [Column("MenuGroupCode")]
    [StringLength(50)]
    public string? MenuGroupCode { get; set; }

    [Column("ExecType")]
    [StringLength(20)]
    public string ExecType { get; set; } = "ASYNC";

    [Column("EventType")]
    [StringLength(50)]
    public string? EventType { get; set; }

    [Column("IsGenerateTodo")]
    public bool IsGenerateTodo { get; set; } = true;

    [Column("TodoTitle")]
    [StringLength(200)]
    public string? TodoTitle { get; set; }

    [Column("HandleMode")]
    [StringLength(30)]
    public string HandleMode { get; set; } = "SINGLE";

    [Column("DefaultDueMinutes")]
    public int? DefaultDueMinutes { get; set; }

    [Column("DispSeq")]
    public int DispSeq { get; set; } = 99;

    [Column("BStatus")]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("Remark")]
    public string? Remark { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("Operator")]
    [StringLength(30)]
    public string? OperatorName { get; set; }
}
