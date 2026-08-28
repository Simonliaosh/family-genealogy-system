using FamilyTree.Configuration;
using FamilyTree.Helpers;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class HrHomeController : Controller
{
    private readonly HrHomeService _home;
    private readonly EPrincipalAccessService _access;

    public HrHomeController(HrHomeService home, EPrincipalAccessService access)
    {
        _home = home;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = ResolveUserId();
        if (!uid.HasValue) return Forbid();

        var canApprove = (await _access.TryAuthorizeMvcAsync(uid.Value, "HrLeave", "Approval", ct)) != null;
        var vm = await _home.GetDashboardAsync(uid.Value, canApprove, ct);
        return View(vm);
    }

    private int? ResolveUserId()
    {
        var raw = User.FindFirst(FrameworkClaimTypes.EUserId)?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
