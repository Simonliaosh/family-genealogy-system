using FamilyTree.Helpers;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EDictQueryController : Controller
{
    private readonly EDictQueryService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "DictTypeCode", "DictTypeName"
    };

    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppCode", "DictTypeCode", "DictTypeName", "BStatus", "IsSystem", "IsEditable", "ItemCount"
    };

    public EDictQueryController(EDictQueryService service)
    {
        _service = service;
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string? appCode1,
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
        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "DictTypeCode";
        var sc = (searchContent ?? "").Trim();
        var pSize = Math.Clamp(pageShowNum.GetValueOrDefault(16), 1, 200);
        var page = Math.Max(1, intPage.GetValueOrDefault(1));
        var sortField = (selectField ?? "").Trim();
        if (!SortWhitelist.Contains(sortField)) sortField = "";
        var sortArrow = (selectFieldArrow ?? "0").Trim();

        var (pageRows, total, totalPages, pageOut) = await _service.GetTypeIndexPageAsync(
            appCode1, bStatus1, sf, sc, sortField, sortArrow, page, pSize, ct);

        ViewBag.AppCode1 = appCode1 ?? "";
        ViewBag.BStatus1 = bStatus1 ?? "";
        ViewBag.SearchField = sf;
        ViewBag.SearchContent = sc;
        ViewBag.SelectField = string.IsNullOrWhiteSpace(sortField) ? "-" : sortField;
        ViewBag.SelectFieldArrow = sortArrow;
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.AppCodeOptions = EDictQueryService.BuildAppCodeFilterOptions();
        ViewBag.BStatusOptions = EDictQueryService.BuildBStatusFilterOptions();
        return View(pageRows);
    }

    [HttpGet]
    public async Task<IActionResult> Items(
        string code,
        string? bStatus1,
        string? searchContent,
        int? intPage,
        int? pageShowNum,
        CancellationToken ct = default)
    {
        if (!CanView()) return Forbid();
        if (string.IsNullOrWhiteSpace(code)) return NotFound();
        var header = await _service.GetTypeHeaderAsync(code, ct);
        if (header == null) return NotFound();

        var page = intPage.GetValueOrDefault(1);
        var pSize = Math.Clamp(pageShowNum.GetValueOrDefault(32), 1, 200);
        var (pageRows, total, totalPages, pageOut) = await _service.GetItemsPageAsync(
            code, bStatus1, searchContent ?? "", page, pSize, ct);

        ViewBag.TypeHeader = header;
        ViewBag.BStatus1 = bStatus1 ?? "";
        ViewBag.SearchContent = searchContent ?? "";
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.BStatusOptions = EDictQueryService.BuildBStatusFilterOptions();
        return View(pageRows);
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
