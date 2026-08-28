using FamilyTree.Helpers;
using System.Diagnostics;
using FamilyTree.Models;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly EPrincipalAccessService _ePrincipalAccess;
    private readonly HomeWorkbenchService _workbench;
    private readonly FrameworkDbContext _db;

    public HomeController(
        EPrincipalAccessService ePrincipalAccess,
        HomeWorkbenchService workbench,
        FrameworkDbContext db)
    {
        _ePrincipalAccess = ePrincipalAccess;
        _workbench = workbench;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        if (string.Equals(
                User.FindFirst(FrameworkClaimTypes.AuthPrincipalKind)?.Value,
                FrameworkClaimTypes.KindEUsers,
                StringComparison.OrdinalIgnoreCase)
            && int.TryParse(User.FindFirst(FrameworkClaimTypes.EUserId)?.Value, out var eUserId))
        {
            if (await DatabaseSchemaHelper.TableExistsAsync(_db, "Tbl_Dash_Indicator", HttpContext.RequestAborted))
            {
                ViewData["Title"] = "我的工作台";
                return View("~/Views/EDashBoard/Index.cshtml");
            }

            var eMenus = await _ePrincipalAccess.GetSidebarMenuAsync(eUserId, HttpContext.RequestAborted);
            var dashboard = await _workbench.BuildAsync(eUserId, HttpContext.RequestAborted);

            ViewBag.EmployeeName = User.FindFirst("EmployeeName")?.Value ?? User.Identity?.Name ?? "";
            ViewBag.Menus = eMenus;
            ViewBag.Stats = dashboard.Stats;
            ViewBag.HomeMainRows = dashboard.MainRows;
            return View();
        }

        return RedirectToAction("Login", "Account");
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
