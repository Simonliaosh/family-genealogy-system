using FamilyTree.Configuration;
using FamilyTree.Filters;
using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FamilyTree.Controllers.Api;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[ApiController]
[Route("api/FtAuth")]
public class FtAuthApiController : ControllerBase
{
    private readonly FtApiAuthService _auth;
    public FtAuthApiController(FtApiAuthService auth) => _auth = auth;

    [HttpGet("Config")]
    [FtApiAllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AnonymousApi)]
    public IActionResult Config() => FtApiJson.Ok(_auth.PublicConfig());

    public sealed class WeChatLoginBody
    {
        public string? Code { get; set; }
        public string? MockOpenId { get; set; }
    }

    [HttpPost("WeChatLogin")]
    [FtApiAllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> WeChatLogin([FromBody] WeChatLoginBody body, CancellationToken ct)
    {
        var r = await _auth.WeChatLoginAsync(body.Code, body.MockOpenId, ct);
        if (!r.Ok) return FtApiJson.Fail(r.Msg);
        return FtApiJson.Ok(new { token = r.Token, userId = r.UserId, realName = r.RealName, needIdCard = r.NeedIdCard });
    }

    public sealed class IdLoginBody
    {
        public string? IdCard { get; set; }
        public string? Password { get; set; }
    }

    [HttpPost("IdCardLogin")]
    [FtApiAllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> IdCardLogin([FromBody] IdLoginBody body, CancellationToken ct)
    {
        var r = await _auth.IdCardLoginAsync(body.IdCard, body.Password, ct);
        if (!r.Ok) return FtApiJson.Fail(r.Msg);
        return FtApiJson.Ok(new { token = r.Token, userId = r.UserId, realName = r.RealName, needIdCard = r.NeedIdCard });
    }

    [HttpPost("Register")]
    [FtApiAllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> Register([FromBody] FtMemberRegisterVm body, CancellationToken ct)
    {
        var r = await _auth.RegisterAsync(body, ct);
        if (!r.Ok) return FtApiJson.Fail(r.Msg);
        return FtApiJson.Ok(new { token = r.Token, userId = r.UserId }, r.Msg);
    }

    [HttpPost("BindIdCard")]
    [FtApiAuthorize]
    public async Task<IActionResult> BindIdCard([FromBody] IdLoginBody body, CancellationToken ct)
    {
        var uid = FtApiHttp.UserId(HttpContext);
        var r = await _auth.BindIdCardAsync(uid, body.IdCard ?? "", ct);
        return r.Ok ? FtApiJson.Ok(null, r.Msg) : FtApiJson.Fail(r.Msg);
    }

    [HttpPost("BindWeChat")]
    [FtApiAuthorize]
    public async Task<IActionResult> BindWeChat([FromBody] WeChatLoginBody body, CancellationToken ct)
    {
        var uid = FtApiHttp.UserId(HttpContext);
        var r = await _auth.BindWeChatAsync(uid, body.Code, body.MockOpenId, ct);
        return r.Ok ? FtApiJson.Ok(null, r.Msg) : FtApiJson.Fail(r.Msg);
    }
}
