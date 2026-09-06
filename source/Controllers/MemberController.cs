using FamilyTree.Configuration;
using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FamilyTree.Controllers;

public class MemberController : Controller
{
    private const string WxStateKey = "FT.WxOAuth.State";
    private const string WxReturnKey = "FT.WxOAuth.ReturnUrl";
    private const string PendingCreateInviteKey = "FT.PendingCreateInvite";
    private const string PendingJoinClanKey = "FT.PendingJoinClan";

    private readonly FtAccountService _acc;
    private readonly FtTreeService _tree;
    private readonly FtMyProfileService _profile;
    private readonly FtPersonService _persons;
    private readonly FtClanService _clans;
    private readonly FtDutyAccess _duty;
    private readonly FtApiAuthService _auth;
    private readonly FtMemberCookieSignIn _cookie;
    private readonly FamilyTreeOptions _opt;

    public MemberController(
        FtAccountService acc,
        FtTreeService tree,
        FtMyProfileService profile,
        FtPersonService persons,
        FtClanService clans,
        FtDutyAccess duty,
        FtApiAuthService auth,
        FtMemberCookieSignIn cookie,
        IOptions<FamilyTreeOptions> opt)
    {
        _acc = acc;
        _tree = tree;
        _profile = profile;
        _persons = persons;
        _clans = clans;
        _duty = duty;
        _auth = auth;
        _cookie = cookie;
        _opt = opt.Value;
    }

    /// <summary>注册邀请落地页（扫码直达）。</summary>
    [HttpGet, AllowAnonymous]
    public IActionResult Join(string? returnUrl)
    {
        var ret = SafeReturn(returnUrl);
        ViewBag.MpEnabled = _auth.MpConfigured || _auth.MpDevMock;
        ViewBag.ReturnUrl = ret;
        return View("Join", new FtMemberRegisterVm { ReturnUrl = ret });
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Register() => RedirectToAction(nameof(Join));

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(FtMemberRegisterVm model, CancellationToken ct)
    {
        var (ok, msg, user) = await _acc.RegisterAsync(model, ct);
        if (!ok || user == null)
        {
            ModelState.AddModelError(string.Empty, msg);
            ViewBag.MpEnabled = _auth.MpConfigured || _auth.MpDevMock;
            ViewBag.ReturnUrl = SafeReturn(model.ReturnUrl);
            return View(model);
        }
        var afterLogin = SafeReturn(model.ReturnUrl);
        await _cookie.SignInAsync(user, rememberMe: true);
        TempData["SuccessMessage"] = afterLogin.Contains("/Member/CreateClan", StringComparison.OrdinalIgnoreCase)
            ? "注册成功。请验证邀请码并创建家族，您将成为族谱管理员。"
            : afterLogin.Contains("/Member/JoinClan", StringComparison.OrdinalIgnoreCase)
                ? "注册成功。请确认加入家族。"
                : "注册成功。请继续认证与录入。";
        return LocalRedirect(afterLogin);
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public Task<IActionResult> Register(FtMemberRegisterVm model, CancellationToken ct) => Join(model, ct);

    /// <summary>
    /// 公众号菜单/分享入口：微信内打开后走网页授权，自动识别或注册并登录。
    /// 开发未配公众号时：/Member/Wx?mock=testuser1
    /// </summary>
    [HttpGet, AllowAnonymous]
    public IActionResult Wx(string? returnUrl = null, string? mock = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(SafeReturn(returnUrl));

        if (!_auth.MpConfigured && !_auth.MpDevMock)
        {
            TempData["ErrorMessage"] = "公众号未配置。请填写 FamilyTree:WeChat:MpAppId / MpAppSecret。";
            return RedirectToAction(nameof(Join), new { returnUrl });
        }

        // 本机开发：无公众号时用 mock 直接建号登录
        if (_auth.MpDevMock)
        {
            var m = (mock ?? "").Trim();
            if (m.Length < 4)
                m = "dev" + Guid.NewGuid().ToString("N")[..8];
            return RedirectToAction(nameof(WxCallback), new { code = m, state = "dev", returnUrl });
        }

        var state = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString(WxStateKey, state);
        HttpContext.Session.SetString(WxReturnKey, SafeReturn(returnUrl));

        var callback = AbsUrl(Url.Action(nameof(WxCallback), "Member") ?? "/Member/WxCallback");
        var url = _auth.BuildMpAuthorizeUrl(callback, state);
        return Redirect(url);
    }

    /// <summary>公众号 OAuth 回调：code → OpenID → 自动注册/登录。</summary>
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> WxCallback(string? code, string? state, string? returnUrl, string? mockOpenId, CancellationToken ct)
    {
        if (!_auth.MpConfigured && !_auth.MpDevMock)
        {
            TempData["ErrorMessage"] = "公众号未配置。";
            return RedirectToAction(nameof(Join), new { returnUrl });
        }

        if (_auth.MpConfigured)
        {
            var expect = HttpContext.Session.GetString(WxStateKey);
            if (string.IsNullOrEmpty(expect) || !string.Equals(expect, state, StringComparison.Ordinal))
            {
                TempData["ErrorMessage"] = "微信授权已失效，请重试。";
                return RedirectToAction(nameof(Wx), new { returnUrl });
            }
            HttpContext.Session.Remove(WxStateKey);
        }

        var (ok, msg, user) = await _auth.MpLoginOrRegisterAsync(code, mockOpenId ?? code, ct);
        if (!ok || user == null)
        {
            TempData["ErrorMessage"] = msg;
            return RedirectToAction(nameof(Join), new { returnUrl });
        }

        await _cookie.SignInAsync(user, rememberMe: true);
        var dest = HttpContext.Session.GetString(WxReturnKey);
        HttpContext.Session.Remove(WxReturnKey);
        if (string.IsNullOrWhiteSpace(dest))
            dest = SafeReturn(returnUrl);
        return LocalRedirect(dest);
    }

    /// <summary>扫码总入口：按状态跳到认证或录入人物。</summary>
    [Authorize]
    public async Task<IActionResult> Go(CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var name = User.FindFirst("EmployeeName")?.Value;
        var self = await _persons.EnsureSelfPersonAsync(uid.Value, name, null, ct);
        if (!self.IsCertified)
            return RedirectToAction("Index", "MemberGuide", new { step = "cert" });
        return RedirectToAction("Index", "MemberGuide");
    }

    [Authorize]
    public async Task<IActionResult> Home(CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var name = User.FindFirst("EmployeeName")?.Value;
        var self = await _persons.EnsureSelfPersonAsync(uid.Value, name, null, ct);
        ViewBag.Self = self;
        ViewBag.HasSelf = await _profile.BoundPersonAsync(uid.Value, ct) != null;
        ViewBag.CanShare = await _persons.CanCertifyAsync(uid.Value, ct);
        ViewBag.MyClan = await _clans.GetUserClanAsync(uid.Value, ct);
        return View();
    }

    /// <summary>创建自己的家族（须邀请码；超管可直接建。创建者成为族谱管理员）。</summary>
    [Authorize]
    public async Task<IActionResult> CreateClan(string? c, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var code = (c ?? "").Trim().ToUpperInvariant();
        if (code.Length > 0)
            HttpContext.Session.SetString(PendingCreateInviteKey, code);
        else
            code = (HttpContext.Session.GetString(PendingCreateInviteKey) ?? "").Trim().ToUpperInvariant();
        ViewBag.MyClan = await _clans.GetUserClanAsync(uid.Value, ct);
        ViewBag.InviteCode = code;
        ViewBag.IsSuper = await _duty.IsSuperAsync(uid.Value, ct);
        var (inviteOk, inviteHint) = await _clans.CheckCreateInviteAsync(code, ct);
        ViewBag.InviteOk = inviteOk;
        ViewBag.InviteHint = inviteHint;
        return View();
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClan(string? clanName, string? inviteCode, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var (ok, msg, _) = await _clans.CreateAsync(uid.Value, clanName, FtClaims.Operator(User), inviteCode, ct);
        if (!ok)
        {
            TempData["ErrorMessage"] = msg;
            var code = (inviteCode ?? "").Trim().ToUpperInvariant();
            ViewBag.MyClan = await _clans.GetUserClanAsync(uid.Value, ct);
            ViewBag.InviteCode = code;
            ViewBag.IsSuper = await _duty.IsSuperAsync(uid.Value, ct);
            var (inviteOk, inviteHint) = await _clans.CheckCreateInviteAsync(code, ct);
            ViewBag.InviteOk = inviteOk;
            ViewBag.InviteHint = inviteHint;
            return View();
        }
        HttpContext.Session.Remove(PendingCreateInviteKey);
        TempData["SuccessMessage"] = msg;
        return RedirectToAction(nameof(FamilyShare));
    }

    /// <summary>凭家族 ID 加入（扫码落地）。</summary>
    [Authorize]
    public async Task<IActionResult> JoinClan(string? c, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var mine = await _clans.GetUserClanAsync(uid.Value, ct);
        if (mine != null)
        {
            ViewBag.MyClan = mine;
            return View("JoinClanAlready");
        }
        var code = (c ?? "").Trim().ToUpperInvariant();
        if (code.Length > 0)
            HttpContext.Session.SetString(PendingJoinClanKey, code);
        else
            code = (HttpContext.Session.GetString(PendingJoinClanKey) ?? "").Trim().ToUpperInvariant();
        var clan = await _clans.GetByCodeAsync(code, ct);
        ViewBag.ClanCode = code;
        ViewBag.Clan = clan;
        return View();
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinClanConfirm(string? clanCode, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var (ok, msg) = await _clans.JoinAsync(uid.Value, clanCode, FtClaims.Operator(User), ct);
        if (ok)
        {
            HttpContext.Session.Remove(PendingJoinClanKey);
            TempData["SuccessMessage"] = msg;
        }
        else TempData["ErrorMessage"] = msg;
        return RedirectToAction(nameof(Home));
    }

    /// <summary>分享家族二维码（已入族成员）。</summary>
    [Authorize]
    public async Task<IActionResult> FamilyShare(CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var clan = await _clans.GetUserClanAsync(uid.Value, ct);
        if (clan == null)
        {
            TempData["ErrorMessage"] = "请先创建或加入家族。";
            return RedirectToAction(nameof(CreateClan));
        }
        ViewBag.Clan = clan;
        ViewBag.JoinUrl = AbsUrl("/Member/JoinClan?c=" + Uri.EscapeDataString(clan.ClanCode));
        ViewBag.WxUrl = AbsUrl("/Member/Wx?returnUrl=" + Uri.EscapeDataString("/Member/JoinClan?c=" + clan.ClanCode));
        return View();
    }

    [Authorize]
    public async Task<IActionResult> FamilyQr(string? kind, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var clan = await _clans.GetUserClanAsync(uid.Value, ct);
        if (clan == null) return Forbid();
        var url = string.Equals(kind, "wx", StringComparison.OrdinalIgnoreCase)
            ? AbsUrl("/Member/Wx?returnUrl=" + Uri.EscapeDataString("/Member/JoinClan?c=" + clan.ClanCode))
            : AbsUrl("/Member/JoinClan?c=" + Uri.EscapeDataString(clan.ClanCode));
        return File(FtQrPng.PngBytes(url, 8), "image/png");
    }

    /// <summary>管理员分享：注册邀请码 + 录入人物入口码。</summary>
    [Authorize]
    public async Task<IActionResult> Share(CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        if (!await _persons.CanCertifyAsync(uid, ct))
        {
            TempData["ErrorMessage"] = "仅分支管理员或超管可分享注册/录入人物二维码。";
            return RedirectToAction(nameof(Home));
        }
        ViewBag.JoinUrl = AbsUrl("/Member/Join");
        ViewBag.WxUrl = AbsUrl("/Member/Wx");
        ViewBag.GoUrl = AbsUrl("/Member/Go");
        return View();
    }

    [Authorize]
    public IActionResult ShareQr(string? kind)
    {
        var url = string.Equals(kind, "go", StringComparison.OrdinalIgnoreCase)
            ? AbsUrl("/Member/Go")
            : string.Equals(kind, "wx", StringComparison.OrdinalIgnoreCase)
                ? AbsUrl("/Member/Wx")
                : AbsUrl("/Member/Join");
        return File(FtQrPng.PngBytes(url, 8), "image/png");
    }

    [Authorize]
    public async Task<IActionResult> Cert(CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var name = User.FindFirst("EmployeeName")?.Value;
        var self = await _persons.EnsureSelfPersonAsync(uid.Value, name, null, ct);
        ViewBag.CertUrl = CertifyAbsoluteUrl(self.CertCode);
        return View(self);
    }

    [Authorize]
    public async Task<IActionResult> CertQr(CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var name = User.FindFirst("EmployeeName")?.Value;
        var self = await _persons.EnsureSelfPersonAsync(uid.Value, name, null, ct);
        var png = FtQrPng.PngBytes(CertifyAbsoluteUrl(self.CertCode), 8);
        return File(png, "image/png");
    }

    /// <summary>已认证族人：全屏只读主谱（无侧栏、不可编辑）。</summary>
    [Authorize]
    public async Task<IActionResult> ClanTree(CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var name = User.FindFirst("EmployeeName")?.Value;
        var self = await _persons.EnsureSelfPersonAsync(uid.Value, name, null, ct);
        if (!await CanViewClanTreeAsync(uid.Value, self, ct))
            return View("ClanTreeDenied");

        // 不在 GET 上写库：清理待审占位改由 /FtTree/Maintain 显式触发
        var clanId = await _clans.GetUserClanIdAsync(uid.Value, ct);
        int? rootClan = clanId;
        if (!clanId.HasValue)
        {
            if (!await _duty.IsSuperAsync(uid.Value, ct))
            {
                TempData["ErrorMessage"] = "请先加入或创建家族后再查看族谱。";
                return RedirectToAction(nameof(Home));
            }
            rootClan = null;
        }

        var roots = await _tree.CurrentRootsAsync(ct, rootClan);
        HashSet<int>? allow = rootClan is int cid
            ? await _clans.GetClanPersonIdsAsync(cid, ct)
            : null;
        var forest = new List<FtTreeNodeVm>();
        var seen = new HashSet<int>();
        foreach (var r in roots)
        {
            if (!seen.Add(r.Id)) continue;
            var node = await _tree.BuildDownAsync(r.Id, ct, withPeerStubs: false, allowIds: allow, mainGenealogyOnly: true);
            if (node != null) forest.Add(node);
        }

        var clan = await _clans.GetUserClanAsync(uid.Value, ct);
        ViewBag.SelfName = self.FullName;
        ViewBag.ClanName = clan?.ClanName;
        return View(forest);
    }

    /// <summary>生成族谱一览页二维码（已认证族人可分享）。</summary>
    [Authorize]
    public async Task<IActionResult> ClanTreeShare(CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var name = User.FindFirst("EmployeeName")?.Value;
        var self = await _persons.EnsureSelfPersonAsync(uid.Value, name, null, ct);
        if (!await CanViewClanTreeAsync(uid.Value, self, ct))
            return View("ClanTreeDenied");

        ViewBag.PageUrl = AbsUrl("/Member/ClanTree");
        ViewBag.WxUrl = AbsUrl("/Member/Wx?returnUrl=" + Uri.EscapeDataString("/Member/ClanTree"));
        return View();
    }

    [Authorize]
    public async Task<IActionResult> ClanTreeQr(string? kind, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var name = User.FindFirst("EmployeeName")?.Value;
        var self = await _persons.EnsureSelfPersonAsync(uid.Value, name, null, ct);
        if (!await CanViewClanTreeAsync(uid.Value, self, ct))
            return Forbid();

        var url = string.Equals(kind, "wx", StringComparison.OrdinalIgnoreCase)
            ? AbsUrl("/Member/Wx?returnUrl=" + Uri.EscapeDataString("/Member/ClanTree"))
            : AbsUrl("/Member/ClanTree");
        return File(FtQrPng.PngBytes(url, 8), "image/png");
    }

    private async Task<bool> CanViewClanTreeAsync(int userId, FtPerson self, CancellationToken ct)
    {
        if (await _persons.IsStaffAsync(userId, ct)) return true;
        return self.IsCertified;
    }

    /// <summary>
    /// 本地跳转地址白名单。手写的「以 / 开头且不以 // 开头」比 <c>Url.IsLocalUrl</c> 弱：
    /// 它会放行 <c>/\evil.com</c>，随后被 <c>LocalRedirect</c> 二次校验时抛出未处理的 500。
    /// </summary>
    private string SafeReturn(string? returnUrl)
    {
        var u = (returnUrl ?? "").Trim();
        if (u.Length == 0) return "/Member/Go";
        return Url.IsLocalUrl(u) ? u : "/Member/Go";
    }

    private string CertifyAbsoluteUrl(string? code) =>
        AbsUrl(Url.Action("Open", "FtCertify", new { c = code }) ?? $"/FtCertify/Open?c={Uri.EscapeDataString(code ?? "")}");

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
