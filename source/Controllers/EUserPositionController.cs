using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EUserPositionController : Controller
{
    private readonly EUserPositionService _service;

    public EUserPositionController(EUserPositionService service)
    {
        _service = service;
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string? bStatus1,
        string? searchContent,
        string? selectField,
        string? selectFieldArrow,
        int? intPage,
        int? pageShowNum,
        CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim)) return Forbid();

        var sc = (searchContent ?? "").Trim();
        var pSize = pageShowNum.GetValueOrDefault(16);
        if (pSize <= 0) pSize = 16;
        if (pSize > 200) pSize = 200;
        var page = intPage.GetValueOrDefault(1);
        if (page <= 0) page = 1;
        var sortField = (selectField ?? "").Trim();
        var sortArrow = (selectFieldArrow ?? "0").Trim();

        var (pageRows, total, totalPages, pageOut) = await _service.GetIndexPageAsync(
            bStatus1, sc, sortField, sortArrow, page, pSize, ct);

        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = FunctionLimitUi.CanDelete(lim, false);
        ViewBag.BStatus1 = bStatus1 ?? "";
        ViewBag.SearchContent = sc;
        ViewBag.SelectField = string.IsNullOrWhiteSpace(sortField) ? "-" : sortField;
        ViewBag.SelectFieldArrow = sortArrow;
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.BStatusOptions = EUserPositionService.BuildBStatusFilterOptions();
        return View(pageRows);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        await PopulateFormOptionsAsync(ct);
        return View(new EUserPositionFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EUserPositionFormVm model, string? selectNo, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();

        if (!ModelState.IsValid) return await ReturnFormInvalid(nameof(Create), model, ct);
        foreach (var (k, m) in await _service.GetSaveValidationErrorsAsync(model, false, ct))
            ModelState.AddModelError(k, m);
        if (!ModelState.IsValid) return await ReturnFormInvalid(nameof(Create), model, ct);

        try
        {
            await _service.CreateAsync(model, GetMemberId(), ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.PosId), ex.Message);
            return await ReturnFormInvalid(nameof(Create), model, ct);
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
        return View(_service.ToForm(row));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EUserPositionFormVm model, string? selectNo, string? polistRt, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        ViewBag.PolistRt = polistRt;
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (id != model.DataId) return NotFound();
        if (await _service.GetByIdNoTrackAsync(id, ct) == null) return NotFound();

        if (!ModelState.IsValid) return await ReturnFormInvalid(nameof(Edit), model, ct);
        foreach (var (k, m) in await _service.GetSaveValidationErrorsAsync(model, true, ct))
            ModelState.AddModelError(k, m);
        if (!ModelState.IsValid) return await ReturnFormInvalid(nameof(Edit), model, ct);

        try
        {
            await _service.TryUpdateAsync(id, model, GetMemberId(), ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.PosId), ex.Message);
            return await ReturnFormInvalid(nameof(Edit), model, ct);
        }

        if (string.Equals(selectNo, "0", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        return RedirectToAction(nameof(Edit), new { id, polistRt });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        await _service.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchDelete(int[]? ids, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (ids != null && ids.Length > 0)
            await _service.BatchDeleteAsync(ids, ct);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReturnFormInvalid(string viewName, EUserPositionFormVm model, CancellationToken ct)
    {
        await PopulateFormOptionsAsync(ct);
        return View(viewName, model);
    }

    private async Task PopulateFormOptionsAsync(CancellationToken ct)
    {
        var users = await _service.LoadActiveUsersAsync(ct);
        var depts = await _service.LoadActiveDepartmentsAsync(ct);
        var poses = await _service.LoadActivePositionsAsync(ct);

        ViewBag.UserOptions = users.Select(x => (x.Id.ToString(), $"{x.LoginId} - {x.RealName}".Trim())).ToList();
        ViewBag.DeptOptions = depts.Select(x => (x.Id.ToString(), $"{x.Code} - {x.Name}".Trim())).ToList();
        ViewBag.PosOptions = poses.Select(x => (x.Id.ToString(), $"{x.Code} - {x.Name}".Trim())).ToList();
        ViewBag.BStatusFormOptions = EUserPositionService.BuildBStatusFormOptions();
    }

    private string GetMemberId()
    {
        var memberId = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
        memberId = (memberId ?? "").Trim();
        if (memberId.Length == 0) memberId = "system";
        return memberId.Length <= 8 ? memberId : memberId[..8];
    }
}
