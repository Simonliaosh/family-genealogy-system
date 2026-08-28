using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EMenuGroupController : Controller
{
    private readonly EMenuGroupService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppCode", "MenuGroupCode", "MenuGroupName", "Remark"
    };

    public EMenuGroupController(EMenuGroupService service)
    {
        _service = service;
    }

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
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim)) return Forbid();

        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "MenuGroupCode";
        var sc = (searchContent ?? "").Trim();
        var pSize = pageShowNum.GetValueOrDefault(16);
        if (pSize <= 0) pSize = 16;
        if (pSize > 200) pSize = 200;
        var page = intPage.GetValueOrDefault(1);
        if (page <= 0) page = 1;
        var sortField = (selectField ?? "").Trim();
        var sortArrow = (selectFieldArrow ?? "0").Trim();

        var (pageRows, total, totalPages, pageOut) = await _service.GetIndexPageAsync(
            bStatus1, appCode1, sf, sc, sortField, sortArrow, page, pSize, ct);

        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = FunctionLimitUi.CanDelete(lim, false);
        ViewBag.BStatus1 = bStatus1 ?? "";
        ViewBag.AppCode1 = appCode1 ?? "";
        ViewBag.AppCodeFilterOptions = EMenuGroupService.BuildAppCodeFilterOptions();
        ViewBag.SearchField = sf;
        ViewBag.SearchContent = sc;
        ViewBag.SelectField = string.IsNullOrWhiteSpace(sortField) ? "-" : sortField;
        ViewBag.SelectFieldArrow = sortArrow;
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.BStatusOptions = EMenuGroupService.BuildBStatusFilterOptions();
        return View(pageRows);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        PopulateFormOptions(null);
        return View(new EMenuGroupFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EMenuGroupFormVm model, string? selectNo, CancellationToken ct = default)
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
            await _service.CreateAsync(model, GetMemberId(), ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.MenuGroupCode), ex.Message);
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
        var form = _service.ToForm(row);
        PopulateFormOptions(form);
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EMenuGroupFormVm model, string? selectNo, string? polistRt, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        ViewBag.PolistRt = polistRt;
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (string.IsNullOrWhiteSpace(id) || !string.Equals(id.Trim(), model.MenuGroupCode?.Trim(), StringComparison.OrdinalIgnoreCase))
            return NotFound();

        var existing = await _service.GetByCodeNoTrackAsync(id.Trim(), ct);
        if (existing == null) return NotFound();

        _service.NormalizeFormForSave(model, isEdit: true);
        model.MenuGroupCode = existing.MenuGroupCode;
        model.AppCode = string.IsNullOrWhiteSpace(existing.AppCode) ? "FRAME" : existing.AppCode.Trim();
        if (!ModelState.IsValid) return ReturnEditInvalid(model);

        foreach (var (key, msg) in await _service.GetSaveValidationErrorsAsync(model, true, existing.MenuGroupCode, ct))
            ModelState.AddModelError(key, msg);
        if (!ModelState.IsValid) return ReturnEditInvalid(model);

        await _service.TryUpdateAsync(existing.MenuGroupCode, model, GetMemberId(), ct);
        if (string.Equals(selectNo, "0", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        return RedirectToAction(nameof(Edit), new { id = existing.MenuGroupCode, polistRt });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(Index));
        await _service.DeleteAsync(id.Trim(), ct);
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

    private IActionResult ReturnCreateInvalid(EMenuGroupFormVm model)
    {
        PopulateFormOptions(model);
        return View(nameof(Create), model);
    }

    private IActionResult ReturnEditInvalid(EMenuGroupFormVm model)
    {
        ViewBag.EditMode = true;
        PopulateFormOptions(model);
        return View(nameof(Edit), model);
    }

    private void PopulateFormOptions(EMenuGroupFormVm? model)
    {
        ViewBag.AppCodeOptions = EMenuGroupService.BuildAppCodeFormOptions();
        ViewBag.BStatusOptions = EMenuGroupService.BuildBStatusFormOptions();
    }

    private string GetMemberId()
    {
        var memberId = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
        return _service.Normalize(memberId, 30, "system");
    }
}
