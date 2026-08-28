using FamilyTree.Helpers;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class ELoginLogController : Controller
{
    private readonly ELoginLogService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "LoginId", "IPAddress", "FailReason", "UserID"
    };

    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "LoginTime", "LoginId", "LoginStatus", "DataID"
    };

    public ELoginLogController(ELoginLogService service)
    {
        _service = service;
    }

    private string MemberId => User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? "";

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string? loginStatus1,
        string? loginIdFilter,
        string? loginTimeFrom,
        string? loginTimeTo,
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

        var eUserId = await _service.ResolveEUserDataIdByMemberIdAsync(MemberId, ct);

        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "LoginId";
        var sc = (searchContent ?? "").Trim();
        var pSize = pageShowNum.GetValueOrDefault(16);
        if (pSize <= 0) pSize = 16;
        if (pSize > 200) pSize = 200;
        var page = intPage.GetValueOrDefault(1);
        if (page <= 0) page = 1;
        var sortField = (selectField ?? "").Trim();
        if (!SortWhitelist.Contains(sortField)) sortField = "";
        var sortArrow = (selectFieldArrow ?? "0").Trim();

        var (pageRows, total, totalPages, pageOut, rangeFrom, rangeToEx) = await _service.GetIndexPageAsync(
            MemberId, eUserId, loginStatus1, loginIdFilter, loginTimeFrom, loginTimeTo,
            sf, sc, sortField, sortArrow, page, pSize, ct);

        ViewBag.LoginStatusOptions = ELoginLogService.BuildLoginStatusFilterOptions();
        ViewBag.LoginStatus1 = loginStatus1 ?? "";
        ViewBag.LoginIdFilter = loginIdFilter ?? "";
        ViewBag.LoginTimeFrom = loginTimeFrom ?? "";
        ViewBag.LoginTimeTo = loginTimeTo ?? "";
        ViewBag.SearchField = sf;
        ViewBag.SearchContent = sc;
        ViewBag.SelectField = string.IsNullOrWhiteSpace(sortField) ? "-" : sortField;
        ViewBag.SelectFieldArrow = sortArrow;
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.RangeNote = $"数据范围：与当前账号相关的记录；时间窗 {rangeFrom:yyyy-MM-dd} ～ {rangeToEx.AddDays(-1):yyyy-MM-dd}（未选手动日期时默认最近 {ELoginLogService.DefaultWindowDays} 天，最大跨度 {ELoginLogService.MaxRangeDays} 天）。";
        ViewBag.CanExport = FunctionLimitUi.MidIsOne(lim, 2);
        return View(pageRows);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id, CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim) || !FunctionLimitUi.CanView(lim, false)) return Forbid();
        var eUserId = await _service.ResolveEUserDataIdByMemberIdAsync(MemberId, ct);
        var vm = await _service.GetDetailForMemberAsync(id, MemberId, eUserId, ct);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        string? loginStatus1,
        string? loginIdFilter,
        string? loginTimeFrom,
        string? loginTimeTo,
        string? searchField,
        string? searchContent,
        CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim) || !FunctionLimitUi.MidIsOne(lim, 2)) return Forbid();

        var eUserId = await _service.ResolveEUserDataIdByMemberIdAsync(MemberId, ct);
        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "LoginId";
        var sc = (searchContent ?? "").Trim();

        var rows = await _service.GetExportRowsAsync(
            MemberId, eUserId, loginStatus1, loginIdFilter, loginTimeFrom, loginTimeTo, sf, sc, ct);
        var ids = rows.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value);
        var map = await _service.GetUserDisplayMapAsync(ids, ct);
        var bytes = ELoginLogService.BuildCsv(rows, map);
        var name = $"elogin_{DateTime.Now:yyyyMMddHHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", name);
    }
}
