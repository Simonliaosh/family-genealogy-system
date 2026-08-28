using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FamilyTree.Configuration;

namespace FamilyTree.Controllers;

/// <summary>问卷星式录入向导：同页逐步填写（入族→本人→认证→配偶→父母→子女）。</summary>
[Authorize]
public class MemberGuideController : Controller
{
    public static readonly string[] Steps =
        ["clan", "self", "cert", "spouse", "father", "mother", "grandfather", "grandmother", "child", "done"];

    private readonly FtClanService _clans;
    private readonly FtPersonService _persons;
    private readonly FtPersonDraftService _drafts;
    private readonly FtPersonMarryService _marry;
    private readonly FtDutyAccess _duty;
    private readonly FtApiAuthService _auth;
    private readonly FamilyTreeOptions _opt;

    public MemberGuideController(
        FtClanService clans,
        FtPersonService persons,
        FtPersonDraftService drafts,
        FtPersonMarryService marry,
        FtDutyAccess duty,
        FtApiAuthService auth,
        IOptions<FamilyTreeOptions> opt)
    {
        _clans = clans;
        _persons = persons;
        _drafts = drafts;
        _marry = marry;
        _duty = duty;
        _auth = auth;
        _opt = opt.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? step, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var name = User.FindFirst("EmployeeName")?.Value;
        var self = await _persons.EnsureSelfPersonAsync(uid.Value, name, null, ct);
        var clan = await _clans.GetUserClanAsync(uid.Value, ct);
        var resolved = ResolveStep(self, clan);
        var requested = string.IsNullOrWhiteSpace(step) ? resolved : step.Trim().ToLowerInvariant();
        var cur = ClampStep(requested, resolved);
        ViewBag.Step = cur;
        ViewBag.StepIndex = IndexOf(cur);
        ViewBag.StepCount = Steps.Length;
        ViewBag.Clan = clan;
        ViewBag.Self = self;
        ViewBag.CertUrl = CertifyAbsoluteUrl(self.CertCode);
        ViewBag.IsBranchAdmin = await _duty.IsBranchAdminAsync(uid.Value, ct);
        ViewBag.Suggested = resolved;
        return View();
    }

    private static string ClampStep(string requested, string resolved)
    {
        // 未入族 / 未填本人：不能跳过
        if (resolved == "clan") return "clan";
        if (resolved == "self" && IndexOf(requested) > IndexOf("self")) return "self";
        if (!Steps.Contains(requested)) return resolved;
        return requested;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSelf(string? fullName, string? birthDate, byte gender,
        string? fatherName, string? motherName, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var op = FtClaims.Operator(User);
        try
        {
            var self = await _persons.GetSelfPersonAsync(uid.Value, ct)
                       ?? await _persons.EnsureSelfPersonAsync(uid.Value, fullName, null, ct);
            var form = _persons.ToForm(self);
            form.FullName = fullName ?? "";
            form.BirthDate = birthDate;
            form.Gender = gender;
            form.FatherName = fatherName ?? "";
            form.MotherName = motherName;
            _persons.NormalizeFormForSave(form);
            if (string.IsNullOrWhiteSpace(form.FullName))
            {
                TempData["ErrorMessage"] = "请填写您的姓名。";
                return RedirectToAction(nameof(Index), new { step = "self" });
            }
            await _persons.TryUpdateAsync(self.DataId, form, uid.Value, op, ct);
            TempData["SuccessMessage"] = "本人信息已保存。";
            return RedirectToAction(nameof(Index), new { step = "cert" });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index), new { step = "self" });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AfterCert() => RedirectToAction(nameof(Index), new { step = "spouse" });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSpouse(string? spouseName, string? spouseBirth, string? marryType, string? next, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        var self = await _persons.GetSelfPersonAsync(uid.Value, ct);
        if (self == null)
            return RedirectToAction(nameof(Index), new { step = "self" });

        if (string.Equals(next, "skip", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(spouseName))
            return RedirectToAction(nameof(Index), new { step = "father" });

        try
        {
            var m = new FtMarryFormVm
            {
                PersonId = self.DataId,
                SpouseName = spouseName,
                SpouseBirth = spouseBirth,
                MarryType = string.IsNullOrWhiteSpace(marryType) ? "原配" : marryType,
                HouseSeq = 1
            };
            await _marry.SaveAsync(m, uid.Value, FtClaims.Operator(User), ct);
            TempData["SuccessMessage"] = "配偶信息已保存。";
            return RedirectToAction(nameof(Index), new { step = "father" });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index), new { step = "spouse" });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRelative(string relation, string? fullName, string? birthDate,
        byte gender, string? fatherName, string? motherName, string? next, CancellationToken ct)
    {
        var uid = FtClaims.UserId(User);
        if (uid == null) return Challenge();
        relation = (relation ?? "").Trim().ToUpperInvariant();
        if (relation is not ("FATHER" or "MOTHER" or "CHILD" or "GRANDFATHER" or "GRANDMOTHER"))
            return RedirectToAction(nameof(Index));

        var stepKey = relation switch
        {
            "FATHER" => "father",
            "MOTHER" => "mother",
            "GRANDFATHER" => "grandfather",
            "GRANDMOTHER" => "grandmother",
            _ => "child"
        };

        if (relation is "GRANDFATHER" or "GRANDMOTHER")
        {
            var selfCheck = await _persons.GetSelfPersonAsync(uid!.Value, ct);
            if (selfCheck?.FatherPersonId is not > 0)
            {
                TempData["ErrorMessage"] = "请先填写并保存父亲信息，再录入祖父/祖母。";
                return RedirectToAction(nameof(Index), new { step = "father" });
            }
        }

        if (string.Equals(next, "skip", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(fullName))
        {
            var skipTo = relation switch
            {
                "FATHER" => "mother",
                "MOTHER" => "grandfather",
                "GRANDFATHER" => "grandmother",
                "GRANDMOTHER" => "child",
                _ => "done"
            };
            return RedirectToAction(nameof(Index), new { step = skipTo });
        }

        try
        {
            var self = await _persons.GetSelfPersonAsync(uid.Value, ct);
            var form = new FtDraftFormVm
            {
                RelationType = relation,
                FullName = fullName ?? "",
                BirthDate = birthDate,
                Gender = gender,
                FatherName = fatherName ?? self?.FatherName ?? "",
                MotherName = motherName ?? self?.MotherName
            };
            if (relation == "CHILD")
            {
                // 子女：父母填本人姓名（按性别）
                if (self != null)
                {
                    if (self.Gender == 0) form.MotherName = self.FullName;
                    else form.FatherName = self.FullName;
                }
            }
            _drafts.Normalize(form);
            var draftId = await _drafts.SaveDraftAsync(form, uid.Value, FtClaims.Operator(User), ct);
            await _drafts.ArchiveAsNewAsync(draftId, uid.Value, FtClaims.Operator(User), ct);
            TempData["SuccessMessage"] = relation switch
            {
                "FATHER" => "父亲信息已保存。",
                "MOTHER" => "母亲信息已保存。",
                "GRANDFATHER" => "祖父信息已保存。",
                "GRANDMOTHER" => "祖母信息已保存。",
                _ => "子女信息已保存。"
            };

            if (relation == "CHILD" && string.Equals(next, "more", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(Index), new { step = "child" });

            var go = relation switch
            {
                "FATHER" => "mother",
                "MOTHER" => "grandfather",
                "GRANDFATHER" => "grandmother",
                "GRANDMOTHER" => "child",
                _ => "done"
            };
            return RedirectToAction(nameof(Index), new { step = go });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index), new { step = stepKey });
        }
    }

    private static string ResolveStep(FtPerson self, FtClan? clan)
    {
        if (clan == null) return "clan";
        var name = FtText.NormName(self.FullName);
        if (name.Length == 0 || name == "未命名" || string.IsNullOrWhiteSpace(self.FatherName))
            return "self";
        if (!self.IsCertified) return "cert";
        return "spouse";
    }

    private static int IndexOf(string step)
    {
        var i = Array.IndexOf(Steps, step);
        return i < 0 ? 0 : i;
    }

    private string CertifyAbsoluteUrl(string? code)
    {
        var path = Url.Action("Open", "FtCertify", new { c = code }) ?? $"/FtCertify/Open?c={Uri.EscapeDataString(code ?? "")}";
        return AbsUrl(path);
    }

    private string AbsUrl(string pathOrUrl)
    {
        if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return pathOrUrl;
        var root = _auth.ResolvePublicBaseUrl();
        if (root.Length == 0)
            root = (_opt.PublicBaseUrl ?? "").Trim().TrimEnd('/');
        if (root.Length == 0)
            root = $"{Request.Scheme}://{Request.Host.Value}".TrimEnd('/');
        if (!pathOrUrl.StartsWith('/')) pathOrUrl = "/" + pathOrUrl;
        return root + pathOrUrl;
    }
}
