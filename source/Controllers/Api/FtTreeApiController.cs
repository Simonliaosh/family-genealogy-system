using FamilyTree.Filters;
using FamilyTree.Helpers;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers.Api;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[ApiController]
[FtApiAuthorize]
[Route("api/FtTree")]
public class FtTreeApiController : ControllerBase
{
    private readonly FtTreeService _tree;
    private readonly FtMyProfileService _profile;
    private readonly FtPersonService _persons;
    public FtTreeApiController(FtTreeService tree, FtMyProfileService profile, FtPersonService persons)
    {
        _tree = tree;
        _profile = profile;
        _persons = persons;
    }

    [HttpGet("Roots")]
    public async Task<IActionResult> Roots(CancellationToken ct)
    {
        var uid = FtApiHttp.UserId(HttpContext);
        var (roots, allowIds, mainMode) = await _tree.ResolveScopeAsync(uid, _persons, ct);
        var preferred = await _tree.PickDefaultRootAsync(uid, roots, allowIds, ct);
        var list = await _tree.PreferPersonalRootInListAsync(uid, roots.ToList(), allowIds, preferred, ct);
        return FtApiJson.Ok(new
        {
            mainMode,
            defaultRootId = preferred,
            roots = list.Select(x => new { id = x.Id, name = x.Name }).ToList()
        });
    }

    [HttpGet("Main")]
    public async Task<IActionResult> Main(int? rootId, string? mode, CancellationToken ct)
    {
        var uid = FtApiHttp.UserId(HttpContext);
        var (roots, allowIds, mainMode) = await _tree.ResolveScopeAsync(uid, _persons, ct);
        var preferred = await _tree.PickDefaultRootAsync(uid, roots, allowIds, ct);
        var rid = rootId ?? preferred;
        if (rid > 0 && allowIds != null && !allowIds.Contains(rid))
            rid = preferred > 0 ? preferred : roots.FirstOrDefault().Id;
        if (rid <= 0)
            return FtApiJson.Ok(new { rootId = 0, mode = mode ?? "down", mainMode, node = (object?)null, siblings = Array.Empty<object>() });
        var packed = await PackAsync(rid, mode, ct, allowIds, mainMode);
        return FtApiJson.Ok(new { rootId = packed.RootId, mode = packed.Mode, mainMode, node = packed.Node, siblings = packed.Siblings });
    }

    [HttpGet("Mine")]
    public async Task<IActionResult> Mine(string? mode, CancellationToken ct)
    {
        var uid = FtApiHttp.UserId(HttpContext);
        var self = await _profile.BoundPersonAsync(uid, ct);
        if (self == null) return FtApiJson.Ok(new { bound = false });
        var (_, allowIds, mainMode) = await _tree.ResolveScopeAsync(uid, _persons, ct);
        // 「我的树」在未入主谱时强制个人小树范围
        if (!mainMode)
        {
            var mine = await _persons.OwnedOrBoundPersonsAsync(uid, ct);
            allowIds = mine.Select(x => x.DataId).ToHashSet();
        }
        var root = await _tree.PersonalRootAsync(uid, ct, allowIds) ?? self.DataId;
        if (allowIds != null && !allowIds.Contains(root))
            root = self.DataId;
        var packed = await PackAsync(self.DataId, mode, ct, allowIds, mainMode, downRoot: root);
        return FtApiJson.Ok(new
        {
            bound = true,
            mainMode,
            selfId = self.DataId,
            selfName = self.FullName,
            rootId = packed.RootId,
            mode = packed.Mode,
            node = packed.Node,
            siblings = packed.Siblings
        });
    }

    private async Task<(int RootId, string Mode, object? Node, object Siblings)> PackAsync(
        int id, string? mode, CancellationToken ct, HashSet<int>? allowIds, bool mainMode, int? downRoot = null)
    {
        var m = (mode ?? "down").Trim().ToLowerInvariant();
        if (m is "up" or "trace")
            return (id, "up", await _tree.BuildUpAsync(id, ct, allowIds), Array.Empty<object>());
        if (m is "side" or "lr")
            return (id, "side", null, await _tree.SiblingsAsync(id, ct, allowIds));
        var root = downRoot ?? id;
        return (root, "down", await _tree.BuildDownAsync(root, ct, withPeerStubs: mainMode, allowIds: allowIds, mainGenealogyOnly: mainMode), Array.Empty<object>());
    }
}
