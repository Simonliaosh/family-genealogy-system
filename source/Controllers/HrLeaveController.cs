using FamilyTree.Configuration;
using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class HrLeaveController : Controller
{
    private readonly HrLeaveService _service;
    private readonly EPrincipalAccessService _access;

    public HrLeaveController(HrLeaveService service, EPrincipalAccessService access)
    {
        _service = service;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = ResolveUserId();
        if (!uid.HasValue) return Forbid();
        var rows = await _service.GetMyListAsync(uid.Value, ct);
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        if (!CanCreate()) return Forbid();
        ViewBag.LeaveTypes = await _service.GetLeaveTypeFormOptionsAsync(ct);
        return View(new HrLeaveFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HrLeaveFormVm model, CancellationToken ct)
    {
        if (!CanCreate()) return Forbid();
        var uid = ResolveUserId();
        if (!uid.HasValue) return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.LeaveTypes = await _service.GetLeaveTypeFormOptionsAsync(ct);
            return View(model);
        }

        var op = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
        var (result, errors) = await _service.SubmitAsync(model, uid.Value, op, ct);
        foreach (var (key, msg) in errors)
            ModelState.AddModelError(key, msg);

        if (!result.Success)
        {
            ViewBag.SubmitResult = result;
            ViewBag.LeaveTypes = await _service.GetLeaveTypeFormOptionsAsync(ct);
            return View(model);
        }

        TempData["HrLeaveMessage"] = $"提交成功。单号关联事件实例 {result.EventInstanceId}，生成待办 {result.TodoCreatedCount} 条。";
        return RedirectToAction(nameof(Details), new { id = result.LeaveId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = ResolveUserId();
        if (!uid.HasValue) return Forbid();
        var canApprove = await HasApprovalAccessAsync(uid.Value, ct);
        if (!await _service.CanUserViewAsync(id, uid.Value, canApprove, ct))
            return Forbid();
        var vm = await _service.GetDetailsAsync(id, uid, canApprove, ct);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Approval(CancellationToken ct)
    {
        var uid = ResolveUserId();
        if (!uid.HasValue || !await HasApprovalAccessAsync(uid.Value, ct))
            return Forbid();
        var rows = await _service.GetPendingApprovalListAsync(ct);
        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string action, string? remark, CancellationToken ct)
    {
        var uid = ResolveUserId();
        if (!uid.HasValue || !await HasApprovalAccessAsync(uid.Value, ct))
            return Forbid();
        if (!await _service.CanUserViewAsync(id, uid.Value, canApprove: true, ct))
            return Forbid();

        var approved = string.Equals(action, "approve", StringComparison.OrdinalIgnoreCase);
        var op = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
        var (ok, msg) = await _service.ApproveAsync(id, approved, uid.Value, op, remark, ct);
        TempData[ok ? "HrLeaveMessage" : "HrLeaveError"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<bool> HasApprovalAccessAsync(int userId, CancellationToken ct)
    {
        var auth = await _access.TryAuthorizeMvcAsync(userId, "HrLeave", "Approval", ct);
        return auth != null;
    }

    private int? ResolveUserId()
    {
        var raw = User.FindFirst(FrameworkClaimTypes.EUserId)?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }

    private string? Lim => HttpContext.Items["PubFunctionLimit"] as string;

    private bool CanView() => !string.IsNullOrEmpty(Lim) && FunctionLimitUi.CanView(Lim!, false);

    private bool CanCreate() => !string.IsNullOrEmpty(Lim) && FunctionLimitUi.CanCreate(Lim!, false);

    private bool CanUpdate() => !string.IsNullOrEmpty(Lim) && FunctionLimitUi.CanUpdate(Lim!, false);
}
