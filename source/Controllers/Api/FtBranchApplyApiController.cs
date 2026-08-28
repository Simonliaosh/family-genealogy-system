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
[Route("api/FtBranchApply")]
public class FtBranchApplyApiController : ControllerBase
{
    private readonly FtBranchAdminApplyService _service;
    public FtBranchApplyApiController(FtBranchAdminApplyService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var uid = FtApiHttp.UserId(HttpContext);
        var status = await _service.StatusForAsync(uid, ct);
        var (rows, _, _, _) = await _service.GetIndexPageAsync(uid, null, 1, 20, ct);
        return FtApiJson.Ok(new
        {
            status,
            rows = rows.Select(x => new
            {
                id = x.DataId,
                status = x.ApplyStatus,
                reason = x.ApplyReason,
                remark = x.Remark,
                createDate = x.CreateDate
            })
        });
    }

    public sealed class ApplyBody
    {
        public string? Reason { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Apply([FromBody] ApplyBody body, CancellationToken ct)
    {
        try
        {
            var msg = await _service.ApplyAsync(FtApiHttp.UserId(HttpContext), body.Reason, FtApiHttp.Operator(HttpContext), ct);
            return FtApiJson.Ok(null, msg);
        }
        catch (InvalidOperationException ex)
        {
            return FtApiJson.Fail(ex.Message);
        }
    }
}
