using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("FamilyTree_ApiToken")]
public class FtApiToken
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("UserId")]
    public int UserId { get; set; }

    [Column("TokenHash")]
    [StringLength(64)]
    public string TokenHash { get; set; } = "";

    [Column("ExpireDate")]
    public DateTime ExpireDate { get; set; }

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
