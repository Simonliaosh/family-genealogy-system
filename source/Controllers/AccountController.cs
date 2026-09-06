using FamilyTree.Configuration;
using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace FamilyTree.Controllers;

public class AccountController : Controller
{
    private const string PendingIdentityUserIdKey = "Pending.Identity.EUserId";
    private const string PendingIdentityRememberKey = "Pending.Identity.RememberMe";
    private readonly FrameworkDbContext _context;
    private readonly ILogger<AccountController> _logger;
    private readonly AuditService _auditService;
    private readonly FrameworkRbacOptions _rbacOpt;
    private readonly EUsersService _eUsersService;
    private readonly IConfiguration _configuration;

    private readonly FtAccountService _ftAccount;

    public AccountController(
        FrameworkDbContext context,
        ILogger<AccountController> logger,
        AuditService auditService,
        IOptions<FrameworkRbacOptions> rbacOpt,
        EUsersService eUsersService,
        IConfiguration configuration,
        FtAccountService ftAccount)
    {
        _context = context;
        _logger = logger;
        _auditService = auditService;
        _rbacOpt = rbacOpt.Value;
        _eUsersService = eUsersService;
        _configuration = configuration;
        _ftAccount = ftAccount;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            HttpContext.Session.SetString("Login.ReturnUrl", returnUrl);
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(FamilyTree.Configuration.RateLimitPolicies.Login)]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // 身份证登录是一等流程，日志里不能出现完整号码
        _logger.LogInformation("尝试登录: {LoginName}", FtText.MaskLoginId(model.LoginName));
        var loginName = (model.LoginName ?? "").Trim();
        var plainPassword = model.Password ?? "";
        var fromIp = GetClientIp() ?? "";
        var ua = GetUserAgent();

        if (!_rbacOpt.PreferEUsersLogin)
        {
            AddLoginPageError("本框架仅支持 Tbl_E_Users 登录，请在配置中开启 PreferEUsersLogin。");
            return View(model);
        }

        EUser? eUser = null;
        if (FtText.IsIdCard(loginName))
        {
            try { eUser = await _ftAccount.FindUserByIdCardAsync(loginName, HttpContext.RequestAborted); }
            catch { /* 绑定表未建时回退登录名 */ }
        }
        var loginKey = FtText.IsIdCard(loginName) ? loginName : FtText.NormalizeLoginName(loginName);
        eUser ??= await _context.EUsers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.LoginId == loginKey && !x.IsDeleted, HttpContext.RequestAborted);
        if (eUser == null)
        {
            await AppendELoginLogAsync(loginName, null, "失败", "无此账号", fromIp);
            await _auditService.LogLoginAsync(loginName, loginName, false, fromIp, ua, "无 Tbl_E_Users 账号");
            AddLoginPageError("用户名有误或密码出错次数太多！");
            return View(model);
        }

        return await TryCompleteEUsersLoginAsync(eUser, plainPassword, fromIp, ua, model, loginName);
    }

    private async Task<IActionResult> TryCompleteEUsersLoginAsync(
        EUser trackedOrDetached,
        string plainPassword,
        string fromIp,
        string? ua,
        LoginViewModel model,
        string loginName)
    {
        var user = await _context.EUsers.FirstOrDefaultAsync(x => x.DataId == trackedOrDetached.DataId, HttpContext.RequestAborted);
        if (user == null)
        {
            AddLoginPageError("用户名有误或密码出错次数太多！");
            return View(model);
        }

        async Task<IActionResult> FailAsync(string reason, string uiMessage)
        {
            await AppendELoginLogAsync(loginName, user.DataId, "失败", reason, fromIp);
            await _auditService.LogLoginAsync(loginName, loginName, false, fromIp, ua, reason);
            AddLoginPageError(uiMessage);
            return View(model);
        }

        if (user.IsDeleted)
            return await FailAsync("账号已删除", "用户名有误或密码出错次数太多！");

        if (!EPrincipalAccessService.IsEUserAccountUsable(user.BStatus))
            return await FailAsync("账号状态不可用", "用户名有误或密码出错次数太多！");

        if (!user.IsEnabled)
            return await FailAsync("账号已停用", "用户名有误或密码出错次数太多！");

        if (user.IsLocked)
            return await FailAsync("账号已锁定", "用户名有误或密码出错次数太多！");

        if (user.LoginCount >= user.MaxLoginCount)
            return await FailAsync("超过最大登录次数", "用户名有误或密码出错次数太多！");

        if (!_eUsersService.VerifyPassword(plainPassword, user))
        {
            LoginAttempts.MarkFailure(user);
            await _context.SaveChangesAsync(HttpContext.RequestAborted);

            return await FailAsync("密码错误", "用户名有误或密码出错次数太多！");
        }

        if (PasswordHasher.ShouldUpgrade(user.PasswordAlgo))
            _eUsersService.ApplyStoredPassword(user, plainPassword);

        LoginAttempts.MarkSuccess(user);
        await _context.SaveChangesAsync(HttpContext.RequestAborted);

        await AppendELoginLogAsync(loginName, user.DataId, "成功", null, fromIp);
        await _auditService.LogLoginAsync(loginName, loginName, true, fromIp, ua, null);
        _logger.LogInformation("登录成功(E 账号): {LoginId}", FtText.MaskLoginId(user.LoginId));

        HttpContext.Session.Remove("mypost");
        HttpContext.Session.Remove("mySubpost");
        HttpContext.Session.Remove("mySubMember");

        var identityOptions = BuildIdentityOptions(user);
        var selectedIdentityType = GetDefaultIdentityType(user.UserType, identityOptions);
        if (identityOptions.Count > 1)
        {
            HttpContext.Session.SetString(PendingIdentityUserIdKey, user.DataId.ToString());
            HttpContext.Session.SetString(PendingIdentityRememberKey, model.RememberMe ? "1" : "0");
            HttpContext.Session.SetString(FrameworkClaimTypes.CurrentIdentityType, selectedIdentityType);
            return RedirectToAction(nameof(SelectIdentity));
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.LoginId),
            new Claim(ClaimTypes.NameIdentifier, "0"),
            new Claim("EmployeeName", user.RealName),
            new Claim(FrameworkClaimTypes.MemberId, user.LoginId),
            new Claim("MemberID", user.LoginId),
            new Claim(FrameworkClaimTypes.EUserId, user.DataId.ToString()),
            new Claim(FrameworkClaimTypes.AuthPrincipalKind, FrameworkClaimTypes.KindEUsers),
            new Claim(FrameworkClaimTypes.CurrentIdentityType, selectedIdentityType),
        };
        HttpContext.Session.SetString(FrameworkClaimTypes.CurrentIdentityType, selectedIdentityType);
        await SignInAsync(claims, model.RememberMe);
        return RedirectAfterLogin(model.ReturnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> SelectIdentity()
    {
        var rawId = HttpContext.Session.GetString(PendingIdentityUserIdKey);
        if (!int.TryParse(rawId, out var userId)) return RedirectToAction(nameof(Login));

        var user = await _context.EUsers.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == userId && !x.IsDeleted, HttpContext.RequestAborted);
        if (user == null) return RedirectToAction(nameof(Login));

        var options = BuildIdentityOptions(user);
        if (options.Count <= 1) return await CompleteIdentitySignInAsync(user, options.FirstOrDefault().Value, null);

        var selected = HttpContext.Session.GetString(FrameworkClaimTypes.CurrentIdentityType) ?? GetDefaultIdentityType(user.UserType, options);
        var vm = new AccountIdentitySelectViewModel
        {
            LoginId = user.LoginId,
            RealName = user.RealName,
            SelectedIdentityType = selected,
            IdentityOptions = options
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectIdentity(AccountIdentitySelectViewModel model)
    {
        var rawId = HttpContext.Session.GetString(PendingIdentityUserIdKey);
        if (!int.TryParse(rawId, out var userId)) return RedirectToAction(nameof(Login));

        var user = await _context.EUsers.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == userId && !x.IsDeleted, HttpContext.RequestAborted);
        if (user == null) return RedirectToAction(nameof(Login));

        var options = BuildIdentityOptions(user);
        model.IdentityOptions = options;
        model.LoginId = user.LoginId;
        model.RealName = user.RealName;
        if (!options.Any(x => string.Equals(x.Value, model.SelectedIdentityType, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(model.SelectedIdentityType), "请选择有效身份");
            return View(model);
        }

        return await CompleteIdentitySignInAsync(user, model.SelectedIdentityType, null);
    }

    private async Task<IActionResult> CompleteIdentitySignInAsync(EUser user, string? selectedIdentityType, bool? rememberMe)
    {
        var options = BuildIdentityOptions(user);
        var identityType = string.IsNullOrWhiteSpace(selectedIdentityType)
            ? GetDefaultIdentityType(user.UserType, options)
            : selectedIdentityType!;
        if (!options.Any(x => string.Equals(x.Value, identityType, StringComparison.OrdinalIgnoreCase)))
            identityType = GetDefaultIdentityType(user.UserType, options);

        var remember = rememberMe ?? string.Equals(HttpContext.Session.GetString(PendingIdentityRememberKey), "1", StringComparison.OrdinalIgnoreCase);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.LoginId),
            new Claim(ClaimTypes.NameIdentifier, "0"),
            new Claim("EmployeeName", user.RealName),
            new Claim(FrameworkClaimTypes.MemberId, user.LoginId),
            new Claim("MemberID", user.LoginId),
            new Claim(FrameworkClaimTypes.EUserId, user.DataId.ToString()),
            new Claim(FrameworkClaimTypes.AuthPrincipalKind, FrameworkClaimTypes.KindEUsers),
            new Claim(FrameworkClaimTypes.CurrentIdentityType, identityType),
        };

        HttpContext.Session.SetString(FrameworkClaimTypes.CurrentIdentityType, identityType);
        HttpContext.Session.Remove(PendingIdentityUserIdKey);
        HttpContext.Session.Remove(PendingIdentityRememberKey);
        await SignInAsync(claims, remember);
        return RedirectAfterLogin(null);
    }

    private IActionResult RedirectAfterLogin(string? returnUrl)
    {
        var url = returnUrl;
        if (string.IsNullOrWhiteSpace(url))
            url = HttpContext.Session.GetString("Login.ReturnUrl");
        HttpContext.Session.Remove("Login.ReturnUrl");
        if (!string.IsNullOrWhiteSpace(url) && Url.IsLocalUrl(url))
            return LocalRedirect(url);
        return RedirectToAction("Index", "Home");
    }

    private static List<(string Value, string Text)> BuildIdentityOptions(EUser user)
    {
        var options = new List<(string Value, string Text)>();
        if (EUserExternalRefHelper.GetCustomerId(user).HasValue) options.Add(("Customer", "客户"));
        if (EUserExternalRefHelper.GetPartnerId(user).HasValue) options.Add(("Partner", "合作伙伴"));
        if (EUserExternalRefHelper.GetSupplierId(user).HasValue) options.Add(("Supplier", "供应商"));
        if (!string.IsNullOrWhiteSpace(user.ShareHolderCode)) options.Add(("Shareholder", "股东"));
        if (ShouldIncludeEmployeeIdentity(user.UserType) || options.Count == 0) options.Add(("Employee", "员工"));
        return options.DistinctBy(x => x.Value).ToList();
    }

    private static string GetDefaultIdentityType(string? userType, List<(string Value, string Text)> options)
    {
        var mapped = MapUserTypeToIdentity(userType);
        if (!string.IsNullOrWhiteSpace(mapped) && options.Any(x => x.Value == mapped)) return mapped;
        return options.FirstOrDefault().Value ?? "Employee";
    }

    private static bool ShouldIncludeEmployeeIdentity(string? userType)
    {
        var v = (userType ?? "").Trim();
        return v == "1"
               || v.Equals("EMPLOYEE", StringComparison.OrdinalIgnoreCase)
               || v.Equals("Employee", StringComparison.OrdinalIgnoreCase)
               || v == "员工";
    }

    private static string MapUserTypeToIdentity(string? userType)
    {
        var v = (userType ?? "").Trim();
        return v switch
        {
            "1" or "EMPLOYEE" => "Employee",
            "2" or "CUSTOMER" => "Customer",
            "3" or "SUPPLIER" => "Supplier",
            "6" or "PARTNER" => "Partner",
            "8" or "SHAREHOLDER" => "Shareholder",
            _ when v.Equals("Employee", StringComparison.OrdinalIgnoreCase) || v == "员工" => "Employee",
            _ when v.Equals("Customer", StringComparison.OrdinalIgnoreCase) || v == "客户" => "Customer",
            _ when v.Equals("Supplier", StringComparison.OrdinalIgnoreCase) || v == "供应商" => "Supplier",
            _ when v.Equals("Partner", StringComparison.OrdinalIgnoreCase) || v == "合作伙伴" => "Partner",
            _ when v.Equals("Shareholder", StringComparison.OrdinalIgnoreCase) || v == "股东" => "Shareholder",
            _ => ""
        };
    }

    private async Task AppendELoginLogAsync(string loginId, int? userId, string status, string? failReason, string? ip)
    {
        try
        {
            var masked = FtText.MaskLoginId(loginId);
            _context.ELoginLogs.Add(new ELoginLog
            {
                LoginId = masked.Length > 30 ? masked[..30] : masked,
                UserId = userId,
                LoginStatus = status.Length > 10 ? status[..10] : status,
                FailReason = string.IsNullOrWhiteSpace(failReason) ? null : (failReason.Length > 100 ? failReason[..100] : failReason),
                IPAddress = string.IsNullOrWhiteSpace(ip) ? null : (ip.Length > 50 ? ip[..50] : ip),
                LoginTime = DateTime.Now
            });
            await _context.SaveChangesAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入 Tbl_E_LoginLog 失败: {LoginId}", FtText.MaskLoginId(loginId));
        }
    }

    private void AddLoginPageError(string message) =>
        ModelState.AddModelError(string.Empty, message);

    private Task SignInAsync(List<Claim> claims, bool rememberMe)
    {
        var mCompanyName = (_configuration["App:CompanyDisplayName"] ?? "").Trim();
        if (string.IsNullOrEmpty(mCompanyName))
            mCompanyName = "FamilyTree";

        claims.Add(new Claim(FrameworkClaimTypes.MCompanyName, mCompanyName));

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            AllowRefresh = true
        };
        return HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
    }

    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("用户退出: {Name}", User.Identity?.Name);
        HttpContext.Session.Remove(FrameworkClaimTypes.CurrentIdentityType);
        HttpContext.Session.Remove(PendingIdentityUserIdKey);
        HttpContext.Session.Remove(PendingIdentityRememberKey);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied() => View();

    [HttpGet]
    [Authorize]
    public IActionResult Profile() => View();

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View();

    private string? GetClientIp()
    {
        var forwarded = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var ip = forwarded.Trim();
            if (!ip.Contains("unknown", StringComparison.OrdinalIgnoreCase))
            {
                if (ip.Contains(',')) ip = ip.Split(',')[0];
                else if (ip.Contains(';')) ip = ip.Split(';')[0];
                return ip.Trim();
            }
        }
        return (HttpContext.Connection.RemoteIpAddress?.ToString() ?? "").Trim();
    }

    private string? GetUserAgent() =>
        HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
}
