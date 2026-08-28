using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EDepartmentController : Controller
{
    private readonly EDepartmentService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "DeptCode", "DeptCName", "DeptEName"
    };

    public EDepartmentController(EDepartmentService service)
    {
        _service = service;
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string? bStatus1,
        string? deptLevel1,
        string? parentDeptId1,
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

        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "DeptCode";
        var sc = (searchContent ?? "").Trim();
        var pSize = pageShowNum.GetValueOrDefault(16);
        if (pSize <= 0) pSize = 16;
        if (pSize > 200) pSize = 200;
        var page = intPage.GetValueOrDefault(1);
        if (page <= 0) page = 1;
        var sortField = (selectField ?? "").Trim();
        var sortArrow = (selectFieldArrow ?? "0").Trim();

        var (pageRows, total, totalPages, pageOut, parentOptions) = await _service.GetIndexPageAsync(
            bStatus1, deptLevel1, parentDeptId1,
            sf, sc,
            sortField, sortArrow,
            page, pSize, ct);

        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = FunctionLimitUi.CanDelete(lim, false);
        ViewBag.BStatus1 = bStatus1 ?? "";
        ViewBag.DeptLevel1 = deptLevel1 ?? "";
        ViewBag.ParentDeptId1 = parentDeptId1 ?? "";
        ViewBag.SearchField = sf;
        ViewBag.SearchContent = sc;
        ViewBag.SelectField = string.IsNullOrWhiteSpace(sortField) ? "-" : sortField;
        ViewBag.SelectFieldArrow = sortArrow;
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.ParentDeptOptions = parentOptions;
        ViewBag.BStatusOptions = EDepartmentService.BuildBStatusFilterOptions();
        ViewBag.DeptLevelOptions = EDepartmentService.BuildDeptLevelFilterOptions();
        return View(pageRows);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        ViewBag.ParentDeptOptions = await _service.GetParentDeptOptionsAsync(null, ct);
        return View(new EDepartmentFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EDepartmentFormVm model, string? selectNo, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();

        _service.NormalizeFormForSave(model, false, null);
        if (!ModelState.IsValid) return await ReturnCreateInvalid(model, ct);

        foreach (var (key, msg) in await _service.GetSaveValidationErrorsAsync(model, false, ct))
            ModelState.AddModelError(key, msg);
        if (!ModelState.IsValid) return await ReturnCreateInvalid(model, ct);

        try
        {
            await _service.CreateAsync(model, GetMemberId(), ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.DeptCode), ex.Message);
            return await ReturnCreateInvalid(model, ct);
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
        ViewBag.ParentDeptOptions = await _service.GetParentDeptOptionsAsync(id, ct);
        return View(_service.ToForm(row));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EDepartmentFormVm model, string? selectNo, string? polistRt, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        ViewBag.PolistRt = polistRt;
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (id != model.DataId) return NotFound();

        var row = await _service.GetByIdNoTrackAsync(id, ct);
        if (row == null) return NotFound();
        model.DeptCode = row.DeptCode;
        _service.NormalizeFormForSave(model, true, row.DeptCode);
        if (!ModelState.IsValid) return await ReturnEditInvalid(model, ct);

        foreach (var (key, msg) in await _service.GetSaveValidationErrorsAsync(model, true, ct))
            ModelState.AddModelError(key, msg);
        if (!ModelState.IsValid) return await ReturnEditInvalid(model, ct);

        try
        {
            await _service.TryUpdateAsync(id, model, GetMemberId(), ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.DeptCode), ex.Message);
            return await ReturnEditInvalid(model, ct);
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

    private async Task<IActionResult> ReturnCreateInvalid(EDepartmentFormVm model, CancellationToken ct)
    {
        ViewBag.ParentDeptOptions = await _service.GetParentDeptOptionsAsync(null, ct);
        return View(nameof(Create), model);
    }

    private async Task<IActionResult> ReturnEditInvalid(EDepartmentFormVm model, CancellationToken ct)
    {
        ViewBag.ParentDeptOptions = await _service.GetParentDeptOptionsAsync(model.DataId, ct);
        return View(nameof(Edit), model);
    }

    private string GetMemberId()
    {
        var memberId = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
        return _service.Normalize(memberId, 8, "system");
    }
}
