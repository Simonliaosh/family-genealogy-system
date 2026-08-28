using FamilyTree.Helpers;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EEventInstanceQueryController : Controller
{
    private readonly EEventInstanceQueryService _service;

    public EEventInstanceQueryController(EEventInstanceQueryService service)
    {
        _service = service;
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string? appCode1,
        string? eventCode1,
        string? eventStatus1,
        string? createTimeFrom,
        string? createTimeTo,
        string? searchField,
        string? searchContent,
        int? intPage,
        int? pageShowNum,
        CancellationToken ct = default)
    {
        if (!CanView()) return Forbid();
        var page = intPage.GetValueOrDefault(1);
        var pSize = Math.Clamp(pageShowNum.GetValueOrDefault(16), 1, 200);
        var (pageRows, total, totalPages, pageOut) = await _service.GetIndexPageAsync(
            appCode1, eventCode1, eventStatus1, createTimeFrom, createTimeTo,
            searchField ?? "ObjectKey", searchContent ?? "", page, pSize, ct);

        ViewBag.AppCode1 = appCode1 ?? "";
        ViewBag.EventCode1 = eventCode1 ?? "";
        ViewBag.EventStatus1 = eventStatus1 ?? "";
        ViewBag.CreateTimeFrom = createTimeFrom ?? "";
        ViewBag.CreateTimeTo = createTimeTo ?? "";
        ViewBag.SearchField = searchField ?? "ObjectKey";
        ViewBag.SearchContent = searchContent ?? "";
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.AppCodeOptions = EEventInstanceQueryService.BuildAppCodeFilterOptions();
        ViewBag.EventCodeOptions = await _service.LoadEventCodeFilterOptionsAsync(ct);
        ViewBag.StatusOptions = EEventInstanceQueryService.BuildStatusFilterOptions();
        return View(pageRows);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id, CancellationToken ct = default)
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
