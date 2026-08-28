using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EMemberController : Controller
{
    private readonly EMemberService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "MemberID", "MemberName", "Remark"
    };

    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "MemberID", "MemberName", "DeptID", "PosID", "CreateDate"
    };

    public EMemberController(EMemberService service)
    {
        _service = service;
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string? deptId1,
        string? posId1,
        string? sex1,
        string? perGrade1,
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

        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "MemberID";
        var sc = (searchContent ?? "").Trim();
        var pSize = pageShowNum.GetValueOrDefault(16);
        if (pSize <= 0) pSize = 16;
        if (pSize > 200) pSize = 200;
        var page = intPage.GetValueOrDefault(1);
        if (page <= 0) page = 1;
        var sortField = (selectField ?? "").Trim();
        if (!SortWhitelist.Contains(sortField)) sortField = "";
        var sortArrow = (selectFieldArrow ?? "0").Trim();

        var (pageRows, total, totalPages, pageOut, deptOpts, posOpts) = await _service.GetIndexPageAsync(
            deptId1, posId1, sex1, perGrade1,
            sf, sc, sortField, sortArrow, page, pSize, ct);

        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = FunctionLimitUi.CanDelete(lim, false);
        ViewBag.DeptId1 = deptId1 ?? "";
        ViewBag.PosId1 = posId1 ?? "";
        ViewBag.Sex1 = sex1 ?? "";
        ViewBag.PerGrade1 = perGrade1 ?? "";
        ViewBag.DeptFilterOptions = deptOpts;
        ViewBag.PosFilterOptions = posOpts;
        ViewBag.SexFilterOptions = await _service.GetSexFilterOptionsAsync(ct);
        ViewBag.PerGradeFilterOptions = await _service.GetPerGradeFilterOptionsAsync(ct);
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
    public async Task<IActionResult> Details(int id, string? polistRt = null, CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim) || !FunctionLimitUi.CanView(lim, false)) return Forbid();
        var vm = await _service.GetDetailVmAsync(id, ct);
        if (vm == null) return NotFound();
        ViewBag.PolistRt = polistRt;
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        await FillFormLookupsAsync(ct);
        return View(new EMemberFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EMemberFormVm model, string? selectNo, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();

        _service.NormalizeFormForSave(model, false, null);
        if (!ModelState.IsValid) return await ReturnCreateInvalidAsync(model, ct);

        foreach (var (k, m) in await _service.GetSaveValidationErrorsAsync(model, false, ct))
            ModelState.AddModelError(k, m);
        if (!ModelState.IsValid) return await ReturnCreateInvalidAsync(model, ct);

        try
        {
            await _service.CreateAsync(model, GetMemberId(), ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.MemberId), ex.Message);
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
        await FillFormLookupsAsync(ct);
        return View(_service.ToForm(row));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EMemberFormVm model, string? selectNo, string? polistRt, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        ViewBag.PolistRt = polistRt;
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (id != model.DataId) return NotFound();

        var row = await _service.GetByIdNoTrackAsync(id, ct);
        if (row == null) return NotFound();
        model.MemberId = row.MemberId;
        _service.NormalizeFormForSave(model, true, row.MemberId);
        if (!ModelState.IsValid) return await ReturnEditInvalidAsync(model, ct);

        foreach (var (k, m) in await _service.GetSaveValidationErrorsAsync(model, true, ct))
            ModelState.AddModelError(k, m);
        if (!ModelState.IsValid) return await ReturnEditInvalidAsync(model, ct);

        try
        {
            await _service.TryUpdateAsync(id, model, GetMemberId(), ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.MemberId), ex.Message);
            return await ReturnEditInvalidAsync(model, ct);
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

    private async Task FillFormLookupsAsync(CancellationToken ct)
    {
        ViewBag.DeptOptions = await _service.GetDeptOptionsForFormAsync(ct);
        ViewBag.PosOptions = await _service.GetPosOptionsForFormAsync(ct);
        ViewBag.SexFormOptions = await _service.GetSexFormOptionsAsync(ct);
        ViewBag.PerGradeFormOptions = await _service.GetPerGradeFormOptionsAsync(ct);
        ViewBag.HealthFormOptions = await _service.GetHealthFormOptionsAsync(ct);
        ViewBag.ELevelFormOptions = await _service.GetELevelFormOptionsAsync(ct);
    }

    private async Task<IActionResult> ReturnCreateInvalidAsync(EMemberFormVm model, CancellationToken ct)
    {
        await FillFormLookupsAsync(ct);
        return View(nameof(Create), model);
    }

    private async Task<IActionResult> ReturnEditInvalidAsync(EMemberFormVm model, CancellationToken ct)
    {
        await FillFormLookupsAsync(ct);
        return View(nameof(Edit), model);
    }

    private string GetMemberId()
    {
        var memberId = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
        return _service.Normalize(memberId, 8, "system");
    }
}
