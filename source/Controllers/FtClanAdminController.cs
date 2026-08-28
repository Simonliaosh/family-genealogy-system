using FamilyTree.Configuration;
using FamilyTree.Helpers;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FamilyTree.Controllers;

/// <summary>家族信息：超管只委任族谱管理员；族谱管理员管本族成员与支链（超管不插手业务/支链）。</summary>
[Authorize]
public class FtClanAdminController : Controller
{
    private readonly FtClanService _clans;
    private readonly FtDutyAccess _duty;
    private readonly FtBranchAdminService _branchPosts;
    private readonly FtBranchAdminApplyService _applies;
    private readonly FtApiAuthService _auth;
    private readonly FamilyTreeOptions _opt;

    public FtClanAdminController(
        FtClanService clans,
        FtDutyAccess duty,
        FtBranchAdminService branchPosts,
        FtBranchAdminApplyService applies,
        FtApiAuthService auth,
        IOptions<FamilyTreeOptions> opt)
    {
        _clans = clans;
        _duty = duty;
        _branchPosts = branchPosts;
        _applies = applies;
        _auth = auth;
        _opt = opt.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        if (!await _duty.CanManageClansAsync(uid, ct)) return Forbid();

        var isSuper = await _duty.IsSuperAsync(uid, ct);
        var isClanAdmin = await _duty.IsClanAdminAsync(uid, ct);
        ViewBag.IsSuper = isSuper;
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false);

        // 族谱管理员（非超管）：直达本族成员页
        if (isClanAdmin && !isSuper)
        {
            var my = await _clans.GetUserClanIdAsync(uid, ct);
            if (my is int cid)
                return RedirectToAction(nameof(Members), new { id = cid });
        }

        return View(await _clans.ListClansForAdminAsync(uid, ct));
    }

    /// <summary>超管 / 族谱管理员：生成「创建新家族」邀请二维码。</summary>
    [HttpGet]
    public async Task<IActionResult> InviteCreate(CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        if (!await _duty.CanManageClansAsync(uid, ct)) return Forbid();
        return View("InviteCreateStart");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> InviteCreate(string? remark, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var (ok, msg, invite) = await _clans.CreateClanCreateInviteAsync(uid, FtClaims.Operator(User), remark, ct);
        if (!ok || invite == null)
        {
            TempData["ErrorMessage"] = msg;
            return RedirectToAction(nameof(InviteCreate));
        }
        TempData["SuccessMessage"] = msg;
        ViewBag.InviteCode = invite.InviteCode;
        ViewBag.ExpireAt = invite.ExpireAt;
        ViewBag.PageUrl = AbsUrl("/Member/CreateClan?c=" + Uri.EscapeDataString(invite.InviteCode));
        ViewBag.WxUrl = AbsUrl("/Member/Wx?returnUrl=" + Uri.EscapeDataString("/Member/CreateClan?c=" + invite.InviteCode));
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> InviteCreateQr(string? c, string? kind, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        if (!await _duty.CanManageClansAsync(uid, ct)) return Forbid();
        var inv = await _clans.FindOpenCreateInviteAsync(c, ct);
        if (inv == null) return NotFound();
        var path = "/Member/CreateClan?c=" + Uri.EscapeDataString(inv.InviteCode);
        var url = string.Equals(kind, "wx", StringComparison.OrdinalIgnoreCase)
            ? AbsUrl("/Member/Wx?returnUrl=" + Uri.EscapeDataString(path))
            : AbsUrl(path);
        return File(FtQrPng.PngBytes(url, 8), "image/png");
    }

    [HttpGet]
    public async Task<IActionResult> Members(int id, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var isSuper = await _duty.IsSuperAsync(uid, ct);
        var isClanAdmin = await _duty.IsClanAdminAsync(uid, ct);
        if (!isSuper && !isClanAdmin) return Forbid();

        var myClan = await _clans.GetUserClanIdAsync(uid, ct);
        if (!isSuper && myClan != id) return Forbid();
        var clan = await _clans.GetAsync(id, ct);
        if (clan == null) return NotFound();
        ViewBag.Clan = clan;
        ViewBag.IsSuper = isSuper;
        ViewBag.IsClanAdmin = isClanAdmin;
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false);
        ViewBag.Applies = isClanAdmin ? await _applies.ListPendingAsync(uid, ct) : new List<Models.ViewModels.FtBranchApplyRowVm>();
        return View(await _clans.ListMembersAsync(id, ct));
    }

    /// <summary>仅超管：委任族谱管理员。</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetClanAdmin(int userId, bool grant, int clanId, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var (ok, msg) = await _clans.SetClanAdminAsync(userId, grant, FtClaims.UserId(User) ?? 0, FtClaims.Operator(User), ct);
        if (ok) TempData["SuccessMessage"] = msg;
        else TempData["ErrorMessage"] = msg;
        return RedirectToAction(nameof(Members), new { id = clanId });
    }

    /// <summary>仅族谱管理员：本家族支链管理员任免。</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetBranchAdmin(int userId, bool grant, int clanId, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        if (!await _duty.IsClanAdminAsync(uid, ct))
        {
            TempData["ErrorMessage"] = "仅族谱管理员可任免支链管理员。";
            return RedirectToAction(nameof(Members), new { id = clanId });
        }
        try
        {
            await _branchPosts.SetAsync(userId, grant, uid, FtClaims.Operator(User), ct);
            TempData["SuccessMessage"] = grant ? "已设为本家族支链管理员。" : "已取消支链管理员。";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Members), new { id = clanId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveBranchApply(int id, bool pass, int clanId, string? remark, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var (ok, msg) = await _applies.ApproveAsync(id, pass, FtClaims.UserId(User) ?? 0, FtClaims.Operator(User), remark, ct);
        if (ok) TempData["SuccessMessage"] = msg;
        else TempData["ErrorMessage"] = msg;
        return RedirectToAction(nameof(Members), new { id = clanId });
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }

    private string AbsUrl(string pathOrUrl)
    {
        if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return pathOrUrl;
        var root = _auth.ResolvePublicBaseUrl();
        if (root.Length == 0)
            root = (_opt.PublicBaseUrl ?? "").Trim().TrimEnd('/');
        if (root.Length == 0)
        {
            var req = Request;
            root = $"{req.Scheme}://{req.Host.Value}".TrimEnd('/');
        }
        if (!pathOrUrl.StartsWith('/')) pathOrUrl = "/" + pathOrUrl;
        return root + pathOrUrl;
    }
}
