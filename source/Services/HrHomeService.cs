using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public sealed class HrHomeService
{
    private readonly FrameworkDbContext _db;
    private readonly HrLeaveService _leave;

    public HrHomeService(FrameworkDbContext db, HrLeaveService leave)
    {
        _db = db;
        _leave = leave;
    }

    public async Task<HrHomeVm> GetDashboardAsync(int userId, bool canApprove, CancellationToken ct)
    {
        var todoCount = await _db.ETodoTasks.AsNoTracking()
            .CountAsync(t => t.UserId == userId && t.Status == 0, ct);

        return new HrHomeVm
        {
            MyPendingLeaveCount = await _leave.CountMyPendingAsync(userId, ct),
            MyTodoCount = todoCount,
            PendingApprovalCount = canApprove ? await _leave.CountPendingApprovalAsync(ct) : 0,
            CanApprove = canApprove
        };
    }
}
