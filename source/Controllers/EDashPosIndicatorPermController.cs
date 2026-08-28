using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EDashPosIndicatorPermController : Controller
{
    private readonly DashPosIndicatorPermService _service;

    public EDashPosIndicatorPermController(DashPosIndicatorPermService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? posId, CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim) || !FunctionLimitUi.CanView(lim, false)) return Forbid();

        var vm = await _service.GetIndexAsync(posId ?? 0, ct);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(DashPosPermSaveVm model, CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim) || !FunctionLimitUi.CanUpdate(lim, false)) return Forbid();

        if (!ModelState.IsValid)
        {
            var vm = await _service.GetIndexAsync(model.PosId, ct);
            ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
            return View(nameof(Index), vm);
        }

        var op = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
        await _service.SaveAsync(model, op, ct);
        return RedirectToAction(nameof(Index), new { posId = model.PosId });
    }
}
