using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class ESubscriptionController : Controller
{
    private readonly ESubscriptionService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Duty", "AppCode", "SubType", "EventCode", "ResourceID", "FunctionLimit", "Remark"
    };

    public ESubscriptionController(ESubscriptionService service)
    {
        _service = service;
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string? bStatus1,
        string? subType1,
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
        var sortArrow = (selectFieldArrow ?? "0").Trim();

        var (pageRows, total, totalPages, pageOut) = await _service.GetIndexPageAsync(
            bStatus1, subType1, sf, sc, sortField, sortArrow, page, pSize, ct);

        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = FunctionLimitUi.CanDelete(lim, false);
        ViewBag.BStatus1 = bStatus1 ?? "";
        ViewBag.SubType1 = subType1 ?? "";
        ViewBag.SearchField = sf;
        ViewBag.SearchContent = sc;
        ViewBag.SelectField = string.IsNullOrWhiteSpace(sortField) ? "-" : sortField;
        ViewBag.SelectFieldArrow = sortArrow;
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.BStatusOptions = ESubscriptionService.BuildBStatusFilterOptions();
        ViewBag.SubTypeOptions = await _service.GetSubTypeFilterOptionsAsync(ct);
        ViewBag.SearchFieldOptions = ESubscriptionService.BuildSearchFieldOptions();
        return View(pageRows);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        await PopulateFormOptionsAsync(null, ct);
        return View(new ESubscriptionFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ESubscriptionFormVm model, string? selectNo, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();

        _service.NormalizeFormForSave(model);
        if (!ModelState.IsValid) return await ReturnInvalid(nameof(Create), model, ct);
        foreach (var (k, m) in await _service.GetSaveValidationErrorsAsync(model, false, ct))
            ModelState.AddModelError(k, m);
        if (!ModelState.IsValid) return await ReturnInvalid(nameof(Create), model, ct);

        await _service.CreateAsync(model, GetMemberId(), ct);
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
        var form = _service.ToForm(row);
        await PopulateFormOptionsAsync(form, ct);
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ESubscriptionFormVm model, string? selectNo, string? polistRt, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        ViewBag.PolistRt = polistRt;
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (id != model.DataId) return NotFound();
        if (await _service.GetByIdNoTrackAsync(id, ct) == null) return NotFound();

        _service.NormalizeFormForSave(model);
        if (!ModelState.IsValid) return await ReturnInvalid(nameof(Edit), model, ct);
        foreach (var (k, m) in await _service.GetSaveValidationErrorsAsync(model, true, ct))
            ModelState.AddModelError(k, m);
        if (!ModelState.IsValid) return await ReturnInvalid(nameof(Edit), model, ct);

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

    private async Task<IActionResult> ReturnInvalid(string view, ESubscriptionFormVm model, CancellationToken ct)
    {
        await PopulateFormOptionsAsync(model, ct);
        return View(view, model);
    }

    private async Task PopulateFormOptionsAsync(ESubscriptionFormVm? model, CancellationToken ct)
    {
        var duties = await _service.LoadActiveDutiesAsync(ct);
        var events = await _service.LoadActiveEventsAsync(model?.AppCode, ct);
        var resources = await _service.LoadActiveResourcesAsync(ct);
        var depts = await _service.LoadActiveDepartmentsAsync(ct);

        ViewBag.DutyOptions = duties.Select(x => (x.Id.ToString(), $"{x.Code} - {x.Name}".Trim())).ToList();
        ViewBag.SubTypeOptions = await _service.GetSubTypeFormOptionsAsync(ct);
        ViewBag.EventOptions = events.Select(x => (x.Code, $"{x.Code} - {x.Name}".Trim())).ToList();
        ViewBag.ResourceOptions = resources.Select(x => (Value: x.Code, Text: x.DisplayText)).ToList();
        var deptOptions = new List<(string Value, string Text)> { ("", "不限") };
        deptOptions.AddRange(depts.Select(x => (x.Id.ToString(), $"{x.Code} - {x.Name}".Trim())));
        ViewBag.DeptOptions = deptOptions;
        ViewBag.AppCodeOptions = ESubscriptionService.BuildAppCodeFormOptions();
        ViewBag.BStatusOptions = ESubscriptionService.BuildBStatusFormOptions();
    }

    private string GetMemberId()
    {
        var memberId = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
        return _service.Normalize(memberId, 30, "system");
    }
}
