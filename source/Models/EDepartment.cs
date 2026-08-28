using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_Department")]
public class EDepartment
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("DeptCode")]
    [StringLength(30)]
    public string DeptCode { get; set; } = "";

    [Column("DeptCName")]
    [StringLength(100)]
    public string DeptCName { get; set; } = "";

    [Column("DeptEName")]
    [StringLength(100)]
    public string? DeptEName { get; set; }

    [Column("ParentDeptID")]
    public int? ParentDeptId { get; set; }

    [Column("DeptLevel")]
    public int DeptLevel { get; set; } = 1;

    [Column("DeptPath")]
    [StringLength(500)]
    public string? DeptPath { get; set; }

    [Column("DeptType")]
    [StringLength(30)]
    public string? DeptType { get; set; }

    [Column("LeaderUserID")]
    public int? LeaderUserId { get; set; }

    [Column("DispSeq")]
    public int DispSeq { get; set; } = 99;

    [Column("DDescription")]
    public string? DDescription { get; set; }

    [Column("Remark")]
    public string? Remark { get; set; }

    [Column("BStatus")]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("Operator")]
    [StringLength(30)]
    public string? OperatorName { get; set; }
}
