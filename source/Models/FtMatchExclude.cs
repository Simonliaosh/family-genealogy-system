using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

/// <summary>排除匹配：认定两人不是同一人，后续匹配跳过。</summary>
[Table("FamilyTree_MatchExclude")]
public class FtMatchExclude
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("PersonLoId")]
    public int PersonLoId { get; set; }

    [Column("PersonHiId")]
    public int PersonHiId { get; set; }

    [Column("MarkUserId")]
    public int MarkUserId { get; set; }

    [Column("Remark")]
    [StringLength(256)]
    public string? Remark { get; set; }

    [Column("BStatus")]
    [StringLength(1)]
    public string BStatus { get; set; } = "1";

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("Operator")]
    [StringLength(30)]
    public string OperatorName { get; set; } = "";
}
