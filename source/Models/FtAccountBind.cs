using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("FamilyTree_AccountBind")]
public class FtAccountBind
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("UserId")]
    public int UserId { get; set; }

    [Column("WechatOpenId")]
    [StringLength(64)]
    public string? WechatOpenId { get; set; }

    [Column("IdCardHash")]
    [StringLength(64)]
    public string? IdCardHash { get; set; }

    [Column("Mobile")]
    [StringLength(32)]
    public string? Mobile { get; set; }

    [Column("BStatus")]
    [StringLength(1)]
    public string BStatus { get; set; } = "1";

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("Remark")]
    [StringLength(512)]
    public string? Remark { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("Operator")]
    [StringLength(30)]
    public string OperatorName { get; set; } = "";
}
