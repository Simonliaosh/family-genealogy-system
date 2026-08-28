using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_ManagerSubordinate")]
public class EManagerSubordinate
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("ManagerUserID")]
    public int ManagerUserId { get; set; }

    [Column("SubUserID")]
    public int SubUserId { get; set; }

    [Column("ManagerPostID")]
    public int? ManagerPostId { get; set; }

    [Column("SubPostID")]
    public int? SubPostId { get; set; }

    [Column("DeptID")]
    public int? DeptId { get; set; }

    [Column("BStatus")]
    [StringLength(20)]
    public string BStatus { get; set; } = "启用";

    [Column("DispSeq")]
    public int DispSeq { get; set; } = 99;

    [Column("Remark")]
    public string? Remark { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("Operator")]
    [StringLength(8)]
    public string? OperatorName { get; set; }
}
