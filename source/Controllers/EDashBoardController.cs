using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class EDashBoardController : Controller
{
    private readonly DashBoardService _service;

    public EDashBoardController(DashBoardService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> GetLayout(CancellationToken ct)
    {
        var uid = ResolveUserId();
        if (!uid.HasValue)
            return Json(new DashApiResult<DashLayoutVm> { Code = 401, Msg = "未登录" });
        return Json(await _service.GetLayoutAsync(uid.Value, ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetIndicatorData([FromBody] DashGetIndicatorDataRequest req, CancellationToken ct)
    {
        var uid = ResolveUserId();
        if (!uid.HasValue)
            return Json(new DashApiResult<object> { Code = 401, Msg = "未登录" });
        if (!ModelState.IsValid)
            return Json(new DashApiResult<object> { Code = 422, Msg = "参数校验失败" });
        return Json(await _service.GetIndicatorDataAsync(uid.Value, req, ct));
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableIndicators(CancellationToken ct)
    {
        var uid = ResolveUserId();
        if (!uid.HasValue)
            return Json(new DashApiResult<IReadOnlyList<DashIndicatorListRowVm>> { Code = 401, Msg = "未登录" });
        return Json(await _service.GetAvailableIndicatorsAsync(uid.Value, ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SwitchUserPos([FromBody] DashSwitchUserPosRequest req, CancellationToken ct)
    {
        var uid = ResolveUserId();
        if (!uid.HasValue)
            return Json(new DashApiResult<DashLayoutVm> { Code = 401, Msg = "未登录" });
        if (req.UserPosId <= 0)
            return Json(new DashApiResult<DashLayoutVm> { Code = 422, Msg = "请选择任岗" });
        return Json(await _service.SwitchUserPosAsync(uid.Value, req.UserPosId, GetOperator(), ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGlobalFilter([FromBody] DashGlobalFilterVm filter, CancellationToken ct)
    {
        var uid = ResolveUserId();
        if (!uid.HasValue)
            return Json(new DashApiResult<object> { Code = 401, Msg = "未登录" });
        if (!ModelState.IsValid)
            return Json(new DashApiResult<object> { Code = 422, Msg = "参数校验失败" });
        return Json(await _service.SaveGlobalFilterAsync(uid.Value, filter, GetOperator(), ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLayout([FromBody] DashSaveLayoutRequest req, CancellationToken ct)
    {
        var uid = ResolveUserId();
        if (!uid.HasValue)
            return Json(new DashApiResult<object> { Code = 401, Msg = "未登录" });
        if (!ModelState.IsValid)
            return Json(new DashApiResult<object> { Code = 422, Msg = "参数校验失败" });
        return Json(await _service.SaveLayoutAsync(uid.Value, req, GetOperator(), ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCard([FromBody] DashAddCardRequest req, CancellationToken ct)
    {
        var uid = ResolveUserId();
        if (!uid.HasValue)
            return Json(new DashApiResult<object> { Code = 401, Msg = "未登录" });
        if (!ModelState.IsValid)
            return Json(new DashApiResult<object> { Code = 422, Msg = "参数校验失败" });
        return Json(await _service.AddCardAsync(uid.Value, req, GetOperator(), ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCardHide([FromBody] DashSetCardHideRequest req, CancellationToken ct)
    {
        var uid = ResolveUserId();
        if (!uid.HasValue)
            return Json(new DashApiResult<object> { Code = 401, Msg = "未登录" });
        if (req.CardId <= 0)
            return Json(new DashApiResult<object> { Code = 422, Msg = "卡片ID无效" });
        return Json(await _service.SetCardHideAsync(uid.Value, req.CardId, req.IsHide, GetOperator(), ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCardTitle([FromBody] DashUpdateCardTitleRequest req, CancellationToken ct)
    {
        var uid = ResolveUserId();
        if (!uid.HasValue)
            return Json(new DashApiResult<object> { Code = 401, Msg = "未登录" });
        if (req.CardId <= 0 || string.IsNullOrWhiteSpace(req.CardTitle))
            return Json(new DashApiResult<object> { Code = 422, Msg = "参数校验失败" });
        return Json(await _service.UpdateCardTitleAsync(uid.Value, req.CardId, req.CardTitle.Trim(), GetOperator(), ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCard([FromBody] DashDeleteCardRequest req, CancellationToken ct)
    {
        var uid = ResolveUserId();
        if (!uid.HasValue)
            return Json(new DashApiResult<object> { Code = 401, Msg = "未登录" });
        if (req.CardId <= 0)
            return Json(new DashApiResult<object> { Code = 422, Msg = "卡片ID无效" });
        return Json(await _service.DeleteCardAsync(uid.Value, req.CardId, GetOperator(), ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetToPosDefault(CancellationToken ct)
    {
        var uid = ResolveUserId();
        if (!uid.HasValue)
            return Json(new DashApiResult<DashLayoutVm> { Code = 401, Msg = "未登录" });
        return Json(await _service.ResetToPosDefaultAsync(uid.Value, GetOperator(), ct));
    }

    private int? ResolveUserId()
    {
        return int.TryParse(User.FindFirst(FrameworkClaimTypes.EUserId)?.Value, out var id) ? id : null;
    }

    private string GetOperator() =>
        User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? User.Identity?.Name ?? "";
}

public sealed class DashSwitchUserPosRequest
{
    public int UserPosId { get; set; }
}

public sealed class DashSetCardHideRequest
{
    public int CardId { get; set; }
    public bool IsHide { get; set; }
}

public sealed class DashUpdateCardTitleRequest
{
    public int CardId { get; set; }
    public string CardTitle { get; set; } = "";
}

public sealed class DashDeleteCardRequest
{
    public int CardId { get; set; }
}
