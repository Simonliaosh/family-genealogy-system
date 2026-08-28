using FamilyTree.Helpers;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

[Authorize]
public class FtTreeController : Controller
{
    private readonly FtTreeService _tree;
    private readonly FtPeerService _peers;
    private readonly FtPersonService _persons;
    public FtTreeController(FtTreeService tree, FtPeerService peers, FtPersonService persons)
    {
        _tree = tree;
        _peers = peers;
        _persons = persons;
    }

    [HttpGet, HttpPost]
    public async Task<IActionResult> Index(int? rootId, string? scope, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        await _persons.ClearInMainOnPendingLinkSourcesAsync(ct);

        var isStaff = await _persons.IsStaffAsync(uid, ct);
        var joinedMain = await _persons.UserHasJoinedMainAsync(uid, ct);
        var scopeKey = (scope ?? "").Trim().ToLowerInvariant();

        // 普通族人：未真正入主谱时强制只能看小树（忽略 mine/all）
        if (!isStaff && !joinedMain)
            scopeKey = "personal";
        else if (string.IsNullOrEmpty(scopeKey))
            scopeKey = (isStaff || joinedMain) ? "mine" : "personal";

        // 我录入的小树：严格本人 Owner/Bind，管理岗也不扩成全库
        if (scopeKey == "personal")
        {
            var mine = await _persons.OwnedOrBoundPersonsStrictAsync(uid, ct);
            var (_, allow, personalRoots) = await _tree.BuildPersonalTreeScopeAsync(mine, ct);
            var preferred = await _tree.PickDefaultRootAsync(uid, personalRoots, allow, ct);
            if (preferred <= 0 && personalRoots.Count > 0) preferred = personalRoots[0].Id;
            var rid = rootId ?? preferred;
            if (rid > 0 && !allow.Contains(rid))
                rid = preferred > 0 ? preferred : personalRoots.FirstOrDefault().Id;

            ViewBag.MainMode = false;
            ViewBag.PersonalMode = true;
            ViewBag.JoinedMain = joinedMain;
            ViewBag.IsStaff = isStaff;
            ViewBag.Scope = "personal";
            ViewBag.Roots = personalRoots;
            ViewBag.RootId = rid;
            ViewBag.Forest = null;
            FtTreeNodeVm? personalTree = rid > 0
                ? await _tree.BuildDownAsync(rid, ct, withPeerStubs: false, allowIds: allow, mainGenealogyOnly: false)
                : null;
            // 主谱引用节点加标记
            if (personalTree != null)
                MarkMainRefNodes(personalTree, mine.Select(x => x.DataId).ToHashSet());
            return View(personalTree);
        }

        var (roots, allowIds, mainMode) = await _tree.ResolveScopeAsync(uid, _persons, ct);
        if (mainMode)
        {
            var healed = await _tree.HealParentEdgesAsync(allowIds, ct);
            if (healed > 0)
                (roots, allowIds, mainMode) = await _tree.ResolveScopeAsync(uid, _persons, ct);
        }
        var preferredMain = await _tree.PickDefaultRootAsync(uid, roots, allowIds, ct);
        var forestRoots = roots.ToList();
        var displayRoots = await _tree.PreferPersonalRootInListAsync(uid, roots.ToList(), allowIds, preferredMain, ct);

        var fullForest = mainMode && scopeKey == "all";
        ViewBag.MainMode = mainMode;
        ViewBag.PersonalMode = false;
        ViewBag.JoinedMain = joinedMain;
        ViewBag.IsStaff = isStaff;
        ViewBag.Scope = fullForest ? "all" : "mine";
        ViewBag.Roots = displayRoots;

        if (fullForest)
        {
            ViewBag.RootId = 0;
            var forest = new List<FtTreeNodeVm>();
            var seen = new HashSet<int>();
            foreach (var r in forestRoots)
            {
                if (!seen.Add(r.Id)) continue;
                var node = await _tree.BuildDownAsync(r.Id, ct, withPeerStubs: true, allowIds: allowIds, mainGenealogyOnly: true);
                if (node != null) forest.Add(node);
            }
            ViewBag.Forest = forest;
            return View(model: null);
        }

        var ridMain = rootId ?? preferredMain;
        if (ridMain > 0 && allowIds != null && !allowIds.Contains(ridMain))
            ridMain = preferredMain > 0 ? preferredMain : displayRoots.FirstOrDefault().Id;
        ViewBag.RootId = ridMain;
        ViewBag.Forest = null;
        FtTreeNodeVm? single = ridMain > 0
            ? await _tree.BuildDownAsync(ridMain, ct, withPeerStubs: mainMode, allowIds: allowIds, mainGenealogyOnly: mainMode)
            : null;
        if (single == null && mainMode && displayRoots.Count > 0)
        {
            foreach (var r in displayRoots)
            {
                single = await _tree.BuildDownAsync(r.Id, ct, withPeerStubs: true, allowIds: allowIds, mainGenealogyOnly: true);
                if (single != null)
                {
                    ViewBag.RootId = r.Id;
                    break;
                }
            }
        }
        return View(single);
    }

    private static void MarkMainRefNodes(FtTreeNodeVm node, HashSet<int> mineIds)
    {
        if (!mineIds.Contains(node.Id))
            node.IsMainRef = true;
        foreach (var c in node.Children)
            MarkMainRefNodes(c, mineIds);
    }

    [HttpGet]
    public async Task<IActionResult> GetTreeJson(int rootId, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var (_, allowIds, mainMode) = await _tree.ResolveScopeAsync(uid, _persons, ct);
        if (allowIds != null && !allowIds.Contains(rootId))
            return Json(null);
        var node = await _tree.BuildDownAsync(rootId, ct, withPeerStubs: mainMode, allowIds: allowIds, mainGenealogyOnly: mainMode);
        return Json(node);
    }

    [HttpGet]
    public async Task<IActionResult> ExpandPeer(int bridgeId, int personId, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        try
        {
            var kids = await _peers.ExpandRemoteAsync(bridgeId, personId, ct);
            return Json(new
            {
                ok = true,
                data = kids.Select(k => new
                {
                    nodeKey = k.NodeKey,
                    name = k.Name,
                    birth = k.Birth,
                    lazyExpand = k.LazyExpand,
                    bridgeId = k.BridgeId,
                    remotePersonId = k.RemotePersonId
                })
            });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { ok = false, message = ex.Message });
        }
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}

[Authorize]
public class FtMainTreeController : Controller
{
    private readonly FtPersonService _persons;
    public FtMainTreeController(FtPersonService persons) => _persons = persons;

    [HttpGet, HttpPost]
    public async Task<IActionResult> Index(string? searchContent, int? intPage, int? pageShowNum, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var (rows, total, pages, p) = await _persons.GetIndexPageAsync(uid, "1", "FullName", searchContent ?? "", "FullName", "0",
            intPage.GetValueOrDefault(1), pageShowNum.GetValueOrDefault(16), ct);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false);
        ViewBag.SearchContent = searchContent ?? "";
        ViewBag.IntPage = p;
        ViewBag.PageShowNum = pageShowNum.GetValueOrDefault(16);
        ViewBag.TotalPages = pages;
        ViewBag.TotalRecords = total;
        ViewBag.SearchField = "FullName";
        ViewBag.SelectField = "FullName";
        ViewBag.SelectFieldArrow = "0";
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Include(int id, bool withDesc, CancellationToken ct)
    {
        try
        {
            await _persons.SetMainAsync(id, true, withDesc, FtClaims.UserId(User) ?? 0, FtClaims.Operator(User), ct);
            TempData["SuccessMessage"] = "已纳入。";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Exclude(int id, CancellationToken ct)
    {
        try
        {
            await _persons.SetMainAsync(id, false, true, FtClaims.UserId(User) ?? 0, FtClaims.Operator(User), ct);
            TempData["SuccessMessage"] = "已移出。";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}

[Authorize]
public class FtMyProfileController : Controller
{
    private readonly FtMyProfileService _service;
    private readonly FtPhotoService _photos;
    public FtMyProfileController(FtMyProfileService service, FtPhotoService photos)
    {
        _service = service;
        _photos = photos;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var p = await _service.BoundPersonAsync(uid, ct);
        if (p == null)
        {
            ViewBag.NeedBind = true;
            return View(new FtProfileFormVm());
        }
        return View(new FtProfileFormVm
        {
            PersonId = p.DataId,
            FullName = p.FullName,
            NickName = p.NickName,
            SelfIntro = p.SelfIntro,
            WechatId = p.WechatId,
            PrivacyLevel = p.PrivacyLevel,
            ShowPhoto = p.ShowPhoto,
            ShowWechat = p.ShowWechat,
            ShowSelfIntro = p.ShowSelfIntro,
            ShowBirthDetail = p.ShowBirthDetail,
            ShowResume = p.ShowResume,
            PrintAllow = p.PrintAllow,
            Photos = await _photos.ListVisibleAsync(p, uid, ct)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(FtProfileFormVm model, CancellationToken ct)
    {
        try
        {
            await _service.SaveAsync(model, FtClaims.UserId(User) ?? 0, ct);
            TempData["SuccessMessage"] = "已保存。";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto(IFormFile? file, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        var p = await _service.BoundPersonAsync(uid, ct);
        if (p == null)
        {
            TempData["ErrorMessage"] = "尚未绑定本人档案。";
            return RedirectToAction(nameof(Index));
        }
        try
        {
            if (file == null) throw new InvalidOperationException("请选择照片。");
            await _photos.UploadAsync(p.DataId, uid, file, ct);
            TempData["SuccessMessage"] = "照片已上传。";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(string? path, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User) ?? 0;
        var p = await _service.BoundPersonAsync(uid, ct);
        if (p == null) return RedirectToAction(nameof(Index));
        try
        {
            await _photos.DeleteAsync(p.DataId, uid, path, ct);
            TempData["SuccessMessage"] = "已删除照片。";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}

[Authorize]
public class FtOpLogController : Controller
{
    private readonly FtOpLogService _service;
    public FtOpLogController(FtOpLogService service) => _service = service;

    [HttpGet, HttpPost]
    public async Task<IActionResult> Index(string? searchField, string? searchContent, string? selectField, string? selectFieldArrow,
        int? intPage, int? pageShowNum, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var (rows, total, pages, p) = await _service.GetIndexPageAsync(searchField ?? "OpType", searchContent ?? "",
            selectField ?? "CreateDate", selectFieldArrow ?? "1", intPage.GetValueOrDefault(1), pageShowNum.GetValueOrDefault(16), ct);
        ViewBag.SearchField = searchField ?? "OpType";
        ViewBag.SearchContent = searchContent ?? "";
        ViewBag.SelectField = selectField ?? "CreateDate";
        ViewBag.SelectFieldArrow = selectFieldArrow ?? "1";
        ViewBag.IntPage = p;
        ViewBag.PageShowNum = pageShowNum.GetValueOrDefault(16);
        ViewBag.TotalPages = pages;
        ViewBag.TotalRecords = total;
        return View(rows);
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}

[Authorize]
public class FtBatchMatchController : Controller
{
    private readonly FtBatchMatchService _service;
    public FtBatchMatchController(FtBatchMatchService service) => _service = service;

    [HttpGet]
    public IActionResult Index()
    {
        if (!CanView()) return Forbid();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(CancellationToken ct)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)
            && !FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false))
            return Forbid();
        var n = await _service.RunAsync(ct);
        TempData["SuccessMessage"] = $"批量匹配完成，共产生 {n} 条提示（不含自动链入）。";
        return RedirectToAction(nameof(Index));
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}

[Authorize]
public class FtBranchAdminController : Controller
{
    private readonly FtBranchAdminService _service;
    private readonly FtBranchAdminApplyService _applies;
    private readonly FtDutyAccess _duty;
    public FtBranchAdminController(FtBranchAdminService service, FtBranchAdminApplyService applies, FtDutyAccess duty)
    {
        _service = service;
        _applies = applies;
        _duty = duty;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (!CanView()) return Forbid();
        TempData["ErrorMessage"] = "支链管理员由各族谱管理员在「我的家族」审批与任免；超管只委任族谱管理员，不管理支链与业务。";
        return RedirectToAction("Index", "FtClanAdmin");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Set(int userId, bool grant, CancellationToken ct)
    {
        TempData["ErrorMessage"] = "超管不管理支链管理员。";
        return RedirectToAction("Index", "FtClanAdmin");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ApproveApply(int id, bool pass, string? remark, CancellationToken ct)
    {
        TempData["ErrorMessage"] = "请族谱管理员在「我的家族」审批。";
        return RedirectToAction("Index", "FtClanAdmin");
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}

[Authorize]
public class FtExportController : Controller
{
    private readonly FtTreeService _tree;
    private readonly FtPersonService _persons;
    private readonly FtOfflineExportService _offline;
    private readonly FtDutyAccess _duty;

    public FtExportController(
        FtTreeService tree, FtPersonService persons, FtOfflineExportService offline, FtDutyAccess duty)
    {
        _tree = tree;
        _persons = persons;
        _offline = offline;
        _duty = duty;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? rootId, CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User) ?? 0;
        var (roots, allowIds, mainMode) = await _tree.ResolveScopeAsync(uid, _persons, ct);
        var preferred = await _tree.PickDefaultRootAsync(uid, roots, allowIds, ct);
        roots = await _tree.PreferPersonalRootInListAsync(uid, roots, allowIds, preferred, ct);
        ViewBag.MainMode = mainMode;
        ViewBag.CanDownloadZip = await _duty.IsBranchAdminAsync(uid, ct);
        var rid = rootId ?? preferred;
        if (rid > 0 && allowIds != null && !allowIds.Contains(rid))
            rid = preferred > 0 ? preferred : roots.FirstOrDefault().Id;
        ViewBag.Roots = roots;
        ViewBag.RootId = rid;
        return View(rid > 0
            ? await _tree.BuildDownAsync(rid, ct, withPeerStubs: mainMode, allowIds: allowIds, mainGenealogyOnly: mainMode)
            : null);
    }

    /// <summary>导出离线族谱包（genealogy.xml + index.html → zip）。</summary>
    [HttpGet]
    public async Task<IActionResult> DownloadZip(CancellationToken ct)
    {
        if (!CanView()) return Forbid();
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();

        if (!await _duty.IsBranchAdminAsync(uid.Value, ct))
        {
            TempData["ErrorMessage"] = "仅族谱管理员与支链管理员可下载离线族谱包。";
            return RedirectToAction(nameof(Index));
        }

        var result = await _offline.BuildZipAsync(uid.Value, ct);
        if (result == null)
        {
            TempData["ErrorMessage"] = "暂无可导出的族谱数据，请先录入并纳入主谱。";
            return RedirectToAction(nameof(Index));
        }

        return File(result.Value.Bytes, "application/zip", result.Value.FileName);
    }

    private bool CanView()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return !string.IsNullOrEmpty(lim) && FunctionLimitUi.CanView(lim, false);
    }
}
