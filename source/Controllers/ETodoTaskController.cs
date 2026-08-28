using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class ETodoTaskController : Controller
{
    private readonly ETodoTaskService _service;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "TodoTitle", "User", "EventCode", "ObjectKey", "ObjectType", "BusinessID"
    };

    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreateTime", "Status", "DataID"
    };

    public ETodoTaskController(ETodoTaskService service)
    {
        _service = service;
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(
        string? status1,
        string? eventCode1,
        string? searchField,
        string? searchContent,
        string? selectField,
        string? selectFieldArrow,
        int? intPage,
        int? pageShowNum,
        CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim) || !FunctionLimitUi.CanView(lim, false)) return Forbid();

        var uid = await ResolveCurrentEUserIdAsync(ct);
        if (!uid.HasValue) return Forbid();

        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "TodoTitle";
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
            uid.Value, status1, eventCode1, sf, sc, sortField, sortArrow, page, pSize, ct);

        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.StatusOptions = await _service.GetStatusFilterOptionsAsync(ct);
        ViewBag.EventCodeOptions = await _service.GetEventCodeFilterOptionsForUserAsync(uid.Value, ct);
        ViewBag.Status1 = status1 ?? "";
        ViewBag.EventCode1 = eventCode1 ?? "";
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
    public async Task<IActionResult> Details(int id, CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim) || !FunctionLimitUi.CanView(lim, false)) return Forbid();
        var uid = await ResolveCurrentEUserIdAsync(ct);
        if (!uid.HasValue) return Forbid();
        var vm = await _service.GetDetailForUserAsync(id, uid.Value, ct);
        if (vm == null) return NotFound();
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        await PopulateHandlerOptionsAsync(ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Handle(ETodoTaskHandleFormVm model, CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim) || !FunctionLimitUi.CanUpdate(lim, false)) return Forbid();
        var uid = await ResolveCurrentEUserIdAsync(ct);
        if (!uid.HasValue) return Forbid();

        if (!ModelState.IsValid)
        {
            var detail = await _service.GetDetailForUserAsync(model.DataId, uid.Value, ct);
            if (detail == null) return NotFound();
            ViewBag.CanUpdate = true;
            ViewBag.HandleError = "请检查表单。";
            await PopulateHandlerOptionsAsync(ct);
            return View("Details", detail);
        }

        var (ok, msg) = await _service.TryHandleAsync(model.DataId, uid.Value, model.NewStatus, model.HandlerDeptId, model.HandlerPosId, ct);
        if (!ok)
        {
            var detail = await _service.GetDetailForUserAsync(model.DataId, uid.Value, ct);
            if (detail == null) return NotFound();
            ViewBag.CanUpdate = true;
            ViewBag.HandleError = msg;
            await PopulateHandlerOptionsAsync(ct);
            return View("Details", detail);
        }

        TempData["HandleMsg"] = msg;
        return RedirectToAction(nameof(Details), new { id = model.DataId });
    }

    private async Task<int?> ResolveCurrentEUserIdAsync(CancellationToken ct)
    {
        var memberId = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value;
        return await _service.ResolveEUserDataIdByMemberIdAsync(memberId, ct);
    }

    private async Task PopulateHandlerOptionsAsync(CancellationToken ct)
    {
        var depts = await _service.LoadActiveDepartmentOptionsAsync(ct);
        ViewBag.HandlerDeptOptions = new List<(string Value, string Text)> { ("", "不指定") }
            .Concat(depts.Select(x => (x.Id.ToString(), x.Display))).ToList();
        var poses = await _service.LoadActivePositionOptionsAsync(ct);
        ViewBag.HandlerPosOptions = new List<(string Value, string Text)> { ("", "不指定") }
            .Concat(poses.Select(x => (x.Id.ToString(), x.Display))).ToList();
    }
}
