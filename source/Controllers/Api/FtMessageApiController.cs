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
[Route("api/FtMessage")]
public class FtMessageApiController : ControllerBase
{
    private readonly ETodoTaskService _todos;
    private readonly FtPersonLinkService _links;
    public FtMessageApiController(ETodoTaskService todos, FtPersonLinkService links)
    {
        _todos = todos;
        _links = links;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var uid = FtApiHttp.UserId(HttpContext);
        var (todos, _, _, _) = await _todos.GetIndexPageAsync(uid, "", "", "TodoTitle", "", "CreateTime", "1", 1, 30, ct);
        var (links, _, _, _) = await _links.GetIndexPageAsync(uid, null, 1, 30, ct);
        return FtApiJson.Ok(new
        {
            todos = todos.Select(x => new
            {
                id = x.DataId,
                title = x.TitleDisplay,
                status = x.StatusText,
                eventCode = x.EventCode,
                createTime = x.CreateTime
            }),
            links = links.Select(x => new
            {
                id = x.DataId,
                sourceName = x.SourceName,
                targetName = x.TargetName,
                status = x.LinkStatus,
                level = x.MatchLevel,
                createDate = x.CreateDate
            })
        });
    }
}
