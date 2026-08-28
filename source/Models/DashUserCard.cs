using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_Dash_UserCard")]
public class DashUserCard
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [Column("UserPosID")]
    public int UserPosId { get; set; }

    [Column("PosID")]
    public int PosId { get; set; }

    [Column("DeptID")]
    public int DeptId { get; set; }

    [Column("IndicatorID")]
    public int IndicatorId { get; set; }

    [Column("LayoutRow")]
    public int LayoutRow { get; set; }

    [Column("LayoutCol")]
    public int LayoutCol { get; set; }

    [Column("ColSpan")]
    public byte ColSpan { get; set; } = 2;

    [Column("IsLock")]
    public bool IsLock { get; set; }

    [Column("UserFilterJson")]
    public string? UserFilterJson { get; set; }

    [Column("IsHide")]
    public bool IsHide { get; set; }

    [Column("CardTitle")]
    [StringLength(100)]
    public string CardTitle { get; set; } = "";

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
