using System.Text.Json;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public sealed class HrLeaveService
{
    public const string ObjectType = "HrLeaveRequest";
    public const string EventSubmit = "FRAME.LEAVE.SUBMIT";
    public const string EventApproved = "FRAME.LEAVE.APPROVED";
    public const string EventRejected = "FRAME.LEAVE.REJECTED";

    private static readonly Dictionary<string, string> StatusTextMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["PENDING"] = "待审批",
            ["APPROVED"] = "已通过",
            ["REJECTED"] = "已驳回"
        };

    public const string DictLeaveType = "HR_LEAVE_TYPE";

    private readonly FrameworkDbContext _db;
    private readonly EventPublisherService _publisher;
    private readonly DictService _dict;

    public HrLeaveService(FrameworkDbContext db, EventPublisherService publisher, DictService dict)
    {
        _db = db;
        _publisher = publisher;
        _dict = dict;
    }

    public static string StatusDisplay(string? status) =>
        StatusTextMap.TryGetValue((status ?? "").Trim(), out var t) ? t : (status ?? "-");

    public static List<(string Value, string Text)> FallbackLeaveTypeOptions() =>
    [
        ("年假", "年假"), ("事假", "事假"), ("病假", "病假"), ("调休", "调休"), ("其他", "其他")
    ];

    public Task<List<(string Value, string Text)>> GetLeaveTypeFormOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictLeaveType, forFilter: false, formEmptyLabel: null,
            FallbackLeaveTypeOptions(), ct: ct);

    public async Task<int> CountMyPendingAsync(int userId, CancellationToken ct) =>
        await _db.HrLeaveRequests.AsNoTracking()
            .CountAsync(x => !x.IsDeleted && x.ApplicantUserId == userId && x.Status == "PENDING", ct);

    public async Task<int> CountPendingApprovalAsync(CancellationToken ct) =>
        await _db.HrLeaveRequests.AsNoTracking()
            .CountAsync(x => !x.IsDeleted && x.Status == "PENDING", ct);

    public async Task<List<HrLeaveListRowVm>> GetMyListAsync(int userId, CancellationToken ct)
    {
        var rows = await _db.HrLeaveRequests.AsNoTracking()
            .Where(x => !x.IsDeleted && x.ApplicantUserId == userId)
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync(ct);
        return rows.Select(ToListRow).ToList();
    }

    public async Task<List<HrLeaveListRowVm>> GetPendingApprovalListAsync(CancellationToken ct)
    {
        var rows = await _db.HrLeaveRequests.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == "PENDING")
            .OrderBy(x => x.CreateDate)
            .ToListAsync(ct);
        var userIds = rows.Select(x => x.ApplicantUserId).Distinct().ToList();
        var names = await _db.EUsers.AsNoTracking()
            .Where(u => userIds.Contains(u.DataId))
            .ToDictionaryAsync(u => u.DataId, u => u.RealName ?? u.LoginId, ct);

        return rows.Select(x =>
        {
            var row = ToListRow(x);
            row.ApplicantName = names.GetValueOrDefault(x.ApplicantUserId, "-");
            return row;
        }).ToList();
    }

    public async Task<bool> CanUserViewAsync(int leaveId, int userId, bool canApprove, CancellationToken ct)
    {
        if (canApprove) return true;
        return await _db.HrLeaveRequests.AsNoTracking()
            .AnyAsync(x => x.DataId == leaveId && !x.IsDeleted && x.ApplicantUserId == userId, ct);
    }

    public async Task<HrLeaveDetailsVm?> GetDetailsAsync(int id, int? currentUserId, bool canApprove, CancellationToken ct)
    {
        var row = await _db.HrLeaveRequests.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return null;
        if (currentUserId.HasValue && row.ApplicantUserId != currentUserId.Value && !canApprove)
            return null;

        var applicant = await _db.EUsers.AsNoTracking()
            .Where(u => u.DataId == row.ApplicantUserId)
            .Select(u => u.RealName ?? u.LoginId)
            .FirstOrDefaultAsync(ct);

        string? approverName = null;
        if (row.ApproveUserId.HasValue)
        {
            approverName = await _db.EUsers.AsNoTracking()
                .Where(u => u.DataId == row.ApproveUserId.Value)
                .Select(u => u.RealName ?? u.LoginId)
                .FirstOrDefaultAsync(ct);
        }

        return new HrLeaveDetailsVm
        {
            DataId = row.DataId,
            RequestNo = row.RequestNo,
            ApplicantName = applicant ?? "-",
            LeaveType = row.LeaveType,
            StartDate = row.StartDate,
            EndDate = row.EndDate,
            Days = row.Days,
            Reason = row.Reason,
            Status = row.Status,
            StatusText = StatusDisplay(row.Status),
            EventInstanceId = row.EventInstanceId,
            CreateDate = row.CreateDate,
            ApproveUserName = approverName,
            ApproveTime = row.ApproveTime,
            ApproveRemark = row.ApproveRemark,
            CanApprove = canApprove && row.Status == "PENDING"
        };
    }

    public async Task<(HrLeaveSubmitResultVm Result, List<(string Key, string Message)> Errors)> SubmitAsync(
        HrLeaveFormVm model,
        int applicantUserId,
        string operatorName,
        CancellationToken ct)
    {
        var errors = ValidateForm(model);
        if (errors.Count > 0)
            return (new HrLeaveSubmitResultVm { Success = false, Message = "请修正表单错误。" }, errors);

        var start = model.StartDate.Date;
        var end = model.EndDate.Date;
        var overlapPending = await _db.HrLeaveRequests.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.ApplicantUserId == applicantUserId && x.Status == "PENDING"
                && x.StartDate <= end && x.EndDate >= start, ct);
        if (overlapPending)
        {
            errors.Add(("", "您在该日期区间已有待审批的请假单，请勿重复提交。"));
            return (new HrLeaveSubmitResultVm { Success = false, Message = "提交失败。" }, errors);
        }

        var now = DateTime.Now;
        var requestNo = await GenerateRequestNoAsync(now, ct);
        var row = new EHrLeaveRequest
        {
            RequestNo = requestNo,
            ApplicantUserId = applicantUserId,
            LeaveType = model.LeaveType.Trim(),
            StartDate = model.StartDate.Date,
            EndDate = model.EndDate.Date,
            Days = model.Days,
            Reason = model.Reason?.Trim(),
            Status = "PENDING",
            BStatus = "1",
            IsDeleted = false,
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorName
        };
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            _db.HrLeaveRequests.Add(row);
            await _db.SaveChangesAsync(ct);

            var applicantName = await _db.EUsers.AsNoTracking()
                .Where(u => u.DataId == applicantUserId)
                .Select(u => u.RealName ?? u.LoginId)
                .FirstOrDefaultAsync(ct) ?? "员工";

            var payload = JsonSerializer.Serialize(new
            {
                requestNo,
                applicantName,
                leaveType = row.LeaveType,
                startDate = row.StartDate.ToString("yyyy-MM-dd"),
                endDate = row.EndDate.ToString("yyyy-MM-dd"),
                days = row.Days,
                reason = row.Reason
            });

            var publish = await _publisher.PublishAsync(new EventPublishRequest
            {
                AppCode = "FRAME",
                EventCode = EventSubmit,
                ObjectType = ObjectType,
                ObjectKey = row.DataId.ToString(),
                ObjectCode = requestNo,
                ObjectTitle = $"{applicantName} {row.LeaveType} {row.Days}天",
                ObjectUrl = $"/HrLeave/Details/{row.DataId}",
                PayloadJson = payload,
                IdempotencyKey = $"HR-LEAVE-SUBMIT-{row.DataId}",
                TriggerUserId = applicantUserId,
                OccurredTime = now
            }, ct);

            if (!publish.Success)
            {
                await tx.RollbackAsync(ct);
                return (new HrLeaveSubmitResultVm
                {
                    Success = false,
                    Message = "流程发布失败，请假单未保存：" + publish.Message
                }, errors);
            }

            if (publish.EventInstanceId.HasValue)
            {
                row.EventInstanceId = publish.EventInstanceId;
                row.AmendDate = DateTime.Now;
                await _db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return (new HrLeaveSubmitResultVm
            {
                Success = true,
                Message = publish.IdempotentHit
                    ? "提交成功（幂等命中，使用已有流程实例）。"
                    : publish.TodoCreatedCount > 0
                        ? $"提交成功，已生成 {publish.TodoCreatedCount} 条待办。"
                        : "提交成功（未生成待办，请检查事件订阅与流转规则）。",
                LeaveId = row.DataId,
                EventInstanceId = publish.EventInstanceId,
                TodoCreatedCount = publish.TodoCreatedCount
            }, errors);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> ApproveAsync(
        int leaveId,
        bool approved,
        int operatorUserId,
        string operatorName,
        string? approveRemark,
        CancellationToken ct)
    {
        var row = await _db.HrLeaveRequests
            .FirstOrDefaultAsync(x => x.DataId == leaveId && !x.IsDeleted, ct);
        if (row == null) return (false, "请假单不存在。");
        if (row.Status != "PENDING") return (false, "该单据已处理，无法重复审批。");
        if (row.ApplicantUserId == operatorUserId)
            return (false, "不能审批自己的请假申请。");

        var eventCode = approved ? EventApproved : EventRejected;
        var newStatus = approved ? "APPROVED" : "REJECTED";
        var applicantName = await _db.EUsers.AsNoTracking()
            .Where(u => u.DataId == row.ApplicantUserId)
            .Select(u => u.RealName ?? u.LoginId)
            .FirstOrDefaultAsync(ct) ?? "员工";

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var publish = await _publisher.PublishAsync(new EventPublishRequest
            {
                AppCode = "FRAME",
                EventCode = eventCode,
                ObjectType = ObjectType,
                ObjectKey = row.DataId.ToString(),
                ObjectCode = row.RequestNo,
                ObjectTitle = $"{row.RequestNo} {(approved ? "已通过" : "已驳回")}",
                ObjectUrl = $"/HrLeave/Details/{row.DataId}",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    row.RequestNo,
                    approved,
                    operatorUserId,
                    operatorName,
                    remark = approveRemark
                }),
                IdempotencyKey = $"HR-LEAVE-{newStatus}-{row.DataId}",
                TriggerUserId = operatorUserId,
                OccurredTime = DateTime.Now
            }, ct);

            if (!publish.Success)
            {
                await tx.RollbackAsync(ct);
                return (false, "审批结果事件发布失败，单据未变更：" + publish.Message);
            }

            row.Status = newStatus;
            row.ApproveUserId = operatorUserId;
            row.ApproveTime = DateTime.Now;
            row.ApproveRemark = approveRemark?.Trim();
            row.AmendDate = DateTime.Now;
            row.OperatorName = operatorName;

            var todos = await _db.ETodoTasks
                .Where(t => t.ObjectType == ObjectType && t.ObjectKey == row.DataId.ToString() && t.Status == 0)
                .ToListAsync(ct);
            foreach (var todo in todos)
            {
                todo.Status = 1;
                todo.HandleTime = DateTime.Now;
                todo.HandlerDeptId = null;
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, publish.IdempotentHit
                ? (approved ? "该单已批准（幂等命中）。" : "该单已驳回（幂等命中）。")
                : (approved ? "已批准该请假申请。" : "已驳回该请假申请。"));
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static HrLeaveListRowVm ToListRow(EHrLeaveRequest x) => new()
    {
        DataId = x.DataId,
        RequestNo = x.RequestNo,
        LeaveType = x.LeaveType,
        StartDate = x.StartDate,
        EndDate = x.EndDate,
        Days = x.Days,
        Status = x.Status,
        StatusText = StatusDisplay(x.Status),
        CreateDate = x.CreateDate
    };

    private static List<(string Key, string Message)> ValidateForm(HrLeaveFormVm model)
    {
        var errors = new List<(string, string)>();
        if (model.EndDate.Date < model.StartDate.Date)
            errors.Add((nameof(HrLeaveFormVm.EndDate), "结束日期不能早于开始日期。"));
        if (model.Days <= 0)
            errors.Add((nameof(HrLeaveFormVm.Days), "天数必须大于 0。"));
        var spanDays = (model.EndDate.Date - model.StartDate.Date).Days + 1;
        if (model.Days > spanDays)
            errors.Add((nameof(HrLeaveFormVm.Days), $"天数不应超过区间自然日数（{spanDays} 天）。"));
        return errors;
    }

    private async Task<string> GenerateRequestNoAsync(DateTime now, CancellationToken ct)
    {
        var prefix = "LR" + now.ToString("yyyyMMdd");
        var lastNo = await _db.HrLeaveRequests.AsNoTracking()
            .Where(x => x.RequestNo.StartsWith(prefix))
            .OrderByDescending(x => x.RequestNo)
            .Select(x => x.RequestNo)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(lastNo) || lastNo.Length < prefix.Length + 4)
            return prefix + "0001";
        var seq = int.TryParse(lastNo.AsSpan(prefix.Length), out var n) ? n + 1 : 1;
        return prefix + seq.ToString("D4");
    }
}
