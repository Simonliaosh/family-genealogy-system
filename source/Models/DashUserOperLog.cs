using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_Dash_UserOperLog")]
public class DashUserOperLog
{
    [Key]
    [Column("DataID")]
    public long DataId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [Column("PosID")]
    public int? PosId { get; set; }

    [Column("OperType")]
    [StringLength(30)]
    public string OperType { get; set; } = "";

    [Column("OperContent")]
    [StringLength(500)]
    public string? OperContent { get; set; }

    [Column("OperTime")]
    public DateTime OperTime { get; set; }

    [Column("Operator")]
    [StringLength(30)]
    public string? OperatorName { get; set; }
}
