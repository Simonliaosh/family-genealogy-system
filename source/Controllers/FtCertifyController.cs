using FamilyTree.Helpers;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class FtCertifyController : Controller
{
    private readonly FtPersonService _persons;
    public FtCertifyController(FtPersonService persons) => _persons = persons;

    [HttpGet]
    public async Task<IActionResult> Open(string? c, CancellationToken ct)
    {
        var row = await _persons.FindByCertCodeAsync(c, ct);
        var uid = FtClaims.UserId(User) ?? 0;
        ViewBag.Code = (c ?? "").Trim();
        ViewBag.CanCertify = await _persons.CanCertifyAsync(uid, ct);
        if (row == null)
            ViewBag.Err = "认证码无效。请让族员重新打开「申请认证」再扫一次。";
        return View(row);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(string? c, CancellationToken ct)
    {
        try
        {
            await _persons.SetCertifiedByCodeAsync(c, FtClaims.UserId(User) ?? 0, FtClaims.Operator(User), ct);
            TempData["SuccessMessage"] = "已认证。该族员可以申请加入主谱。";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Open), new { c });
    }
}
