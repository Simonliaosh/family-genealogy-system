using FamilyTree.Helpers;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EEventLogController : Controller
{
    private readonly EEventLogService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "EventCode", "ObjectKey", "ObjectType", "BusinessID", "User", "Remark", "HandleResult"
    };

    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreateTime", "EventCode", "HandleResult", "DataID"
    };

    public EEventLogController(EEventLogService service)
    {
        _service = service;
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string? eventCode1,
        string? handleResult1,
        string? createTimeFrom,
        string? createTimeTo,
        string? searchField,
        string? searchContent,
        string? selectField,
        string? selectFieldArrow,
        int? intPage,
        int? pageShowNum,
        CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim) || !FunctionLimitUi.CanView(lim, false)) return Forbid();

        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "EventCode";
        var sc = (searchContent ?? "").Trim();
        var pSize = pageShowNum.GetValueOrDefault(16);
        if (pSize <= 0) pSize = 16;
        if (pSize > 200) pSize = 200;
        var page = intPage.GetValueOrDefault(1);
        if (page <= 0) page = 1;
        var sortField = (selectField ?? "").Trim();
        if (!SortWhitelist.Contains(sortField)) sortField = "";
        var sortArrow = (selectFieldArrow ?? "0").Trim();

        var (pageRows, total, totalPages, pageOut) = await _service.GetIndexPageAsync(
            eventCode1, handleResult1, createTimeFrom, createTimeTo,
            sf, sc, sortField, sortArrow, page, pSize, ct);

        var events = await _service.LoadActiveEventOptionsAsync(ct);
        ViewBag.EventCodeOptions = EEventLogService.BuildEventCodeFilterOptions(events);
        ViewBag.HandleResultOptions = EEventLogService.BuildHandleResultFilterOptions();
        ViewBag.EventCode1 = eventCode1 ?? "";
        ViewBag.HandleResult1 = handleResult1 ?? "";
        ViewBag.CreateTimeFrom = createTimeFrom ?? "";
        ViewBag.CreateTimeTo = createTimeTo ?? "";
        ViewBag.SearchField = sf;
        ViewBag.SearchContent = sc;
        ViewBag.SelectField = string.IsNullOrWhiteSpace(sortField) ? "-" : sortField;
        ViewBag.SelectFieldArrow = sortArrow;
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        return View(pageRows);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id, CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim) || !FunctionLimitUi.CanView(lim, false)) return Forbid();
        var vm = await _service.GetDetailAsync(id, ct);
        if (vm == null) return NotFound();
        return View(vm);
    }
}
