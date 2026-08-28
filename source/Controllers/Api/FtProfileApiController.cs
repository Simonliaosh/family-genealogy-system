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
[Route("api/FtProfile")]
public class FtProfileApiController : ControllerBase
{
    private readonly FtMyProfileService _profile;
    private readonly FtPhotoService _photos;
    public FtProfileApiController(FtMyProfileService profile, FtPhotoService photos)
    {
        _profile = profile;
        _photos = photos;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var uid = FtApiHttp.UserId(HttpContext);
        var p = await _profile.BoundPersonAsync(uid, ct);
        if (p == null)
            return FtApiJson.Ok(new { bound = false });
        var list = await _photos.ListVisibleAsync(p, uid, ct);
        return FtApiJson.Ok(new
        {
            bound = true,
            personId = p.DataId,
            fullName = p.FullName,
            nickName = p.NickName,
            selfIntro = p.SelfIntro,
            wechatId = p.WechatId,
            privacyLevel = p.PrivacyLevel,
            showPhoto = p.ShowPhoto,
            showWechat = p.ShowWechat,
            showSelfIntro = p.ShowSelfIntro,
            showBirthDetail = p.ShowBirthDetail,
            showResume = p.ShowResume,
            printAllow = p.PrintAllow,
            photos = list
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] FtProfileFormVm model, CancellationToken ct)
    {
        try
        {
            await _profile.SaveAsync(model, FtApiHttp.UserId(HttpContext), ct);
            return FtApiJson.Ok(null, "已保存。");
        }
        catch (InvalidOperationException ex)
        {
            return FtApiJson.Fail(ex.Message);
        }
    }

    [HttpPost("UploadPhoto")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPhoto([FromForm] IFormFile? file, [FromForm] int? personId, CancellationToken ct)
    {
        var uid = FtApiHttp.UserId(HttpContext);
        try
        {
            var pid = personId.GetValueOrDefault();
            if (pid <= 0)
            {
                var p = await _profile.BoundPersonAsync(uid, ct)
                    ?? throw new InvalidOperationException("尚未绑定本人档案。");
                pid = p.DataId;
            }
            if (file == null) return FtApiJson.Fail("请选择照片。");
            var path = await _photos.UploadAsync(pid, uid, file, ct);
            return FtApiJson.Ok(new { path });
        }
        catch (InvalidOperationException ex)
        {
            return FtApiJson.Fail(ex.Message);
        }
    }

    [HttpPost("DeletePhoto")]
    public async Task<IActionResult> DeletePhoto([FromBody] DeletePhotoBody body, CancellationToken ct)
    {
        var uid = FtApiHttp.UserId(HttpContext);
        try
        {
            var pid = body.PersonId;
            if (pid <= 0)
            {
                var p = await _profile.BoundPersonAsync(uid, ct)
                    ?? throw new InvalidOperationException("尚未绑定本人档案。");
                pid = p.DataId;
            }
            await _photos.DeleteAsync(pid, uid, body.Path, ct);
            return FtApiJson.Ok(null, "已删除。");
        }
        catch (InvalidOperationException ex)
        {
            return FtApiJson.Fail(ex.Message);
        }
    }

    public sealed class DeletePhotoBody
    {
        public int PersonId { get; set; }
        public string? Path { get; set; }
    }
}
