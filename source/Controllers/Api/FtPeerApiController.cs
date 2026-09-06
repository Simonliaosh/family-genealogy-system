using FamilyTree.Filters;
using FamilyTree.Helpers;
using FamilyTree.Services;
using FamilyTree.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FamilyTree.Controllers.Api;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[ApiController]
[Route("api/FtPeer")]
// 三个端点全部匿名可达（邀请码信息、核销邀请、按令牌拉子女），加限流挡住码枚举
[EnableRateLimiting(RateLimitPolicies.AnonymousApi)]
public class FtPeerApiController : ControllerBase
{
    private readonly FtPeerService _peers;
    public FtPeerApiController(FtPeerService peers) => _peers = peers;

    [HttpGet("InviteInfo")]
    [FtApiAllowAnonymous]
    public async Task<IActionResult> InviteInfo(string? c, CancellationToken ct)
    {
        var info = await _peers.PublicInviteInfoAsync(c, ct);
        return info == null ? FtApiJson.Fail("邀请码无效或已过期。") : FtApiJson.Ok(info);
    }

    [HttpPost("RedeemInvite")]
    [FtApiAllowAnonymous]
    public async Task<IActionResult> RedeemInvite([FromBody] FtPeerService.RedeemInviteBody body, CancellationToken ct)
    {
        try
        {
            var data = await _peers.RedeemInviteApiAsync(body, ct);
            return FtApiJson.Ok(data);
        }
        catch (InvalidOperationException ex)
        {
            return FtApiJson.Fail(ex.Message);
        }
    }

    [HttpGet("Children")]
    [FtApiAllowAnonymous]
    public async Task<IActionResult> Children(int personId, CancellationToken ct)
    {
        try
        {
            var header = Request.Headers.Authorization.ToString();
            var kids = await _peers.ChildrenForPeerTokenAsync(header, personId, ct);
            return FtApiJson.Ok(kids.Select(x => new
            {
                id = x.Id,
                name = x.Name,
                birth = x.Birth,
                gender = x.Gender,
                hasChildren = x.HasChildren
            }));
        }
        catch (InvalidOperationException ex)
        {
            return FtApiJson.Fail(ex.Message, 403);
        }
    }

    public sealed class RevokeByPeerBody
    {
        public string? PeerSiteId { get; set; }
        public int PeerPersonId { get; set; }
        public int LocalPersonId { get; set; }
    }

    [HttpPost("RevokeByPeer")]
    [FtApiAllowAnonymous]
    public async Task<IActionResult> RevokeByPeer([FromBody] RevokeByPeerBody body, CancellationToken ct)
    {
        var header = Request.Headers.Authorization.ToString();
        await _peers.RevokeByPeerApiAsync(header, body.LocalPersonId, body.PeerPersonId, body.PeerSiteId ?? "", ct);
        return FtApiJson.Ok(null, "ok");
    }
}
