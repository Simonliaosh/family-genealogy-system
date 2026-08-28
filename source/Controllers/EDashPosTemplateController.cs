using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EDashPosTemplateController : Controller
{
    private readonly DashPosTemplateService _service;

    public EDashPosTemplateController(DashPosTemplateService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? posId, CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim) || !FunctionLimitUi.CanView(lim, false)) return Forbid();

        var vm = await _service.GetIndexAsync(posId ?? 0, ct);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBatch(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] DashPosTemplateSaveBatchVm? body,
        [FromForm] DashPosTemplateSaveBatchVm? form,
        CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim) || !FunctionLimitUi.CanUpdate(lim, false))
        {
            if (IsJsonRequest())
                return Json(new DashApiResult<object> { Code = 403, Msg = "无权限" });
            return Forbid();
        }

        var model = body ?? form;
        if (model == null || model.PosId <= 0)
        {
            if (IsJsonRequest())
                return Json(new DashApiResult<object> { Code = 422, Msg = "参数校验失败" });
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            if (IsJsonRequest())
                return Json(new DashApiResult<object> { Code = 422, Msg = "参数校验失败" });
            var vm = await _service.GetIndexAsync(model.PosId, ct);
            ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
            return View(nameof(Index), vm);
        }

        var op = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
        var result = await _service.SaveBatchAsync(model, op, ct);

        if (IsJsonRequest())
            return Json(result);

        if (result.Code == 200)
            return RedirectToAction(nameof(Index), new { posId = model.PosId });
        var indexVm = await _service.GetIndexAsync(model.PosId, ct);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.SaveError = result.Msg;
        return View(nameof(Index), indexVm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetToDefault(int posId, CancellationToken ct = default)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim) || !FunctionLimitUi.CanUpdate(lim, false))
        {
            if (IsJsonRequest())
                return Json(new DashApiResult<object> { Code = 403, Msg = "无权限" });
            return Forbid();
        }

        if (posId <= 0)
        {
            if (IsJsonRequest())
                return Json(new DashApiResult<object> { Code = 422, Msg = "请选择岗位" });
            return BadRequest();
        }

        var op = User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
        var result = await _service.ResetToDefaultAsync(posId, op, ct);

        if (IsJsonRequest())
            return Json(result);

        if (result.Code == 200)
            return RedirectToAction(nameof(Index), new { posId });

        var vm = await _service.GetIndexAsync(posId, ct);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.SaveError = result.Msg;
        return View(nameof(Index), vm);
    }

    private bool IsJsonRequest() =>
        Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true;
}
