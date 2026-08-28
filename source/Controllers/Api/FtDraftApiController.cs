using FamilyTree.Filters;
using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers.Api;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[ApiController]
[FtApiAuthorize]
[Route("api/FtDraft")]
public class FtDraftApiController : ControllerBase
{
    private readonly FtPersonDraftService _drafts;
    public FtDraftApiController(FtPersonDraftService drafts) => _drafts = drafts;

    [HttpGet]
    public async Task<IActionResult> List(string? q, int page = 1, CancellationToken ct = default)
    {
        var uid = FtApiHttp.UserId(HttpContext);
        var (rows, total, pages, p) = await _drafts.GetIndexPageAsync(uid, "FullName", q ?? "", "AmendDate", "1", page, 20, ct);
        return FtApiJson.Ok(new { rows, total, pages, page = p });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var row = await _drafts.GetAsync(id, FtApiHttp.UserId(HttpContext), ct);
        if (row == null) return FtApiJson.Fail("草稿不存在。", 404);
        return FtApiJson.Ok(_drafts.ToForm(row));
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] FtDraftFormVm model, CancellationToken ct)
    {
        try
        {
            _drafts.Normalize(model);
            var id = await _drafts.SaveDraftAsync(model, FtApiHttp.UserId(HttpContext), FtApiHttp.Operator(HttpContext), ct);
            return FtApiJson.Ok(new { id }, "已保存草稿。");
        }
        catch (InvalidOperationException ex)
        {
            return FtApiJson.Fail(ex.Message);
        }
    }

    /// <summary>入档预览：有主谱命中则返回 needConfirm+hints，不建档。</summary>
    [HttpPost("{id:int}/Complete")]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        try
        {
            var preview = await _drafts.BuildArchiveConfirmAsync(id, FtApiHttp.UserId(HttpContext), ct);
            if (preview.Hints.Count > 0)
                return FtApiJson.Ok(new { needConfirm = true, draftId = id, hints = preview.Hints }, "发现已入档相似人物，请确认是否按该人加入家族链。");

            var (pid, msg) = await _drafts.ArchiveAsNewAsync(id, FtApiHttp.UserId(HttpContext), FtApiHttp.Operator(HttpContext), ct);
            return FtApiJson.Ok(new { needConfirm = false, personId = pid }, msg);
        }
        catch (InvalidOperationException ex)
        {
            return FtApiJson.Fail(ex.Message);
        }
    }

    [HttpPost("{id:int}/ArchiveJoin")]
    public async Task<IActionResult> ArchiveJoin(int id, [FromQuery] int targetId, [FromQuery] byte? level, CancellationToken ct)
    {
        try
        {
            var (pid, msg, linkId) = await _drafts.ArchiveJoinChainAsync(id, targetId, level, FtApiHttp.UserId(HttpContext), FtApiHttp.Operator(HttpContext), ct);
            return FtApiJson.Ok(new { personId = pid, linkId }, msg);
        }
        catch (InvalidOperationException ex)
        {
            return FtApiJson.Fail(ex.Message);
        }
    }

    [HttpPost("{id:int}/ArchiveAsNew")]
    public async Task<IActionResult> ArchiveAsNew(int id, CancellationToken ct)
    {
        try
        {
            var (pid, msg) = await _drafts.ArchiveAsNewAsync(id, FtApiHttp.UserId(HttpContext), FtApiHttp.Operator(HttpContext), ct);
            return FtApiJson.Ok(new { personId = pid }, msg);
        }
        catch (InvalidOperationException ex)
        {
            return FtApiJson.Fail(ex.Message);
        }
    }
}
