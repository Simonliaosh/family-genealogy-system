using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_E_UserPosition")]
public class EUserPosition
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [Column("PosID")]
    public int PosId { get; set; }

    [Column("MemberID")]
    public int? MemberId { get; set; }

    [Column("DeptID")]
    public int DeptId { get; set; }

    [Column("IsPrimary")]
    public bool IsPrimary { get; set; }

    [Column("BeginDate", TypeName = "date")]
    public DateTime? BeginDate { get; set; }

    [Column("EndDate", TypeName = "date")]
    public DateTime? EndDate { get; set; }

    [Column("BStatus")]
    [StringLength(10)]
    public string BStatus { get; set; } = "启用";

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("Operator")]
    [StringLength(8)]
    public string? OperatorName { get; set; }
}
