using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EAppModuleController : Controller
{
    private readonly EAppModuleService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppCode", "AppName", "BaseUrl"
    };

    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppCode", "AppName", "AppType", "DispSeq", "BStatus", "AmendDate", "OperatorName"
    };

    public EAppModuleController(EAppModuleService service) => _service = service;

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
        var lim = HttpContext.Items["PubFunctionLimit"] as string;

        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "AppCode";
        var pSize = Math.Clamp(pageShowNum.GetValueOrDefault(16), 1, 200);
        var page = Math.Max(1, intPage.GetValueOrDefault(1));
        var sortField = (selectField ?? "").Trim();
        if (!SortWhitelist.Contains(sortField)) sortField = "";
        var sortArrow = (selectFieldArrow ?? "0").Trim();

        var (pageRows, total, totalPages, pageOut) = await _service.GetIndexPageAsync(
            appType1, bStatus1, sf, (searchContent ?? "").Trim(),
            sortField, sortArrow, page, pSize, ct);

        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = FunctionLimitUi.CanDelete(lim, false);
        ViewBag.AppType1 = appType1 ?? "";
        ViewBag.BStatus1 = bStatus1 ?? "";
        ViewBag.SearchField = sf;
        ViewBag.SearchContent = searchContent ?? "";
        ViewBag.SelectField = string.IsNullOrWhiteSpace(sortField) ? "-" : sortField;
        ViewBag.SelectFieldArrow = sortArrow;
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.AppTypeOptions = await _service.GetAppTypeFilterOptionsAsync(ct);
        ViewBag.BStatusOptions = EAppModuleService.BuildBStatusFilterOptions();
        return View(pageRows);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        await PopulateFormOptionsAsync(ct);
        return View(new EAppModuleFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EAppModuleFormVm model, string? selectNo, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();

        _service.NormalizeFormForSave(model, isEdit: false);
        if (!ModelState.IsValid) return await ReturnCreateInvalidAsync(model, ct);

        foreach (var (key, msg) in await _service.GetSaveValidationErrorsAsync(model, false, null, ct))
            ModelState.AddModelError(key, msg);
        if (!ModelState.IsValid) return await ReturnCreateInvalidAsync(model, ct);

        try
        {
            await _service.CreateAsync(model, GetOperatorId(), ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.AppCode), ex.Message);
            return await ReturnCreateInvalidAsync(model, ct);
        }

        if (string.Equals(selectNo, "0", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));
        return RedirectToAction(nameof(Create));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? polistRt = null, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var row = await _service.GetByIdNoTrackAsync(id, ct);
        if (row == null) return NotFound();
        ViewBag.PolistRt = polistRt;
        ViewBag.EditMode = true;
        ViewBag.IsCoreApp = EAppModuleService.ProtectedAppCodes.Contains(row.AppCode);
        await PopulateFormOptionsAsync(ct);
        return View(_service.ToForm(row));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EAppModuleFormVm model, string? selectNo, string? polistRt, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        ViewBag.PolistRt = polistRt;
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();

        var existing = await _service.GetByIdNoTrackAsync(id, ct);
        if (existing == null) return NotFound();

        _service.NormalizeFormForSave(model, isEdit: true);
        model.DataId = existing.DataId;
        model.AppCode = existing.AppCode;
        ViewBag.EditMode = true;
        ViewBag.IsCoreApp = EAppModuleService.ProtectedAppCodes.Contains(existing.AppCode);
        if (!ModelState.IsValid) return await ReturnEditInvalidAsync(model, ct);

        foreach (var (key, msg) in await _service.GetSaveValidationErrorsAsync(model, true, existing.DataId, ct))
            ModelState.AddModelError(key, msg);
        if (!ModelState.IsValid) return await ReturnEditInvalidAsync(model, ct);

        await _service.TryUpdateAsync(id, model, GetOperatorId(), ct);
        if (string.Equals(selectNo, "0", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        return RedirectToAction(nameof(Edit), new { id, polistRt });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        try { await _service.DeleteAsync(id, ct); }
        catch (InvalidOperationException) { }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchDelete(int[]? ids, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (ids is { Length: > 0 })
            await _service.BatchDeleteAsync(ids.Distinct().ToList(), ct);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReturnCreateInvalidAsync(EAppModuleFormVm model, CancellationToken ct)
    {
        await PopulateFormOptionsAsync(ct);
        return View(nameof(Create), model);
    }

    private async Task<IActionResult> ReturnEditInvalidAsync(EAppModuleFormVm model, CancellationToken ct)
    {
        await PopulateFormOptionsAsync(ct);
        return View(nameof(Edit), model);
    }

    private async Task PopulateFormOptionsAsync(CancellationToken ct)
    {
        ViewBag.AppTypeOptions = await _service.GetAppTypeFormOptionsAsync(ct);
        ViewBag.BStatusOptions = EAppModuleService.BuildBStatusFormOptions();
    }

    private string GetOperatorId()
    {
        var memberId = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
        return _service.Normalize(memberId, 30, "system");
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
