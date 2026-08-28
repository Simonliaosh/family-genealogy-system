using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EUserHandoverController : Controller
{
    private readonly EUserHandoverService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Source", "Target", "Operator", "Remark", "HandoverType"
    };

    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "HandoverTime", "HandoverType", "DataID", "SourceUserID", "TargetUserID"
    };

    public EUserHandoverController(EUserHandoverService service)
    {
        _service = service;
    }

    private string MemberId => User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? "";

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string? handoverType1,
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

        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "Source";
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
            handoverType1, sf, sc, sortField, sortArrow, page, pSize, ct);

        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.HandoverTypeOptions = await _service.GetHandoverTypeFilterOptionsAsync(ct);
        ViewBag.HandoverType1 = handoverType1 ?? "";
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
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (await GetOperatorUserIdAsync(ct) == null) return Forbid();
        ViewBag.UserOptions = await _service.LoadActiveUserOptionsAsync(ct);
        ViewBag.HandoverTypeFormOptions = await _service.GetHandoverTypeFormOptionsAsync(ct);
        return View(new EUserHandoverFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EUserHandoverFormVm model, string? selectNo, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();

        var opId = await GetOperatorUserIdAsync(ct);
        if (!opId.HasValue) return Forbid();

        await _service.NormalizeFormForSaveAsync(model, ct);
        if (!ModelState.IsValid) return await ReturnCreateInvalidAsync(model, ct);

        foreach (var (k, m) in await _service.GetSaveValidationErrorsAsync(model, ct))
            ModelState.AddModelError(k, m);
        if (!ModelState.IsValid) return await ReturnCreateInvalidAsync(model, ct);

        await _service.CreateAsync(model, opId.Value, ct);

        if (string.Equals(selectNo, "0", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));
        return RedirectToAction(nameof(Create));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim) || !FunctionLimitUi.CanView(lim, false)) return Forbid();
        var vm = await _service.GetDetailAsync(id, ct);
        if (vm == null) return NotFound();
        return View(vm);
    }

    private async Task<int?> GetOperatorUserIdAsync(CancellationToken ct) =>
        await _service.ResolveEUserDataIdByMemberIdAsync(MemberId, ct);

    private async Task<IActionResult> ReturnCreateInvalidAsync(EUserHandoverFormVm model, CancellationToken ct)
    {
        ViewBag.UserOptions = await _service.LoadActiveUserOptionsAsync(ct);
        ViewBag.HandoverTypeFormOptions = await _service.GetHandoverTypeFormOptionsAsync(ct);
        return View(nameof(Create), model);
    }
}
