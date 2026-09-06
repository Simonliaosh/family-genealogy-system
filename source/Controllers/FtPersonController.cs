using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class FtPersonController : Controller
{
    private readonly FtPersonService _service;
    private readonly FtMatchService _match;
    private readonly FtPhotoService _photos;
    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase) { "FullName", "FatherName", "BirthDate" };
    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase) { "FullName", "FatherName", "AmendDate" };

    public FtPersonController(FtPersonService service, FtMatchService match, FtPhotoService photos)
    {
        _service = service;
        _match = match;
        _photos = photos;
    }

    [HttpGet, HttpPost]
    public async Task<IActionResult> Index(string? inMain1, string? certified1, string? searchField, string? searchContent,
        string? selectField, string? selectFieldArrow, int? intPage, int? pageShowNum, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User);
        if (uid == null) return Forbid();
        await _service.HealLinkedBindingsAsync(ct);
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "FullName";
        var sortField = SortWhitelist.Contains((selectField ?? "").Trim()) ? (selectField ?? "").Trim() : "FullName";
        var (pageRows, total, totalPages, pageOut) = await _service.GetIndexPageAsync(
            uid.Value, inMain1, sf, (searchContent ?? "").Trim(), sortField, (selectFieldArrow ?? "0").Trim(),
            intPage.GetValueOrDefault(1), pageShowNum.GetValueOrDefault(16), ct, certified1);
        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = FunctionLimitUi.CanDelete(lim, false);
        ViewBag.InMain1 = inMain1 ?? "";
        ViewBag.Certified1 = certified1 ?? "";
        ViewBag.SearchField = sf;
        ViewBag.SearchContent = (searchContent ?? "").Trim();
        ViewBag.SelectField = sortField;
        ViewBag.SelectFieldArrow = (selectFieldArrow ?? "0").Trim();
        ViewBag.IntPage = pageOut;
        ViewBag.PageShowNum = pageShowNum.GetValueOrDefault(16);
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = total;
        return View(pageRows);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        return View(new FtPersonFormVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FtPersonFormVm model, string? selectNo, bool forceCreate, CancellationToken ct)
    {
        if (selectNo == "9") return RedirectToAction(nameof(Index));
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        _service.NormalizeFormForSave(model);
        foreach (var (k, msg) in _service.GetSaveValidationErrors(model))
            ModelState.AddModelError(k, msg);
        if (!ModelState.IsValid) return View(model);

        if (!forceCreate)
        {
            var dups = await _service.FindPreCreateDuplicatesAsync(model, uid, ct);
            var own = dups.FirstOrDefault(x => !x.InMain);
            if (own != null)
            {
                TempData["SuccessMessage"] = "您已录入过此人，已打开已有档案。";
                return RedirectToAction(nameof(Edit), new { id = own.PersonId });
            }
            if (dups.Any(x => x.InMain))
            {
                ViewBag.PreCreateDups = dups.Where(x => x.InMain).ToList();
                return View(model);
            }
        }

        try
        {
            var (id, hints) = await _service.CreateAsync(model, uid, FtClaims.Operator(User), ct);
            var (action, route) = FtPersonService.RedirectAfterSave(id, hints);
            if (action == "Confirm")
            {
                TempData["SuccessMessage"] = "已保存。请确认是否与主谱为同一人并申请链入。";
                return RedirectToAction(action, "FtPersonLink", route);
            }
            if (action == "Suggest")
            {
                TempData["SuccessMessage"] = "已保存。发现主谱相似人物，请确认是否链入。";
                return RedirectToAction(action, "FtPersonLink", route);
            }
            TempData["SuccessMessage"] = "已保存。";
            if (selectNo == "0") return RedirectToAction(nameof(Index));
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? polistRt, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)
            && !FunctionLimitUi.CanView(HttpContext.Items["PubFunctionLimit"] as string, false))
            return Forbid();
        var row = await _service.GetAsync(id, ct);
        if (row == null) return NotFound();
        // 已等同主谱：只维护目标那一条
        if (row.SameAsPersonId is int sameId && sameId > 0)
            return RedirectToAction(nameof(Edit), new { id = sameId, polistRt });
        var uid = FtClaims.UserId(User) ?? 0;
        if (!await _service.CanViewAsync(uid, row, ct)) return NotFound();
        await FillPersonEditBagsAsync(uid, row, polistRt, ct);
        var nameLocked = !await _service.CanChangeNameAsync(uid, row, ct);
        return View(_service.ToForm(row, nameLocked));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FtPersonFormVm model, string? selectNo, string? polistRt, CancellationToken ct)
    {
        if (selectNo == "9") return PolistReturnToken.RedirectToIndex(this, polistRt);
        ViewBag.PolistRt = polistRt;
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        _service.NormalizeFormForSave(model);
        // Create / AddRelative 都做了这一步，只有 Edit 漏了：ClipReq 对空输入返回 ""，
        // TryUpdateAsync 又不复校验，于是已存在的人物可以被保存成空 FullName，
        // 此后所有按姓名索引的路径都会把它当不存在。
        foreach (var (k, msg) in _service.GetSaveValidationErrors(model))
            ModelState.AddModelError(k, msg);
        if (!ModelState.IsValid)
        {
            var cur = await _service.GetAsync(id, ct);
            if (cur != null)
            {
                model.NameLocked = !await _service.CanChangeNameAsync(uid, cur, ct);
                await FillPersonEditBagsAsync(uid, cur, polistRt, ct);
            }
            return View(model);
        }
        try
        {
            var hints = await _service.TryUpdateAsync(id, model, uid, FtClaims.Operator(User), ct);
            var (action, route) = FtPersonService.RedirectAfterSave(id, hints);
            if (action is "Confirm" or "Suggest")
            {
                TempData["SuccessMessage"] = "已保存。发现主谱相似人物，请确认是否链入。";
                return RedirectToAction(action, "FtPersonLink", route);
            }
            TempData["SuccessMessage"] = "已保存。";
            if (selectNo == "0") return PolistReturnToken.RedirectToIndex(this, polistRt);
            return RedirectToAction(nameof(Edit), new { id, polistRt });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var row = await _service.GetAsync(id, ct);
            if (row != null)
            {
                model.NameLocked = !await _service.CanChangeNameAsync(uid, row, ct);
                await FillPersonEditBagsAsync(uid, row, polistRt, ct);
            }
            else
            {
                ViewBag.Hints = await _match.MatchPersonAsync(id, ct);
            }
            return View(model);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        try { await _service.SoftDeleteAsync(id, FtClaims.UserId(User) ?? 0, ct); }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMain(int id, bool include, bool withDesc, bool descendantsOnly, string? polistRt, CancellationToken ct)
    {
        try
        {
            var msg = await _service.SetMainAsync(id, include, withDesc, FtClaims.UserId(User) ?? 0, FtClaims.Operator(User), ct, descendantsOnly);
            TempData["SuccessMessage"] = msg;
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Edit), new { id, polistRt });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCertified(int id, bool certified, string? polistRt, CancellationToken ct)
    {
        try
        {
            await _service.SetCertifiedAsync(id, certified, FtClaims.UserId(User) ?? 0, FtClaims.Operator(User), ct);
            TempData["SuccessMessage"] = certified ? "已认证，可以加入主谱。" : "已取消认证。";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Edit), new { id, polistRt });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile? file, string? polistRt, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        try
        {
            if (file == null) throw new InvalidOperationException("请选择照片。");
            await _photos.UploadAsync(id, uid, file, ct);
            TempData["SuccessMessage"] = "照片已上传。";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Edit), new { id, polistRt });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int id, string? path, string? polistRt, CancellationToken ct)
    {
        try
        {
            await _photos.DeleteAsync(id, FtClaims.UserId(User) ?? 0, path, ct);
            TempData["SuccessMessage"] = "已删除照片。";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Edit), new { id, polistRt });
    }

    [HttpGet]
    public async Task<IActionResult> AddRelative(int fromId, string kind, string? polistRt, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        if (!await _service.CanExtendTreeAsync(uid, ct)) return Forbid();
        kind = (kind ?? "").Trim().ToUpperInvariant();
        if (kind is not ("FATHER" or "MOTHER" or "CHILD")) return NotFound();
        var from = await _service.GetAsync(fromId, ct);
        if (from == null || !await _service.CanViewAsync(uid, from, ct)) return NotFound();
        if (kind == "FATHER" && from.FatherPersonId.HasValue)
        {
            var exist = await _service.GetAsync(from.FatherPersonId.Value, ct);
            if (exist != null) return RedirectToAction(nameof(Edit), new { id = exist.DataId, polistRt });
        }
        if (kind == "MOTHER" && from.MotherPersonId.HasValue)
        {
            var exist = await _service.GetAsync(from.MotherPersonId.Value, ct);
            if (exist != null) return RedirectToAction(nameof(Edit), new { id = exist.DataId, polistRt });
        }
        ViewBag.FromId = fromId;
        ViewBag.FromName = from.FullName;
        ViewBag.Kind = kind;
        ViewBag.KindText = FtPersonService.RelativeKindText(kind);
        ViewBag.PolistRt = polistRt;
        return View(_service.PrefillRelative(from, kind));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRelative(int fromId, string kind, FtPersonFormVm model, string? selectNo, string? polistRt, CancellationToken ct)
    {
        kind = (kind ?? "").Trim().ToUpperInvariant();
        if (selectNo == "9") return RedirectToAction(nameof(Edit), new { id = fromId, polistRt });
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        _service.NormalizeFormForSave(model);
        foreach (var (k, msg) in _service.GetSaveValidationErrors(model))
            ModelState.AddModelError(k, msg);
        var from = await _service.GetAsync(fromId, ct);
        ViewBag.FromId = fromId;
        ViewBag.FromName = from?.FullName ?? "";
        ViewBag.Kind = kind;
        ViewBag.KindText = FtPersonService.RelativeKindText(kind);
        ViewBag.PolistRt = polistRt;
        if (!ModelState.IsValid) return View(model);
        try
        {
            var (pid, hints) = await _service.AddRelativeAsync(fromId, kind, model, uid, FtClaims.Operator(User), ct);
            TempData["MatchHints"] = System.Text.Json.JsonSerializer.Serialize(hints);
            var who = FtPersonService.RelativeKindText(kind);
            TempData["SuccessMessage"] = hints.Count > 0
                ? $"已添加{who}。系统发现相似人物，可在详情中申请链入。"
                : $"已添加{who}。可继续为其添加父辈。";
            if (selectNo == "0") return RedirectToAction(nameof(Edit), new { id = fromId, polistRt });
            return RedirectToAction(nameof(Edit), new { id = pid, polistRt });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private async Task FillPersonEditBagsAsync(int uid, FamilyTree.Models.FtPerson row, string? polistRt, CancellationToken ct)
    {
        ViewBag.PolistRt = polistRt;
        ViewBag.Hints = await _match.MatchPersonAsync(row.DataId, ct);
        ViewBag.Photos = await _photos.ListVisibleAsync(row, uid, ct);
        ViewBag.CanManagePhoto = await _photos.CanManageAsync(uid, row, ct);
        ViewBag.CanSave = await _service.CanEditAsync(uid, row, ct);
        ViewBag.CanSetMain = await _service.CanSetMainAsync(uid, row, ct);
        ViewBag.CanCertify = await _service.CanCertifyAsync(uid, ct);
        ViewBag.CanExtendTree = await _service.CanExtendTreeAsync(uid, ct);
        ViewBag.CanApplyMain = await _service.MemberCanApplyMainAsync(uid, ct);
        ViewBag.CanPeer = await _service.CanExtendTreeAsync(uid, ct);
        ViewBag.Lineage = await _service.GetLineageAsync(row, ct);
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
