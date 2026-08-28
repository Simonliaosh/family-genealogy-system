using FamilyTree.Helpers;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EAppModuleQueryController : Controller
{
    private readonly EAppModuleQueryService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppCode", "AppName"
    };

    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppCode", "AppName", "AppType", "DispSeq", "BStatus"
    };

    public EAppModuleQueryController(EAppModuleQueryService service)
    {
        _service = service;
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string? appType1,
        string? bStatus1,
        string? searchField,
        string? searchContent,
        string? selectField,
        string? selectFieldArrow,
        int? intPage,
        int? pageShowNum,
        CancellationToken ct = default)
    {
        if (!CanView()) return Forbid();
        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "AppCode";
        var sc = (searchContent ?? "").Trim();
        var pSize = Math.Clamp(pageShowNum.GetValueOrDefault(16), 1, 200);
        var page = Math.Max(1, intPage.GetValueOrDefault(1));
        var sortField = (selectField ?? "").Trim();
        if (!SortWhitelist.Contains(sortField)) sortField = "";
        var sortArrow = (selectFieldArrow ?? "0").Trim();

        var (pageRows, total, totalPages, pageOut) = await _service.GetIndexPageAsync(
            appType1, bStatus1, sf, sc, sortField, sortArrow, page, pSize, ct);

        ViewBag.AppType1 = appType1 ?? "";
        ViewBag.BStatus1 = bStatus1 ?? "";
        ViewBag.SearchField = sf;
        ViewBag.SearchContent = sc;
        ViewBag.SelectField = string.IsNullOrWhiteSpace(sortField) ? "-" : sortField;
        ViewBag.SelectFieldArrow = sortArrow;
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.AppTypeOptions = await _service.GetAppTypeFilterOptionsAsync(ct);
        ViewBag.BStatusOptions = EAppModuleQueryService.BuildBStatusFilterOptions();
        return View(pageRows);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct = default)
    {
        if (!CanView()) return Forbid();
        var vm = await _service.GetDetailAsync(id, ct);
        if (vm == null) return NotFound();
        return View(vm);
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
