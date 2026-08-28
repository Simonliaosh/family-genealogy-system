using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class FtPeerController : Controller
{
    private readonly FtPeerService _peers;
    private readonly FtPersonService _persons;

    public FtPeerController(FtPeerService peers, FtPersonService persons)
    {
        _peers = peers;
        _persons = persons;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        ViewBag.CanManage = await _peers.CanManagePeerAsync(uid, ct);
        return View(await _peers.ListBridgesAsync(ct));
    }

    [HttpGet]
    public async Task<IActionResult> Invite(int personId, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        if (!await _peers.CanManagePeerAsync(uid, ct)) return Forbid();
        var p = await _persons.GetAsync(personId, ct);
        if (p == null) return NotFound();
        try
        {
            var (code, url, person, expire) = await _peers.CreateInviteAsync(personId, uid, FtClaims.Operator(User), ct);
            ViewBag.InviteCode = code;
            ViewBag.InviteUrl = url;
            ViewBag.Expire = expire;
            ViewBag.PersonName = person.FullName;
            return View();
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Edit", "FtPerson", new { id = personId });
        }
    }

    [HttpGet]
    public IActionResult InviteQr(string? url)
    {
        var u = (url ?? "").Trim();
        if (u.Length == 0) return NotFound();
        return File(FtQrPng.PngBytes(u, 8), "image/png");
    }

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> InviteOpen(string? c, CancellationToken ct)
    {
        var info = await _peers.PublicInviteInfoAsync(c, ct);
        if (info == null)
        {
            ViewBag.Err = "邀请码无效或已过期。";
            return View();
        }
        var t = info.GetType();
        ViewBag.Code = (c ?? "").Trim().ToUpperInvariant();
        ViewBag.FullName = t.GetProperty("fullName")?.GetValue(info)?.ToString();
        ViewBag.FatherName = t.GetProperty("fatherName")?.GetValue(info)?.ToString();
        ViewBag.MotherName = t.GetProperty("motherName")?.GetValue(info)?.ToString();
        ViewBag.BirthDate = t.GetProperty("birthDate")?.GetValue(info)?.ToString();
        ViewBag.BaseUrl = t.GetProperty("baseUrl")?.GetValue(info)?.ToString();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Accept(string? c, string? baseUrl, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        if (!await _peers.CanManagePeerAsync(uid, ct)) return Forbid();
        var persons = await _persons.VisiblePersonsAsync(uid, ct);
        ViewBag.Persons = persons.OrderBy(x => x.FullName).Select(x => (x.DataId, x.FullName)).ToList();
        return View(new FtPeerAcceptVm
        {
            InviteCode = (c ?? "").Trim().ToUpperInvariant(),
            PeerBaseUrl = (baseUrl ?? "").Trim()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(FtPeerAcceptVm model, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        if (!await _peers.CanManagePeerAsync(uid, ct)) return Forbid();
        var persons = await _persons.VisiblePersonsAsync(uid, ct);
        ViewBag.Persons = persons.OrderBy(x => x.FullName).Select(x => (x.DataId, x.FullName)).ToList();
        try
        {
            await _peers.AcceptInviteAsync(model, uid, FtClaims.Operator(User), ct);
            TempData["SuccessMessage"] = "对接已建立。可在族谱树中点击外链房展开。";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(int id, CancellationToken ct)
    {
        try
        {
            await _peers.RevokeAsync(id, FtClaims.UserId(User) ?? 0, FtClaims.Operator(User), ct);
            TempData["SuccessMessage"] = "已取消互通。对方将无法再展开本房数据。";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
