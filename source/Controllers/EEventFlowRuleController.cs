using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EEventFlowRuleController : Controller
{
    private readonly EEventFlowRuleService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "RuleCode", "RuleName", "AppCode", "CurrentEvent", "NextEvent", "ConditionExpr"
    };

    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "DataID", "BStatus", "RuleCode", "CurrentEvent", "NextEvent"
    };

    public EEventFlowRuleController(EEventFlowRuleService service)
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

        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "RuleCode";
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
        ViewBag.BStatusOptions = EEventFlowRuleService.BuildBStatusFilterOptions();
        return View(pageRows);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        await PopulateFormOptionsAsync(null, ct);
        return View(new EEventFlowRuleFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EEventFlowRuleFormVm model, string? selectNo, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();

        _service.NormalizeFormForSave(model);
        if (!ModelState.IsValid) return await ReturnCreateInvalidAsync(model, ct);

        foreach (var (k, m) in await _service.GetSaveValidationErrorsAsync(model, false, ct))
            ModelState.AddModelError(k, m);
        if (!ModelState.IsValid) return await ReturnCreateInvalidAsync(model, ct);

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
    public async Task<IActionResult> Edit(int id, EEventFlowRuleFormVm model, string? selectNo, string? polistRt, CancellationToken ct = default)
    {
        if (string.Equals(selectNo, "9", StringComparison.OrdinalIgnoreCase))
            return PolistReturnToken.RedirectToIndex(this, polistRt);
        ViewBag.PolistRt = polistRt;
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (id != model.DataId) return NotFound();
        if (await _service.GetByIdNoTrackAsync(id, ct) == null) return NotFound();

        _service.NormalizeFormForSave(model);
        if (!ModelState.IsValid) return await ReturnEditInvalidAsync(model, ct);

        foreach (var (k, m) in await _service.GetSaveValidationErrorsAsync(model, true, ct))
            ModelState.AddModelError(k, m);
        if (!ModelState.IsValid) return await ReturnEditInvalidAsync(model, ct);

        await _service.TryUpdateAsync(id, model, GetMemberId(), ct);

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

    private async Task PopulateFormOptionsAsync(EEventFlowRuleFormVm? model, CancellationToken ct)
    {
        var appCode = model?.AppCode ?? "FRAME";
        var events = await _service.LoadActiveEventOptionsAsync(appCode, ct);
        ViewBag.EventOptions = events.Select(x => (x.Code, x.Display)).ToList();
        var depts = await _service.LoadActiveDepartmentOptionsAsync(ct);
        var deptOpts = new List<(string Value, string Text)> { ("", "（不指定）") };
        deptOpts.AddRange(depts.Select(x => (x.Id.ToString(), x.Display)));
        ViewBag.DeptOptions = deptOpts;
        var poses = await _service.LoadActivePositionOptionsAsync(ct);
        var posOpts = new List<(string Value, string Text)> { ("", "（不指定）") };
        posOpts.AddRange(poses.Select(x => (x.Id.ToString(), x.Display)));
        ViewBag.PosOptions = posOpts;
        var duties = await _service.LoadActiveDutyOptionsAsync(ct);
        var dutyOpts = new List<(string Value, string Text)> { ("", "（不指定）") };
        dutyOpts.AddRange(duties.Select(x => (x.Id.ToString(), x.Display)));
        ViewBag.DutyOptions = dutyOpts;
        var users = await _service.LoadActiveUserOptionsAsync(ct);
        var userOpts = new List<(string Value, string Text)> { ("", "（不指定）") };
        userOpts.AddRange(users.Select(x => (x.Id.ToString(), x.Display)));
        ViewBag.UserOptions = userOpts;
        ViewBag.BStatusOptions = EEventFlowRuleService.BuildBStatusFormOptions();
        ViewBag.ActionTypeOptions = await _service.GetActionTypeFormOptionsAsync(ct);
        ViewBag.TargetResolveOptions = await _service.GetTargetResolveFormOptionsAsync(ct);
        ViewBag.HandleModeOptions = await _service.GetHandleModeFormOptionsAsync(ct);
        ViewBag.AppCodeOptions = EEventFlowRuleService.BuildAppCodeFormOptions();
    }

    private async Task<IActionResult> ReturnCreateInvalidAsync(EEventFlowRuleFormVm model, CancellationToken ct)
    {
        await PopulateFormOptionsAsync(model, ct);
        return View(nameof(Create), model);
    }

    private async Task<IActionResult> ReturnEditInvalidAsync(EEventFlowRuleFormVm model, CancellationToken ct)
    {
        await PopulateFormOptionsAsync(model, ct);
        return View(nameof(Edit), model);
    }

    private string GetMemberId()
    {
        var memberId = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
        return _service.Normalize(memberId, 30, "system");
    }
}
