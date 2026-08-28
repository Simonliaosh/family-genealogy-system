using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

/// <summary>请假申请单（Tbl_E_HrLeaveRequest）。</summary>
[Table("Tbl_E_HrLeaveRequest")]
public class EHrLeaveRequest
{
    [Key]
    [Column("DataID")]
    public int DataId { get; set; }

    [Column("RequestNo")]
    [StringLength(30)]
    public string RequestNo { get; set; } = "";

    [Column("ApplicantUserID")]
    public int ApplicantUserId { get; set; }

    [Column("LeaveType")]
    [StringLength(20)]
    public string LeaveType { get; set; } = "";

    [Column("StartDate", TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column("EndDate", TypeName = "date")]
    public DateTime EndDate { get; set; }

    [Column("Days", TypeName = "decimal(5,1)")]
    public decimal Days { get; set; }

    [Column("Reason")]
    [StringLength(500)]
    public string? Reason { get; set; }

    /// <summary>PENDING / APPROVED / REJECTED</summary>
    [Column("Status")]
    [StringLength(20)]
    public string Status { get; set; } = "PENDING";

    [Column("EventInstanceID")]
    public long? EventInstanceId { get; set; }

    [Column("ApproveUserID")]
    public int? ApproveUserId { get; set; }

    [Column("ApproveTime")]
    public DateTime? ApproveTime { get; set; }

    [Column("ApproveRemark")]
    [StringLength(200)]
    public string? ApproveRemark { get; set; }

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
