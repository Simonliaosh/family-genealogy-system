using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EResourcePermissionController : Controller
{
    private readonly EResourcePermissionService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Duty", "ResourceID", "ResourceName"
    };

    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "DutyID", "ResourceID", "BStatus"
    };

    public EResourcePermissionController(EResourcePermissionService service)
    {
        _service = service;
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string? bStatus1,
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

        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "Duty";
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
            bStatus1, sf, sc, sortField, sortArrow, page, pSize, ct);

        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = FunctionLimitUi.CanDelete(lim, false);
        ViewBag.BStatus1 = bStatus1 ?? "";
        ViewBag.SearchField = sf;
        ViewBag.SearchContent = sc;
        ViewBag.SelectField = string.IsNullOrWhiteSpace(sortField) ? "-" : sortField;
        ViewBag.SelectFieldArrow = sortArrow;
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.BStatusOptions = EResourcePermissionService.BuildBStatusFilterOptions();
        return View(pageRows);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        await PopulateFormOptionsAsync(ct);
        ViewBag.LockDutyResource = false;
        return View(new EResourcePermissionFormVm { CanQuery = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EResourcePermissionFormVm model, string? selectNo, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();

        _service.NormalizeFormForSave(model, true);
        if (!ModelState.IsValid) return await ReturnCreateInvalidAsync(model, ct);

        foreach (var (k, m) in await _service.GetSaveValidationErrorsAsync(model, false, true, ct))
            ModelState.AddModelError(k, m);
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
        await PopulateFormOptionsAsync(ct);
        ViewBag.LockDutyResource = true;
        ViewBag.DutyDisplay = await _service.GetDutyDisplayAsync(row.DutyId, ct) ?? $"#{row.DutyId}";
        ViewBag.ResourceDisplay = await _service.GetResourceDisplayAsync(row.ResourceId, ct) ?? row.ResourceId;
        return View(_service.ToForm(row));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EResourcePermissionFormVm model, string? selectNo, string? polistRt, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        ViewBag.PolistRt = polistRt;
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (id != model.DataId) return NotFound();
        var existing = await _service.GetByIdNoTrackAsync(id, ct);
        if (existing == null) return NotFound();

        model.DutyId = existing.DutyId;
        model.ResourceId = existing.ResourceId;
        ModelState.Remove(nameof(model.DutyId));
        ModelState.Remove(nameof(model.ResourceId));
        _service.NormalizeFormForSave(model, false);
        if (!ModelState.IsValid) return await ReturnEditInvalidAsync(model, existing, ct);

        foreach (var (k, m) in await _service.GetSaveValidationErrorsAsync(model, true, false, ct))
            ModelState.AddModelError(k, m);
        if (!ModelState.IsValid) return await ReturnEditInvalidAsync(model, existing, ct);

        try
        {
            await _service.TryUpdateAsync(id, model, GetMemberId(), ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ResourceId), ex.Message);
            return await ReturnEditInvalidAsync(model, existing, ct);
        }

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

    private async Task PopulateFormOptionsAsync(CancellationToken ct)
    {
        var duties = await _service.LoadActiveDutyOptionsAsync(ct);
        ViewBag.DutyOptions = duties.Select(x => (x.Id.ToString(), x.Display)).ToList();
        var resources = await _service.LoadActiveResourceOptionsAsync(ct);
        ViewBag.ResourceOptions = resources.Select(x => (x.Code, x.Display)).ToList();
        ViewBag.BStatusOptions = EResourcePermissionService.BuildBStatusFormOptions();
    }

    private async Task<IActionResult> ReturnCreateInvalidAsync(EResourcePermissionFormVm model, CancellationToken ct)
    {
        await PopulateFormOptionsAsync(ct);
        ViewBag.LockDutyResource = false;
        return View(nameof(Create), model);
    }

    private async Task<IActionResult> ReturnEditInvalidAsync(EResourcePermissionFormVm model, EResourcePermission existing, CancellationToken ct)
    {
        await PopulateFormOptionsAsync(ct);
        ViewBag.LockDutyResource = true;
        ViewBag.DutyDisplay = await _service.GetDutyDisplayAsync(existing.DutyId, ct) ?? $"#{existing.DutyId}";
        ViewBag.ResourceDisplay = await _service.GetResourceDisplayAsync(existing.ResourceId, ct) ?? existing.ResourceId;
        return View(nameof(Edit), model);
    }

    private string GetMemberId()
    {
        var memberId = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
        return _service.Normalize(memberId, 8, "system");
    }
}
