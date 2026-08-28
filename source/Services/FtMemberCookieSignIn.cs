using System.Security.Claims;
using FamilyTree.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace FamilyTree.Services;

/// <summary>成员端 Cookie 登录（与 Account 密码登录同一套 Claim）。</summary>
public sealed class FtMemberCookieSignIn
{
    private readonly IHttpContextAccessor _http;
    private readonly IConfiguration _cfg;

    public FtMemberCookieSignIn(IHttpContextAccessor http, IConfiguration cfg)
    {
        _http = http;
        _cfg = cfg;
    }

    public Task SignInAsync(EUser user, bool rememberMe = true)
    {
        var ctx = _http.HttpContext ?? throw new InvalidOperationException("无 HttpContext。");
        var mCompanyName = (_cfg["App:CompanyDisplayName"] ?? "").Trim();
        if (string.IsNullOrEmpty(mCompanyName))
            mCompanyName = "FamilyTree";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.LoginId),
            new Claim(ClaimTypes.NameIdentifier, "0"),
            new Claim("EmployeeName", user.RealName),
            new Claim(FrameworkClaimTypes.MemberId, user.LoginId),
            new Claim("MemberID", user.LoginId),
            new Claim(FrameworkClaimTypes.EUserId, user.DataId.ToString()),
            new Claim(FrameworkClaimTypes.AuthPrincipalKind, FrameworkClaimTypes.KindEUsers),
            new Claim(FrameworkClaimTypes.CurrentIdentityType, "Employee"),
            new Claim(FrameworkClaimTypes.MCompanyName, mCompanyName),
        };
        ctx.Session.SetString(FrameworkClaimTypes.CurrentIdentityType, "Employee");

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var props = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            AllowRefresh = true
        };
        return ctx.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            props);
    }
}
