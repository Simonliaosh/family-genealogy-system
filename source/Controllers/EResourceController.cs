using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EResourceController : Controller
{
    private readonly EResourceService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppCode", "ResourceID", "ResourceName", "MenuPath", "Remark"
    };

    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppCode", "ResourceID", "ResourceName", "ResourceType", "BStatus"
    };

    public EResourceController(EResourceService service)
    {
        _service = service;
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string? bStatus1,
        string? appCode1,
        string? resourceType1,
        string? menuGroupCode1,
        string? searchField,
        string? searchContent,
        string? selectField,
        string? selectFieldArrow,
        int? intPage,
        int? pageShowNum,
        CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim)) return Forbid();

        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "ResourceID";
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
            bStatus1, appCode1, resourceType1, menuGroupCode1, sf, sc, sortField, sortArrow, page, pSize, ct);

        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = FunctionLimitUi.CanDelete(lim, false);
        ViewBag.BStatus1 = bStatus1 ?? "";
        ViewBag.AppCode1 = appCode1 ?? "";
        ViewBag.ResourceType1 = resourceType1 ?? "";
        ViewBag.MenuGroupCode1 = menuGroupCode1 ?? "";
        ViewBag.AppCodeFilterOptions = EResourceService.BuildAppCodeFilterOptions();
        ViewBag.SearchField = sf;
        ViewBag.SearchContent = sc;
        ViewBag.SelectField = string.IsNullOrWhiteSpace(sortField) ? "-" : sortField;
        ViewBag.SelectFieldArrow = sortArrow;
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.BStatusOptions = EResourceService.BuildBStatusFilterOptions();
        ViewBag.ResourceTypeFilterOptions = await _service.GetResourceTypeFilterOptionsAsync(ct);
        ViewBag.MenuGroupFilterOptions = await _service.BuildMenuGroupFilterOptionsAsync(appCode1, ct);
        return View(pageRows);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        await PopulateFormOptionsAsync(null, ct);
        return View(new EResourceFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EResourceFormVm model, string? selectNo, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();

        _service.NormalizeFormForSave(model, resourceIdEditable: true);
        if (!ModelState.IsValid) return await ReturnCreateInvalidAsync(model, ct);

        foreach (var (key, msg) in await _service.GetSaveValidationErrorsAsync(model, false, true, ct))
            ModelState.AddModelError(key, msg);
        if (!ModelState.IsValid) return await ReturnCreateInvalidAsync(model, ct);

        try
        {
            await _service.CreateAsync(model, GetMemberId(), ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ResourceId), ex.Message);
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
        var form = _service.ToForm(row);
        await PopulateFormOptionsAsync(form, ct);
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EResourceFormVm model, string? selectNo, string? polistRt, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        ViewBag.PolistRt = polistRt;
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (id != model.DataId) return NotFound();
        var existing = await _service.GetByIdNoTrackAsync(id, ct);
        if (existing == null) return NotFound();

        model.ResourceId = existing.ResourceId;
        model.AppCode = string.IsNullOrWhiteSpace(existing.AppCode) ? "FRAME" : existing.AppCode.Trim();
        _service.NormalizeFormForSave(model, resourceIdEditable: false);
        if (!ModelState.IsValid) return await ReturnEditInvalidAsync(model, ct);

        foreach (var (key, msg) in await _service.GetSaveValidationErrorsAsync(model, true, false, ct))
            ModelState.AddModelError(key, msg);
        if (!ModelState.IsValid) return await ReturnEditInvalidAsync(model, ct);

        await _service.TryUpdateAsync(id, model, GetMemberId(), ct);

        if (string.Equals(selectNo, "0", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        return RedirectToAction(nameof(Edit), new { id, polistRt });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? polistRt, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        await _service.DeleteAsync(id, ct);
        return PolistReturnToken.RedirectToIndex(this, polistRt);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchDelete(int[]? ids, string? polistRt, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (ids != null && ids.Length > 0)
            await _service.BatchDeleteAsync(ids, ct);
        return PolistReturnToken.RedirectToIndex(this, polistRt);
    }

    private async Task<IActionResult> ReturnCreateInvalidAsync(EResourceFormVm model, CancellationToken ct)
    {
        await PopulateFormOptionsAsync(model, ct);
        return View(nameof(Create), model);
    }

    private async Task<IActionResult> ReturnEditInvalidAsync(EResourceFormVm model, CancellationToken ct)
    {
        ViewBag.EditMode = true;
        await PopulateFormOptionsAsync(model, ct);
        return View(nameof(Edit), model);
    }

    private async Task PopulateFormOptionsAsync(EResourceFormVm? model, CancellationToken ct)
    {
        ViewBag.AppCodeOptions = EResourceService.BuildAppCodeFormOptions();
        ViewBag.BStatusOptions = EResourceService.BuildBStatusFormOptions();
        ViewBag.ResourceTypeFormOptions = await _service.GetResourceTypeFormOptionsAsync(ct);
        ViewBag.MenuGroupOptions = await _service.LoadActiveMenuGroupOptionsAsync(model?.AppCode, ct);
    }

    private string GetMemberId()
    {
        var memberId = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
        return _service.Normalize(memberId, 30, "system");
    }
}
