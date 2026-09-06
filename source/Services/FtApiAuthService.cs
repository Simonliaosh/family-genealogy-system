using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FamilyTree.Configuration;
using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FamilyTree.Services;

public sealed class FtApiAuthService
{
    public const string HttpUserIdKey = "FtApi.UserId";

    private readonly FrameworkDbContext _db;
    private readonly FamilyTreeOptions _opt;
    private readonly FtAccountService _acc;
    private readonly IHttpClientFactory _http;

    public FtApiAuthService(
        FrameworkDbContext db,
        IOptions<FamilyTreeOptions> opt,
        FtAccountService acc,
        IHttpClientFactory http)
    {
        _db = db;
        _opt = opt.Value;
        _acc = acc;
        _http = http;
    }

    public bool WeChatConfigured =>
        !string.IsNullOrWhiteSpace(_opt.WeChat.AppId) && !string.IsNullOrWhiteSpace(_opt.WeChat.AppSecret);

    /// <summary>公众号网页授权是否已配置。</summary>
    public bool MpConfigured =>
        !string.IsNullOrWhiteSpace(_opt.WeChat.MpAppId) && !string.IsNullOrWhiteSpace(_opt.WeChat.MpAppSecret);

    public bool MpDevMock => _opt.WeChat.AllowDevMock && !MpConfigured;

    /// <summary>
    /// 匿名可读的公开配置。不再回传 <c>allowDevMock</c>——那等于向外界公告
    /// 「本站是否开着一个可用任意 openid 冒名登录的开关」。小程序改为始终渲染微信登录按钮。
    /// </summary>
    public object PublicConfig() => new
    {
        wechatEnabled = WeChatConfigured,
        mpEnabled = MpConfigured,
        publicBaseUrl = ResolvePublicBaseUrl()
    };

    public string ResolvePublicBaseUrl()
    {
        var a = (_opt.WeChat.PublicBaseUrl ?? "").Trim().TrimEnd('/');
        if (a.Length > 0) return a;
        return (_opt.PublicBaseUrl ?? "").Trim().TrimEnd('/');
    }

    /// <summary>生成公众号网页授权跳转地址（snsapi_base 静默）。</summary>
    public string BuildMpAuthorizeUrl(string redirectUri, string state)
    {
        var appId = (_opt.WeChat.MpAppId ?? "").Trim();
        return
            "https://open.weixin.qq.com/connect/oauth2/authorize?appid=" + Uri.EscapeDataString(appId) +
            "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
            "&response_type=code&scope=snsapi_base&state=" + Uri.EscapeDataString(state ?? "") +
            "#wechat_redirect";
    }

    /// <summary>公众号 code → OpenID；未配置时可开发模拟。</summary>
    public async Task<(bool Ok, string Msg, string? OpenId)> ResolveMpOpenIdAsync(
        string? code, string? mockOpenId, CancellationToken ct)
    {
        if (MpConfigured)
            return await MpOAuthAccessTokenAsync(code ?? "", ct);
        if (MpDevMock)
        {
            var mock = (mockOpenId ?? code ?? "").Trim();
            if (mock.Length < 4) return (false, "开发模拟请传入 mockOpenId（至少 4 位）。", null);
            var oid = mock.StartsWith("mp:", StringComparison.OrdinalIgnoreCase) ? mock : "mp:" + mock;
            return (true, "", oid);
        }
        return (false, "公众号未配置。请在 FamilyTree:WeChat 填写 MpAppId / MpAppSecret。", null);
    }

    /// <summary>公众号授权登录/自动注册，返回 Cookie 用用户（不发 API Token）。</summary>
    public async Task<(bool Ok, string Msg, EUser? User)> MpLoginOrRegisterAsync(
        string? code, string? mockOpenId, CancellationToken ct)
    {
        var got = await ResolveMpOpenIdAsync(code, mockOpenId, ct);
        if (!got.Ok) return (false, got.Msg, null);
        var (ok, msg, user) = await _acc.EnsureWxUserAsync(got.OpenId!, ct);
        if (!ok) return (false, msg, null);
        return (true, "", user);
    }

    public async Task<(bool Ok, string Msg, string? Token, int UserId, string RealName, bool NeedIdCard)> WeChatLoginAsync(
        string? code, string? mockOpenId, CancellationToken ct)
    {
        string openId;
        if (WeChatConfigured)
        {
            var got = await Code2SessionAsync(code ?? "", ct);
            if (!got.Ok) return (false, got.Msg, null, 0, "", false);
            openId = got.OpenId!;
        }
        else if (_opt.WeChat.AllowDevMock)
        {
            var mock = (mockOpenId ?? "").Trim();
            if (mock.Length == 0) mock = (code ?? "").Trim();
            if (mock.Length < 4) return (false, "开发模拟登录请传入 mockOpenId 或 code。", null, 0, "", false);
            openId = mock.StartsWith("dev:", StringComparison.OrdinalIgnoreCase) ? mock : "dev:" + mock;
        }
        else
            return (false, "微信未配置。请用身份证登录，或在 FamilyTree:WeChat 填写 AppId/AppSecret。", null, 0, "", false);

        var (ok, msg, user) = await _acc.EnsureWxUserAsync(openId, ct);
        if (!ok) return (false, msg, null, 0, "", false);
        var token = await IssueTokenAsync(user.DataId, "wx", ct);
        var bind = await _db.FtAccountBinds.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user.DataId && !x.IsDeleted, ct);
        var need = string.IsNullOrEmpty(bind?.IdCardHash);
        return (true, "", token, user.DataId, user.RealName, need);
    }

    public async Task<(bool Ok, string Msg, string? Token, int UserId, string RealName, bool NeedIdCard)> IdCardLoginAsync(
        string? idCard, string? password, CancellationToken ct)
    {
        var login = (idCard ?? "").Trim();
        var pwd = password ?? "";
        EUser? user = null;
        const string fail = "登录名或密码不正确。";
        if (FtText.IsIdCard(login))
        {
            var r = await _acc.TryPasswordLoginByIdCardAsync(login, pwd, ct);
            if (!r.Ok) return (false, r.Msg, null, 0, "", false);
            user = r.User;
        }
        else
        {
            var name = FtText.NormalizeLoginName(login);
            user = await _db.EUsers.FirstOrDefaultAsync(x => x.LoginId == name && !x.IsDeleted, ct);
            if (user == null || !LoginAttempts.IsUsable(user))
                return (false, fail, null, 0, "", false);
            if (!PasswordHasher.Verify(pwd, user.PwdHash, user.PasswordAlgo))
            {
                // 与 MVC 登录路径一致地累加失败次数并在超阈值时锁定
                LoginAttempts.MarkFailure(user);
                await _db.SaveChangesAsync(ct);
                return (false, fail, null, 0, "", false);
            }
            LoginAttempts.MarkSuccess(user);
            await _db.SaveChangesAsync(ct);
        }

        var token = await IssueTokenAsync(user!.DataId, "id", ct);
        var bind = await _db.FtAccountBinds.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user.DataId && !x.IsDeleted, ct);
        return (true, "", token, user.DataId, user.RealName, string.IsNullOrEmpty(bind?.IdCardHash));
    }

    public async Task<(bool Ok, string Msg, string? Token, int UserId)> RegisterAsync(FtMemberRegisterVm m, CancellationToken ct)
    {
        var (ok, msg, user) = await _acc.RegisterAsync(m, ct);
        if (!ok || user == null) return (false, msg, null, 0);
        var token = await IssueTokenAsync(user.DataId, "reg", ct);
        return (true, msg, token, user.DataId);
    }

    public async Task<(bool Ok, string Msg)> BindIdCardAsync(int userId, string idCard, CancellationToken ct) =>
        await _acc.BindIdCardToUserAsync(userId, idCard, ct);

    public async Task<(bool Ok, string Msg)> BindWeChatAsync(int userId, string? code, string? mockOpenId, CancellationToken ct)
    {
        string openId;
        if (WeChatConfigured)
        {
            var got = await Code2SessionAsync(code ?? "", ct);
            if (!got.Ok) return (false, got.Msg);
            openId = got.OpenId!;
        }
        else if (_opt.WeChat.AllowDevMock)
        {
            var mock = (mockOpenId ?? code ?? "").Trim();
            if (mock.Length < 4) return (false, "开发模拟绑定请传入 mockOpenId。");
            openId = mock.StartsWith("dev:", StringComparison.OrdinalIgnoreCase) ? mock : "dev:" + mock;
        }
        else
            return (false, "微信未配置。");
        return await _acc.BindWechatToUserAsync(userId, openId, ct);
    }

    public async Task<string> IssueTokenAsync(int userId, string remark, CancellationToken ct)
    {
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var now = DateTime.Now;
        var days = _opt.ApiTokenDays < 1 ? 30 : _opt.ApiTokenDays;
        _db.FtApiTokens.Add(new FtApiToken
        {
            UserId = userId,
            TokenHash = HashToken(raw),
            ExpireDate = now.AddDays(days),
            CreateDate = now,
            AmendDate = now,
            OperatorName = "FT-API",
            Remark = FtText.Clip(remark, 512)
        });
        await _db.SaveChangesAsync(ct);
        return raw;
    }

    public async Task<int?> ValidateTokenAsync(string? token, CancellationToken ct)
    {
        var t = (token ?? "").Trim();
        if (t.Length < 32) return null;
        var hash = HashToken(t);
        var now = DateTime.Now;
        var row = await _db.FtApiTokens.AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.TokenHash == hash && x.ExpireDate > now, ct);
        return row?.UserId;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private async Task<(bool Ok, string Msg, string? OpenId)> Code2SessionAsync(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return (false, "缺少微信登录 code。", null);
        var url =
            "https://api.weixin.qq.com/sns/jscode2session?appid=" + Uri.EscapeDataString(_opt.WeChat.AppId) +
            "&secret=" + Uri.EscapeDataString(_opt.WeChat.AppSecret) +
            "&js_code=" + Uri.EscapeDataString(code.Trim()) +
            "&grant_type=authorization_code";
        return await FetchWeChatOpenIdAsync(url, ct);
    }

    private async Task<(bool Ok, string Msg, string? OpenId)> MpOAuthAccessTokenAsync(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return (false, "缺少公众号授权 code。", null);
        var url =
            "https://api.weixin.qq.com/sns/oauth2/access_token?appid=" + Uri.EscapeDataString(_opt.WeChat.MpAppId) +
            "&secret=" + Uri.EscapeDataString(_opt.WeChat.MpAppSecret) +
            "&code=" + Uri.EscapeDataString(code.Trim()) +
            "&grant_type=authorization_code";
        return await FetchWeChatOpenIdAsync(url, ct);
    }

    private async Task<(bool Ok, string Msg, string? OpenId)> FetchWeChatOpenIdAsync(string url, CancellationToken ct)
    {
        try
        {
            var client = _http.CreateClient("WeChat");
            using var resp = await client.GetAsync(url, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            var r = doc.RootElement;
            if (r.TryGetProperty("errcode", out var ec) && ec.TryGetInt32(out var codeNum) && codeNum != 0)
            {
                var em = r.TryGetProperty("errmsg", out var emEl) ? emEl.GetString() : "";
                return (false, "微信登录失败：" + (em ?? codeNum.ToString()), null);
            }
            var openId = r.TryGetProperty("openid", out var oid) ? oid.GetString() : null;
            if (string.IsNullOrWhiteSpace(openId))
                return (false, "微信未返回 OpenID。", null);
            return (true, "", openId);
        }
        catch (Exception ex)
        {
            return (false, "调用微信失败：" + ex.Message, null);
        }
    }
}
