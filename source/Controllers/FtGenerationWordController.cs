using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class FtGenerationWordController : Controller
{
    private readonly FtGenerationWordService _service;
    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase) { "Word", "Remark" };
    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase) { "SeqNo", "Word", "BStatus", "AmendDate" };

    public FtGenerationWordController(FtGenerationWordService service) => _service = service;

    [HttpGet, HttpPost]
    public async Task<IActionResult> Index(string? bStatus1, string? searchField, string? searchContent,
        string? selectField, string? selectFieldArrow, int? intPage, int? pageShowNum, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "Word";
        var pSize = pageShowNum.GetValueOrDefault(16);
        var page = intPage.GetValueOrDefault(1);
        var sortField = (selectField ?? "").Trim();
        if (!SortWhitelist.Contains(sortField)) sortField = "SeqNo";
        var sortArrow = (selectFieldArrow ?? "0").Trim();
        var (pageRows, total, totalPages, pageOut) = await _service.GetIndexPageAsync(
            bStatus1, sf, (searchContent ?? "").Trim(), sortField, sortArrow, page, pSize, ct);
        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = FunctionLimitUi.CanDelete(lim, false);
        ViewBag.BStatus1 = bStatus1 ?? "";
        ViewBag.SearchField = sf;
        ViewBag.SearchContent = (searchContent ?? "").Trim();
        ViewBag.SelectField = sortField;
        ViewBag.SelectFieldArrow = sortArrow;
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pSize <= 0 ? 16 : pSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        ViewBag.BStatusOptions = EDictTypeService.BuildBStatusFilterOptions();
        return View(pageRows);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        ViewBag.BStatusOptions = EDictTypeService.BuildBStatusFormOptions();
        return View(new FtGenWordFormVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FtGenWordFormVm model, string? selectNo, CancellationToken ct)
    {
        if (selectNo == "9") return RedirectToAction(nameof(Index));
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        _service.Normalize(model);
        if (string.IsNullOrWhiteSpace(model.Word))
            ModelState.AddModelError(nameof(model.Word), "字辈不能为空。");
        if (!ModelState.IsValid)
        {
            ViewBag.BStatusOptions = EDictTypeService.BuildBStatusFormOptions();
            return View(model);
        }
        await _service.CreateAsync(model, FtClaims.Operator(User), ct);
        if (selectNo == "0") return RedirectToAction(nameof(Index));
        return RedirectToAction(nameof(Create));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? polistRt, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var row = await _service.GetAsync(id, ct);
        if (row == null) return NotFound();
        ViewBag.PolistRt = polistRt;
        ViewBag.BStatusOptions = EDictTypeService.BuildBStatusFormOptions();
        return View(_service.ToForm(row));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FtGenWordFormVm model, string? selectNo, string? polistRt, CancellationToken ct)
    {
        if (selectNo == "9") return PolistReturnToken.RedirectToIndex(this, polistRt);
        ViewBag.PolistRt = polistRt;
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        _service.Normalize(model);
        if (!ModelState.IsValid)
        {
            ViewBag.BStatusOptions = EDictTypeService.BuildBStatusFormOptions();
            return View(model);
        }
        await _service.TryUpdateAsync(id, model, FtClaims.Operator(User), ct);
        if (selectNo == "0") return PolistReturnToken.RedirectToIndex(this, polistRt);
        return RedirectToAction(nameof(Edit), new { id, polistRt });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        await _service.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchDelete(int[]? ids, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (ids is { Length: > 0 }) await _service.BatchDeleteAsync(ids, ct);
        return RedirectToAction(nameof(Index));
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
