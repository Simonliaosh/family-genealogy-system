using FamilyTree.Helpers;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class FtBranchApplyController : Controller
{
    private readonly FtBranchAdminApplyService _service;
    public FtBranchApplyController(FtBranchAdminApplyService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.Status = await _service.StatusForAsync(uid, ct);
        var (rows, total, pages, p) = await _service.GetIndexPageAsync(uid, null, 1, 20, ct);
        ViewBag.IntPage = p;
        ViewBag.PageShowNum = 20;
        ViewBag.TotalPages = pages;
        ViewBag.TotalRecords = total;
        ViewBag.SearchField = "ApplyStatus";
        ViewBag.SearchContent = "";
        ViewBag.SelectField = "CreateDate";
        ViewBag.SelectFieldArrow = "1";
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(string? reason, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        try
        {
            var msg = await _service.ApplyAsync(FtClaims.UserId(User) ?? 0, reason, FtClaims.Operator(User), ct);
            TempData["SuccessMessage"] = msg;
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
