using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class HrHomeVm
{
    public int MyPendingLeaveCount { get; set; }
    public int MyTodoCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public bool CanApprove { get; set; }
}

public sealed class HrLeaveFormVm
{
    [Required(ErrorMessage = "请选择假别")]
    [StringLength(20)]
    public string LeaveType { get; set; } = "年假";

    [Required]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [Range(0.5, 365, ErrorMessage = "天数须在 0.5～365 之间")]
    public decimal Days { get; set; } = 1;

    [StringLength(500)]
    public string? Reason { get; set; }
}

public sealed class HrLeaveListRowVm
{
    public int DataId { get; set; }
    public string RequestNo { get; set; } = "";
    public string LeaveType { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Days { get; set; }
    public string Status { get; set; } = "";
    public string StatusText { get; set; } = "";
    public DateTime CreateDate { get; set; }
    public string? ApplicantName { get; set; }
}

public sealed class HrLeaveDetailsVm
{
    public int DataId { get; set; }
    public string RequestNo { get; set; } = "";
    public string ApplicantName { get; set; } = "";
    public string LeaveType { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Days { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "";
    public string StatusText { get; set; } = "";
    public long? EventInstanceId { get; set; }
    public DateTime CreateDate { get; set; }
    public string? ApproveUserName { get; set; }
    public DateTime? ApproveTime { get; set; }
    public string? ApproveRemark { get; set; }
    public bool CanApprove { get; set; }
}

public sealed class HrLeaveApproveVm
{
    public int LeaveId { get; set; }

    [StringLength(200)]
    public string? Remark { get; set; }
}

public sealed class HrLeaveSubmitResultVm
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int? LeaveId { get; set; }
    public long? EventInstanceId { get; set; }
    public int TodoCreatedCount { get; set; }
}
