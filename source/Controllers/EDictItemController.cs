using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EDictItemController : Controller
{
    private readonly EDictItemService _service;

    public EDictItemController(EDictItemService service) => _service = service;

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string code,
        string? bStatus1,
        string? searchContent,
        string? selectField,
        string? selectFieldArrow,
        int? intPage,
        int? pageShowNum,
        CancellationToken ct = default)
    {
        if (!CanView()) return Forbid();
        var lim = HttpContext.Items["PubFunctionLimit"] as string;

        var typeCode = (code ?? "").Trim();
        if (typeCode.Length == 0) return NotFound();
        var type = await _service.GetTypeAsync(typeCode, ct);
        if (type == null) return NotFound();

        var pSize = pageShowNum.GetValueOrDefault(16);
        if (pSize <= 0) pSize = 16;
        if (pSize > 200) pSize = 200;
        var page = intPage.GetValueOrDefault(1);
        if (page <= 0) page = 1;

        var (pageRows, total, totalPages, pageOut) = await _service.GetIndexPageAsync(
            typeCode, bStatus1, (searchContent ?? "").Trim(),
            (selectField ?? "").Trim(), (selectFieldArrow ?? "0").Trim(), page, pSize, ct);

        ViewBag.TypeCode = typeCode;
        ViewBag.TypeName = type.DictTypeName;
        ViewBag.TypeIsEditable = type.IsEditable;
        ViewBag.TypeIsSystem = type.IsSystem;
        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false) && type.IsEditable;
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = FunctionLimitUi.CanDelete(lim, false);
        ViewBag.BStatus1 = bStatus1 ?? "";
        ViewBag.SearchContent = searchContent ?? "";
        ViewBag.SelectField = string.IsNullOrWhiteSpace(selectField) ? "-" : selectField;
        ViewBag.SelectFieldArrow = (selectFieldArrow ?? "0").Trim();
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.BStatusOptions = EDictItemService.BuildBStatusFilterOptions();
        return View(pageRows);
    }

    [HttpGet]
    public async Task<IActionResult> Create(string code, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var type = await _service.GetTypeAsync(code, ct);
        if (type == null || !type.IsEditable) return Forbid();
        ViewBag.TypeName = type.DictTypeName;
        PopulateFormOptions();
        return View(new EDictItemFormVm { DictTypeCode = type.DictTypeCode });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EDictItemFormVm model, string? selectNo, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index), new { code = model.DictTypeCode });
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();

        _service.NormalizeFormForSave(model, isEdit: false);
        if (!ModelState.IsValid) return await ReturnCreateInvalidAsync(model, ct);

        foreach (var (key, msg) in await _service.GetSaveValidationErrorsAsync(model, false, null, ct))
            ModelState.AddModelError(key, msg);
        if (!ModelState.IsValid) return await ReturnCreateInvalidAsync(model, ct);

        try
        {
            await _service.CreateAsync(model, ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ItemCode), ex.Message);
            return await ReturnCreateInvalidAsync(model, ct);
        }

        if (string.Equals(selectNo, "0", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index), new { code = model.DictTypeCode });
        return RedirectToAction(nameof(Create), new { code = model.DictTypeCode });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? polistRt = null, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var row = await _service.GetByIdNoTrackAsync(id, ct);
        if (row == null) return NotFound();
        var type = await _service.GetTypeAsync(row.DictTypeCode, ct);
        ViewBag.PolistRt = polistRt;
        ViewBag.EditMode = true;
        ViewBag.IsSystem = row.IsSystem;
        ViewBag.TypeName = type?.DictTypeName ?? row.DictTypeCode;
        PopulateFormOptions();
        return View(_service.ToForm(row));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EDictItemFormVm model, string? selectNo, string? polistRt, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index), new { code = model.DictTypeCode });
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();

        var existing = await _service.GetByIdNoTrackAsync(id, ct);
        if (existing == null) return NotFound();

        _service.NormalizeFormForSave(model, isEdit: true);
        model.DataId = existing.DataId;
        model.DictTypeCode = existing.DictTypeCode;
        model.ItemCode = existing.ItemCode;
        ViewBag.EditMode = true;
        ViewBag.IsSystem = existing.IsSystem;
        if (!ModelState.IsValid) return ReturnEditInvalid(model);

        foreach (var (key, msg) in await _service.GetSaveValidationErrorsAsync(model, true, existing.DataId, ct))
            ModelState.AddModelError(key, msg);
        if (!ModelState.IsValid) return ReturnEditInvalid(model);

        try
        {
            await _service.TryUpdateAsync(id, model, ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return ReturnEditInvalid(model);
        }

        if (string.Equals(selectNo, "0", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index), new { code = model.DictTypeCode });
        return RedirectToAction(nameof(Edit), new { id, polistRt });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string code, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        try { await _service.DeleteAsync(id, ct); }
        catch (InvalidOperationException) { }
        return RedirectToAction(nameof(Index), new { code });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchDelete(string code, int[]? ids, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (ids is { Length: > 0 })
            await _service.BatchDeleteAsync(ids.Distinct().ToList(), ct);
        return RedirectToAction(nameof(Index), new { code });
    }

    private async Task<IActionResult> ReturnCreateInvalidAsync(EDictItemFormVm model, CancellationToken ct)
    {
        var type = await _service.GetTypeAsync(model.DictTypeCode, ct);
        ViewBag.TypeName = type?.DictTypeName;
        PopulateFormOptions();
        return View(nameof(Create), model);
    }

    private IActionResult ReturnEditInvalid(EDictItemFormVm model)
    {
        PopulateFormOptions();
        return View(nameof(Edit), model);
    }

    private void PopulateFormOptions() =>
        ViewBag.BStatusOptions = EDictItemService.BuildBStatusFormOptions();

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
