using FamilyTree.Helpers;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class FtTreeHealthController : Controller
{
    private readonly FtTreeHealthService _health;
    private readonly FtMatchService _match;
    private readonly FtDutyAccess _duty;

    public FtTreeHealthController(FtTreeHealthService health, FtMatchService match, FtDutyAccess duty)
    {
        _health = health;
        _match = match;
        _duty = duty;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        if (!await _health.CanViewAsync(uid, ct) && !CanViewByLimit())
            return Forbid();

        var issues = await _health.ScanAsync(ct);
        ViewBag.CanOpenConflict = await _duty.IsSuperAsync(uid, ct)
            || await _duty.IsBranchAdminAsync(uid, ct);
        return View(issues);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenConflict(int? personId, int? relatedId, string issueType, string detail, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        if (!await _duty.IsSuperAsync(uid, ct) && !await _duty.IsBranchAdminAsync(uid, ct))
            return Forbid();
        try
        {
            await _match.OpenConflictAsync(personId, relatedId, string.IsNullOrWhiteSpace(issueType) ? "HEALTH" : issueType,
                detail ?? "", FtClaims.Operator(User), uid, ct);
            TempData["SuccessMessage"] = "已开冲突单。";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    private bool CanViewByLimit()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
