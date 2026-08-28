using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

/// <summary>资源权限分配。DutyID → Tbl_E_Duty.DataID（与订阅模块一致）。</summary>
[Table("Tbl_E_ResourcePermission")]
public class EResourcePermission
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("DutyID")]
    public int DutyId { get; set; }

    [Column("ResourceID")]
    [StringLength(50)]
    public string ResourceId { get; set; } = "";

    [Column("CanCreate")]
    public bool CanCreate { get; set; }

    [Column("CanUpdate")]
    public bool CanUpdate { get; set; }

    [Column("CanDelete")]
    public bool CanDelete { get; set; }

    [Column("CanQuery")]
    public bool CanQuery { get; set; } = true;

    [Column("BStatus")]
    [StringLength(10)]
    public string BStatus { get; set; } = "启用";

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("AmendDate")]
    public DateTime AmendDate { get; set; }

    [Column("Operator")]
    [StringLength(8)]
    public string? OperatorName { get; set; }
}
