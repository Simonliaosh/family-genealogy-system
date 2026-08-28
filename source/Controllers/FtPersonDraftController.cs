using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class FtPersonDraftController : Controller
{
    private readonly FtPersonDraftService _service;

    public FtPersonDraftController(FtPersonDraftService service) => _service = service;

    [HttpGet, HttpPost]
    public async Task<IActionResult> Index(string? searchContent, int? intPage, int? pageShowNum, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        var (rows, total, pages, p) = await _service.GetIndexPageAsync(uid, "FullName", searchContent ?? "", "AmendDate", "1",
            intPage.GetValueOrDefault(1), pageShowNum.GetValueOrDefault(16), ct);
        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = FunctionLimitUi.CanDelete(lim, false);
        ViewBag.SearchContent = searchContent ?? "";
        ViewBag.IntPage = p;
        ViewBag.PageShowNum = pageShowNum.GetValueOrDefault(16);
        ViewBag.TotalPages = pages;
        ViewBag.TotalRecords = total;
        ViewBag.SearchField = "FullName";
        ViewBag.SelectField = "AmendDate";
        ViewBag.SelectFieldArrow = "1";
        return View(rows);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        ViewBag.RelationOptions = FtPersonDraftService.RelationOptions();
        return View(new FtDraftFormVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FtDraftFormVm model, string? selectNo, CancellationToken ct)
    {
        if (selectNo == "9") return RedirectToAction(nameof(Index));
        var uid = FtClaims.UserId(User) ?? 0;
        _service.Normalize(model);
        var id = await _service.SaveDraftAsync(model, uid, FtClaims.Operator(User), ct);
        if (selectNo == "0") return RedirectToAction(nameof(Index));
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? polistRt, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        var row = await _service.GetAsync(id, uid, ct);
        if (row == null) return NotFound();
        ViewBag.PolistRt = polistRt;
        ViewBag.RelationOptions = FtPersonDraftService.RelationOptions();
        ViewBag.CanComplete = row.AuditStatus != "PASS";
        return View(_service.ToForm(row));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FtDraftFormVm model, string? selectNo, string? polistRt, CancellationToken ct)
    {
        if (selectNo == "9") return PolistReturnToken.RedirectToIndex(this, polistRt);
        model.DataId = id;
        var uid = FtClaims.UserId(User) ?? 0;
        _service.Normalize(model);
        await _service.SaveDraftAsync(model, uid, FtClaims.Operator(User), ct);
        if (selectNo == "0") return PolistReturnToken.RedirectToIndex(this, polistRt);
        return RedirectToAction(nameof(Edit), new { id, polistRt });
    }

    /// <summary>入档入口：有主谱疑似则先确认；否则直接建档。草稿内容不改，仅改状态。</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        try
        {
            var preview = await _service.BuildArchiveConfirmAsync(id, uid, ct);
            if (preview.Hints.Count > 0)
                return RedirectToAction(nameof(ArchiveConfirm), new { id });

            var (pid, msg) = await _service.ArchiveAsNewAsync(id, uid, FtClaims.Operator(User), ct);
            TempData["SuccessMessage"] = msg;
            return RedirectToAction("Edit", "FtPerson", new { id = pid });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ArchiveConfirm(int id, string? ex, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        try
        {
            var vm = await _service.BuildArchiveConfirmAsync(id, uid, ct);
            var skip = ParseExcludeIds(ex);
            if (skip.Count > 0)
                vm.Hints = vm.Hints.Where(h => !skip.Contains(h.PersonId)).ToList();
            ViewBag.ExcludeCsv = string.Join(",", skip);

            if (vm.Hints.Count == 0)
            {
                var (pid, msg) = await _service.ArchiveAsNewAsync(id, uid, FtClaims.Operator(User), ct);
                TempData["SuccessMessage"] = msg;
                return RedirectToAction("Edit", "FtPerson", new { id = pid });
            }
            return View(vm);
        }
        catch (InvalidOperationException ioe)
        {
            TempData["ErrorMessage"] = ioe.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    /// <summary>确认与已入档人为同一人，按此人加入已有家族链。</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveJoin(int id, int targetId, byte? level, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        try
        {
            var (pid, msg, linkId) = await _service.ArchiveJoinChainAsync(id, targetId, level, uid, FtClaims.Operator(User), ct);
            TempData["SuccessMessage"] = msg;
            if (linkId > 0)
                return RedirectToAction("ApplyQr", "FtPersonLink", new { id = linkId });
            return RedirectToAction("Edit", "FtPerson", new { id = pid });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(ArchiveConfirm), new { id });
        }
    }

    /// <summary>不是同一人：独立写入档案。</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveAsNew(int id, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        try
        {
            var (pid, msg) = await _service.ArchiveAsNewAsync(id, uid, FtClaims.Operator(User), ct);
            TempData["SuccessMessage"] = msg;
            return RedirectToAction("Edit", "FtPerson", new { id = pid });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    /// <summary>本页排除某命中（入档前尚无人物 ID，仅会话级过滤）；若已无剩余命中则独立建档。</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ArchiveExclude(int id, int targetId, string? ex)
    {
        var skip = ParseExcludeIds(ex);
        skip.Add(targetId);
        return RedirectToAction(nameof(ArchiveConfirm), new { id, ex = string.Join(",", skip.OrderBy(x => x)) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        try
        {
            await _service.SoftDeleteAsync(id, uid, ct);
            TempData["SuccessMessage"] = "已删除。";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private static HashSet<int> ParseExcludeIds(string? ex)
    {
        var set = new HashSet<int>();
        if (string.IsNullOrWhiteSpace(ex)) return set;
        foreach (var part in ex.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var id) && id > 0)
                set.Add(id);
        }
        return set;
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
