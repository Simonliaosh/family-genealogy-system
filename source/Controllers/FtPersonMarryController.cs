using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class FtPersonMarryController : Controller
{
    private readonly FtPersonMarryService _service;
    private readonly FtPersonService _persons;
    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
        { "SpouseName", "PersonName", "Remark" };
    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
        { "HouseSeq", "PersonName", "SpouseName", "AmendDate" };

    public FtPersonMarryController(FtPersonMarryService service, FtPersonService persons)
    {
        _service = service;
        _persons = persons;
    }

    [HttpGet, HttpPost]
    public async Task<IActionResult> Index(int? personId, string? marryType1, string? searchField, string? searchContent,
        string? selectField, string? selectFieldArrow, int? intPage, int? pageShowNum, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User);
        if (uid == null) return Forbid();
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        var sf = SearchWhitelist.Contains((searchField ?? "").Trim()) ? (searchField ?? "").Trim() : "SpouseName";
        var sortField = SortWhitelist.Contains((selectField ?? "").Trim()) ? (selectField ?? "").Trim() : "HouseSeq";
        var pid = personId.GetValueOrDefault();
        string? filterPersonName = null;
        if (pid > 0)
        {
            var p = await _persons.GetAsync(pid, ct);
            if (p != null && (p.OwnerUserId == uid.Value || p.BindUserId == uid.Value
                || await _persons.CanEditAsync(uid.Value, p, ct)))
                filterPersonName = p.FullName;
            else
                pid = 0;
        }
        var (pageRows, total, totalPages, pageOut) = await _service.GetIndexPageAsync(
            uid.Value, pid > 0 ? pid : null, marryType1, sf, (searchContent ?? "").Trim(),
            sortField, (selectFieldArrow ?? "0").Trim(),
            intPage.GetValueOrDefault(1), pageShowNum.GetValueOrDefault(16), ct);
        ViewBag.CanCreate = CanWrite(lim);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = CanRemove(lim);
        ViewBag.PersonId = pid > 0 ? pid : 0;
        ViewBag.FilterPersonName = filterPersonName;
        ViewBag.MarryType1 = marryType1 ?? "";
        ViewBag.MarryTypes = FtPersonMarryService.MarryTypes();
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
    public async Task<IActionResult> Create(int? personId, CancellationToken ct)
    {
        if (!CanWrite()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var m = new FtMarryFormVm { PersonId = personId.GetValueOrDefault(), HouseSeq = 99, MarryType = "原配" };
        await FillLookupsAsync(uid, lockPerson: false, ct);
        return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FtMarryFormVm model, string? selectNo, CancellationToken ct)
    {
        if (selectNo == "9") return RedirectToAction(nameof(Index), new { personId = model.PersonId > 0 ? model.PersonId : (int?)null });
        if (!CanWrite()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        _service.Normalize(model);
        if (model.PersonId <= 0)
            ModelState.AddModelError(nameof(model.PersonId), "请选择人物。");
        if (string.IsNullOrWhiteSpace(model.SpouseName))
            ModelState.AddModelError(nameof(model.SpouseName), "配偶姓名不能为空。");
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(uid, lockPerson: false, ct);
            return View(model);
        }
        try
        {
            await _service.SaveAsync(model, uid, FtClaims.Operator(User), ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await FillLookupsAsync(uid, lockPerson: false, ct);
            return View(model);
        }
        if (selectNo == "0") return RedirectToAction(nameof(Index), new { personId = model.PersonId });
        return RedirectToAction(nameof(Create), new { personId = model.PersonId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? polistRt, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false) && !CanView())
            return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var row = await _service.GetAsync(id, ct);
        if (row == null) return NotFound();
        var person = await _persons.GetAsync(row.PersonId, ct);
        if (person == null || !await _persons.CanViewAsync(uid, person, ct)) return NotFound();
        ViewBag.PolistRt = polistRt;
        await FillLookupsAsync(uid, lockPerson: true, ct);
        ViewBag.CanSave = FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)
            && await _persons.CanEditAsync(uid, person, ct);
        return View(_service.ToForm(row, person.FullName));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FtMarryFormVm model, string? selectNo, string? polistRt, CancellationToken ct)
    {
        if (selectNo == "9") return PolistReturnToken.RedirectToIndex(this, polistRt);
        ViewBag.PolistRt = polistRt;
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        model.DataId = id;
        _service.Normalize(model);
        if (string.IsNullOrWhiteSpace(model.SpouseName))
            ModelState.AddModelError(nameof(model.SpouseName), "配偶姓名不能为空。");
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(uid, lockPerson: true, ct);
            ViewBag.CanSave = true;
            return View(model);
        }
        try
        {
            await _service.SaveAsync(model, uid, FtClaims.Operator(User), ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await FillLookupsAsync(uid, lockPerson: true, ct);
            ViewBag.CanSave = true;
            return View(model);
        }
        if (selectNo == "0") return PolistReturnToken.RedirectToIndex(this, polistRt);
        return RedirectToAction(nameof(Edit), new { id, polistRt });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? personId, CancellationToken ct)
    {
        if (!CanRemove()) return Forbid();
        try { await _service.DeleteAsync(id, FtClaims.UserId(User) ?? 0, ct); }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index), new { personId = personId.GetValueOrDefault() > 0 ? personId : null });
    }

    private async Task FillLookupsAsync(int userId, bool lockPerson, CancellationToken ct)
    {
        ViewBag.MarryTypes = FtPersonMarryService.MarryTypes();
        ViewBag.PersonOptions = await _service.EditablePersonOptionsAsync(userId, ct);
        ViewBag.LockPerson = lockPerson;
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }

    private bool CanWrite() => CanWrite(HttpContext.Items["PubFunctionLimit"] as string);

    private static bool CanWrite(string? lim) =>
        FunctionLimitUi.CanCreate(lim, false) || FunctionLimitUi.CanUpdate(lim, false);

    private static bool CanRemove(string? lim) =>
        FunctionLimitUi.CanDelete(lim, false) || FunctionLimitUi.CanUpdate(lim, false);

    private bool CanRemove() => CanRemove(HttpContext.Items["PubFunctionLimit"] as string);
}
