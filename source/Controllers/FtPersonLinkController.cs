using FamilyTree.Configuration;
using FamilyTree.Helpers;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FamilyTree.Controllers;

[Authorize]
public class FtPersonLinkController : Controller
{
    private readonly FtPersonLinkService _service;
    private readonly FtMatchService _match;
    private readonly FtPersonService _persons;
    private readonly FamilyTreeOptions _opt;
    public FtPersonLinkController(FtPersonLinkService service, FtMatchService match, FtPersonService persons, IOptions<FamilyTreeOptions> opt)
    {
        _service = service;
        _match = match;
        _persons = persons;
        _opt = opt.Value;
    }

    [HttpGet, HttpPost]
    public async Task<IActionResult> Index(string? status1, int? intPage, int? pageShowNum, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        var (rows, total, pages, p) = await _service.GetIndexPageAsync(uid, status1, intPage.GetValueOrDefault(1), pageShowNum.GetValueOrDefault(16), ct);
        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.Status1 = status1 ?? "";
        ViewBag.IntPage = p;
        ViewBag.PageShowNum = pageShowNum.GetValueOrDefault(16);
        ViewBag.TotalPages = pages;
        ViewBag.TotalRecords = total;
        ViewBag.SearchField = "LinkStatus";
        ViewBag.SearchContent = "";
        ViewBag.SelectField = "CreateDate";
        ViewBag.SelectFieldArrow = "1";
        return View(rows);
    }

    /// <summary>保存后的确认链入引导：只看该人 vs 主谱命中。</summary>
    [HttpGet]
    public async Task<IActionResult> Suggest(int sourceId, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var src = await _persons.GetAsync(sourceId, ct);
        if (src == null || !await _persons.CanViewAsync(uid, src, ct)) return NotFound();
        if (src.SameAsPersonId.HasValue)
            return RedirectToAction("Edit", "FtPerson", new { id = src.SameAsPersonId.Value });

        var hints = (await _match.MatchPersonAsync(sourceId, ct)).Where(h => h.InMain).OrderBy(h => h.Level).ToList();
        if (hints.Count == 0)
        {
            if (TempData["SuccessMessage"] == null)
                TempData["SuccessMessage"] = "暂无主谱命中，已回到人物详情。";
            return RedirectToAction("Edit", "FtPerson", new { id = sourceId });
        }
        if (hints.Count == 1 && hints[0].Level <= 2)
            return RedirectToAction(nameof(Confirm), new { sourceId, targetId = hints[0].PersonId, level = (byte)hints[0].Level });

        ViewBag.CanApply = FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)
            || FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false);
        ViewBag.SourceId = sourceId;
        ViewBag.SourceName = src.FullName;
        ViewBag.SourceBirth = src.BirthDate;
        ViewBag.SourceFather = src.FatherName;
        ViewBag.SourceMother = src.MotherName;
        return View(hints);
    }

    /// <summary>扫描我录入的人物 vs 主谱，按姓名/父母/出生年匹配后可点确认链入。</summary>
    [HttpGet]
    public async Task<IActionResult> Match(CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var mine = await _persons.OwnedOrBoundPersonsAsync(uid, ct);
        // 只扫尚未等同主谱、且本人侧未标「已链入身份」的人
        var sources = mine.Where(x => !x.SameAsPersonId.HasValue).OrderBy(x => x.FullName).ToList();
        var rows = await _match.ScanForMainLinksAsync(sources, ct);
        ViewBag.CanApply = FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)
            || FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false);
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> Confirm(int sourceId, int targetId, byte? level, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)
            && !FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)
            && !FunctionLimitUi.CanView(HttpContext.Items["PubFunctionLimit"] as string, false))
            return Forbid();
        try
        {
            var vm = await _service.BuildConfirmAsync(sourceId, targetId, level, FtClaims.UserId(User) ?? 0, ct);
            return View(vm);
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Edit", "FtPerson", new { id = sourceId });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(int sourceId, int targetId, byte? level, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)
            && !FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false))
            return Forbid();
        try
        {
            var (linkId, msg) = await _service.ApplyAsync(sourceId, targetId, FtClaims.UserId(User) ?? 0, FtClaims.Operator(User), level, ct);
            TempData["SuccessMessage"] = msg;
            if (linkId > 0)
                return RedirectToAction(nameof(ApplyQr), new { id = linkId });
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Match));
    }

    /// <summary>申请人展示链入申请二维码，发给超管/分支管扫码审批。</summary>
    [HttpGet]
    public async Task<IActionResult> ApplyQr(int id, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        try
        {
            await _service.EnsureApplicantCanViewQrAsync(id, uid, ct);
            var vm = await _service.BuildScanAsync(id, ct);
            if (vm == null) return NotFound();
            ViewBag.ScanUrl = AbsUrl(Url.Action("Open", "FtLinkAudit", new { id }) ?? $"/FtLinkAudit/Open?id={id}");
            return View(vm);
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> ApplyQrImg(int id, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        try
        {
            await _service.EnsureApplicantCanViewQrAsync(id, uid, ct);
            var url = AbsUrl(Url.Action("Open", "FtLinkAudit", new { id }) ?? $"/FtLinkAudit/Open?id={id}");
            return File(FtQrPng.PngBytes(url, 8), "image/png");
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    private string AbsUrl(string pathOrUrl)
    {
        if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return pathOrUrl;
        var root = (_opt.PublicBaseUrl ?? "").Trim().TrimEnd('/');
        if (root.Length == 0)
            root = $"{Request.Scheme}://{Request.Host.Value}".TrimEnd('/');
        if (!pathOrUrl.StartsWith('/')) pathOrUrl = "/" + pathOrUrl;
        return root + pathOrUrl;
    }

    /// <summary>标记不是同一人；之后匹配跳过该对。</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NotMatch(int sourceId, int targetId, string? returnTo, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var src = await _persons.GetAsync(sourceId, ct);
        if (src == null || !await _persons.CanViewAsync(uid, src, ct)) return Forbid();
        try
        {
            await _match.MarkNotMatchAsync(sourceId, targetId, uid, FtClaims.Operator(User), null, ct);
            TempData["SuccessMessage"] = "已排除该匹配，以后不再提示这对人。";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }

        if (string.Equals(returnTo, "Suggest", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Suggest), new { sourceId });
        if (string.Equals(returnTo, "Confirm", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Edit", "FtPerson", new { id = sourceId });
        return RedirectToAction(nameof(Match));
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}

[Authorize]
public class FtLinkAuditController : Controller
{
    private readonly FtPersonLinkService _service;
    private readonly FtDutyAccess _duty;
    public FtLinkAuditController(FtPersonLinkService service, FtDutyAccess duty)
    {
        _service = service;
        _duty = duty;
    }

    [HttpGet, HttpPost]
    public async Task<IActionResult> Index(string? status1, int? intPage, int? pageShowNum, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var st = string.IsNullOrWhiteSpace(status1) ? "PENDING" : status1;
        var (rows, total, pages, p) = await _service.GetIndexPageAsync(uid, st, intPage.GetValueOrDefault(1), pageShowNum.GetValueOrDefault(16), ct);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false);
        ViewBag.Status1 = st;
        ViewBag.IntPage = p;
        ViewBag.PageShowNum = pageShowNum.GetValueOrDefault(16);
        ViewBag.TotalPages = pages;
        ViewBag.TotalRecords = total;
        ViewBag.SearchField = "LinkStatus";
        ViewBag.SearchContent = "";
        ViewBag.SelectField = "CreateDate";
        ViewBag.SelectFieldArrow = "1";
        return View(rows);
    }

    /// <summary>扫码打开：超管/分支管对照后通过或驳回。</summary>
    [HttpGet]
    public async Task<IActionResult> Open(int id, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        // 与 ApplyQr 用同一条归属校验：申请人本人或本族管理岗，否则枚举 id 即可读两侧人物档案
        try
        {
            await _service.EnsureApplicantCanViewQrAsync(id, uid, ct);
        }
        catch (InvalidOperationException ex)
        {
            ViewBag.Err = ex.Message;
            return View(model: null);
        }

        var vm = await _service.BuildScanAsync(id, ct);
        if (vm == null)
        {
            ViewBag.Err = "链入申请不存在或已删除。";
            return View(model: null);
        }
        ViewBag.CanApprove = FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)
            && await _duty.IsBranchAdminAsync(uid, ct);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, bool pass, string? remark, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var (ok, msg) = await _service.ApproveAsync(id, pass, FtClaims.UserId(User) ?? 0, FtClaims.Operator(User), remark, ct);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
        // 扫码页提交后仍回到扫码结果页
        if (Request.Headers.Referer.ToString().Contains("/FtLinkAudit/Open", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Open), new { id });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlink(int id, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var (ok, msg) = await _service.UnlinkAsync(id, FtClaims.UserId(User) ?? 0, FtClaims.Operator(User), ct);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
        return RedirectToAction(nameof(Index));
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}

[Authorize]
public class FtConflictController : Controller
{
    private readonly FtConflictService _service;
    public FtConflictController(FtConflictService service) => _service = service;

    [HttpGet, HttpPost]
    public async Task<IActionResult> Index(string? status1, int? intPage, int? pageShowNum, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var (rows, total, pages, p) = await _service.GetIndexPageAsync(uid, status1, intPage.GetValueOrDefault(1), pageShowNum.GetValueOrDefault(16), ct);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false);
        ViewBag.Status1 = status1 ?? "";
        ViewBag.IntPage = p;
        ViewBag.PageShowNum = pageShowNum.GetValueOrDefault(16);
        ViewBag.TotalPages = pages;
        ViewBag.TotalRecords = total;
        ViewBag.SearchField = "ConflictType";
        ViewBag.SearchContent = "";
        ViewBag.SelectField = "CreateDate";
        ViewBag.SelectFieldArrow = "1";
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int id, string status, string? remark, CancellationToken ct)
    {
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        try { await _service.ResolveAsync(id, status, FtClaims.UserId(User) ?? 0, remark ?? "", ct); TempData["SuccessMessage"] = "已处理。"; }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
