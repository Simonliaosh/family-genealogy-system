using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_Dash_UserSetting")]
public class DashUserSetting
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [Column("CurrentUserPosID")]
    public int CurrentUserPosId { get; set; }

    [Column("GlobalFilterJson")]
    public string? GlobalFilterJson { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("Operator")]
    [StringLength(30)]
    public string? OperatorName { get; set; }
}
