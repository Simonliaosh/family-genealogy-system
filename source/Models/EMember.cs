using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

/// <summary>人员档案（Tbl_E_Member）。</summary>
[Table("Tbl_E_Member")]
public class EMember
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("MemberID")]
    [StringLength(30)]
    public string MemberId { get; set; } = "";

    [Column("MemberName")]
    [StringLength(20)]
    public string MemberName { get; set; } = "";

    [Column("Sex")]
    [StringLength(2)]
    public string? Sex { get; set; }

    [Column("Nation")]
    [StringLength(12)]
    public string? Nation { get; set; }

    [Column("NativePlace")]
    [StringLength(30)]
    public string? NativePlace { get; set; }

    [Column("IDNo")]
    [StringLength(20)]
    public string? IdNo { get; set; }

    [Column("Birthday", TypeName = "date")]
    public DateTime? Birthday { get; set; }

    [Column("ELevel")]
    [StringLength(12)]
    public string? ELevel { get; set; }

    [Column("Degree")]
    [StringLength(20)]
    public string? Degree { get; set; }

    [Column("School")]
    [StringLength(40)]
    public string? School { get; set; }

    [Column("Speciality")]
    [StringLength(30)]
    public string? Speciality { get; set; }

    [Column("ComputerAbility")]
    [StringLength(30)]
    public string? ComputerAbility { get; set; }

    [Column("WorkDate", TypeName = "date")]
    public DateTime? WorkDate { get; set; }

    [Column("PerGrade")]
    [StringLength(10)]
    public string? PerGrade { get; set; }

    /// <summary>默认部门（库列 DefaultDeptID；任岗以 Tbl_E_UserPosition 为准）。</summary>
    [Column("DefaultDeptID")]
    public int? DeptId { get; set; }

    /// <summary>默认岗位（库列 DefaultPosID）。</summary>
    [Column("DefaultPosID")]
    public int? PosId { get; set; }

    [Column("HomeAddr")]
    [StringLength(100)]
    public string? HomeAddr { get; set; }

    [Column("Mobile")]
    [StringLength(20)]
    public string? Mobile { get; set; }

    [Column("HomePhone")]
    [StringLength(20)]
    public string? HomePhone { get; set; }

    [Column("WorkPhone")]
    [StringLength(20)]
    public string? WorkPhone { get; set; }

    [Column("WXNum")]
    [StringLength(30)]
    public string? WxNum { get; set; }

    [Column("QQNum")]
    [StringLength(20)]
    public string? QqNum { get; set; }

    [Column("EMail")]
    [StringLength(80)]
    public string? EMail { get; set; }

    [Column("Health")]
    [StringLength(8)]
    public string? Health { get; set; }

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
