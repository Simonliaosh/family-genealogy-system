using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EDictTypeController : Controller
{
    private readonly EDictTypeService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "DictTypeCode", "DictTypeName", "Remark"
    };

    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppCode", "DictTypeCode", "DictTypeName", "BStatus", "IsSystem", "IsEditable", "ItemCount", "AmendDate"
    };

    public EDictTypeController(EDictTypeService service) => _service = service;

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string? bStatus1,
        string? appCode1,
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

        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "DictTypeCode";
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
            bStatus1, appCode1, sf, sc, sortField, sortArrow, page, pSize, ct);

        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = FunctionLimitUi.CanDelete(lim, false);
        ViewBag.BStatus1 = bStatus1 ?? "";
        ViewBag.AppCode1 = appCode1 ?? "";
        ViewBag.AppCodeFilterOptions = EDictTypeService.BuildAppCodeFilterOptions();
        ViewBag.SearchField = sf;
        ViewBag.SearchContent = sc;
        ViewBag.SelectField = string.IsNullOrWhiteSpace(sortField) ? "-" : sortField;
        ViewBag.SelectFieldArrow = sortArrow;
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.BStatusOptions = EDictTypeService.BuildBStatusFilterOptions();
        return View(pageRows);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        PopulateFormOptions();
        return View(new EDictTypeFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EDictTypeFormVm model, string? selectNo, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();

        _service.NormalizeFormForSave(model, isEdit: false);
        if (!ModelState.IsValid) return ReturnCreateInvalid(model);

        foreach (var (key, msg) in await _service.GetSaveValidationErrorsAsync(model, false, null, ct))
            ModelState.AddModelError(key, msg);
        if (!ModelState.IsValid) return ReturnCreateInvalid(model);

        try
        {
            await _service.CreateAsync(model, ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.DictTypeCode), ex.Message);
            return ReturnCreateInvalid(model);
        }

        if (string.Equals(selectNo, "0", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));
        return RedirectToAction(nameof(Create));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id, string? polistRt = null, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        var row = await _service.GetByCodeNoTrackAsync(id.Trim(), ct);
        if (row == null) return NotFound();
        ViewBag.PolistRt = polistRt;
        ViewBag.EditMode = true;
        ViewBag.IsSystem = row.IsSystem;
        PopulateFormOptions();
        return View(_service.ToForm(row));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EDictTypeFormVm model, string? selectNo, string? polistRt, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        ViewBag.PolistRt = polistRt;
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var existing = await _service.GetByCodeNoTrackAsync(id.Trim(), ct);
        if (existing == null) return NotFound();

        _service.NormalizeFormForSave(model, isEdit: true);
        model.DictTypeCode = existing.DictTypeCode;
        model.AppCode = existing.AppCode.Trim();
        ViewBag.EditMode = true;
        ViewBag.IsSystem = existing.IsSystem;
        if (!ModelState.IsValid) return ReturnEditInvalid(model);

        foreach (var (key, msg) in await _service.GetSaveValidationErrorsAsync(model, true, existing.DictTypeCode, ct))
            ModelState.AddModelError(key, msg);
        if (!ModelState.IsValid) return ReturnEditInvalid(model);

        try
        {
            await _service.TryUpdateAsync(existing.DictTypeCode, model, ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return ReturnEditInvalid(model);
        }

        if (string.Equals(selectNo, "0", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        return RedirectToAction(nameof(Edit), new { id = existing.DictTypeCode, polistRt });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (!string.IsNullOrWhiteSpace(id))
        {
            try { await _service.DeleteAsync(id.Trim(), ct); }
            catch (InvalidOperationException) { /* 可配合 TempData，此处简化 */ }
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchDelete(string[]? ids, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (ids is { Length: > 0 })
        {
            var vals = ids.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            await _service.BatchDeleteAsync(vals, ct);
        }
        return RedirectToAction(nameof(Index));
    }

    private IActionResult ReturnCreateInvalid(EDictTypeFormVm model)
    {
        PopulateFormOptions();
        return View(nameof(Create), model);
    }

    private IActionResult ReturnEditInvalid(EDictTypeFormVm model)
    {
        PopulateFormOptions();
        return View(nameof(Edit), model);
    }

    private void PopulateFormOptions()
    {
        ViewBag.AppCodeOptions = EDictTypeService.BuildAppCodeFormOptions();
        ViewBag.BStatusOptions = EDictTypeService.BuildBStatusFormOptions();
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
