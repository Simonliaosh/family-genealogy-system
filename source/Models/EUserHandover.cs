using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

/// <summary>用户交接记录（Tbl_E_UserHandover）。首版仅落库，不联动权限/待办转派等业务。</summary>
[Table("Tbl_E_UserHandover")]
public class EUserHandover
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("SourceUserID")]
    public int SourceUserId { get; set; }

    [Column("TargetUserID")]
    public int TargetUserId { get; set; }

    [Column("HandoverType")]
    [StringLength(30)]
    public string HandoverType { get; set; } = "";

    [Column("HandoverTime")]
    public DateTime HandoverTime { get; set; }

    [Column("OperatorUserID")]
    public int OperatorUserId { get; set; }

    [Column("Remark")]
    public string? Remark { get; set; }
}
