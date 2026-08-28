using System.Data.Common;
using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Controllers;

[Authorize]
public class EUsersController : Controller
{
    private readonly FrameworkDbContext _context;
    private readonly EUsersService _service;
    private readonly DictService _dict;

    private static readonly HashSet<string> SearchWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "LoginId", "RealName", "UserType"
    };

    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "LoginId", "RealName", "UserType", "LoginCount", "PwdErrorCount", "LastLoginTime", "BStatus"
    };

    public EUsersController(FrameworkDbContext context, EUsersService service, DictService dict)
    {
        _context = context;
        _service = service;
        _dict = dict;
    }

    private string MemberId => User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? "";

    [AcceptVerbs("GET", "POST")]
    public async Task<IActionResult> Index(
        string? userType1, string? isEnabled1, string? isLocked1, string? bStatus1,
        string? searchField, string? searchContent,
        string? selectField, string? selectFieldArrow,
        int intPage = 1, int pageShowNum = 16)
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        if (string.IsNullOrEmpty(lim)) return Forbid();

        if (Request.HasFormContentType)
        {
            userType1 = string.IsNullOrWhiteSpace(userType1) ? Request.Form["userType1"].ToString() : userType1;
            isEnabled1 = string.IsNullOrWhiteSpace(isEnabled1) ? Request.Form["isEnabled1"].ToString() : isEnabled1;
            isLocked1 = string.IsNullOrWhiteSpace(isLocked1) ? Request.Form["isLocked1"].ToString() : isLocked1;
            bStatus1 = string.IsNullOrWhiteSpace(bStatus1) ? Request.Form["bStatus1"].ToString() : bStatus1;
            searchField = string.IsNullOrWhiteSpace(searchField) ? Request.Form["searchField"].ToString() : searchField;
            searchContent = string.IsNullOrWhiteSpace(searchContent) ? Request.Form["searchContent"].ToString() : searchContent;
            selectField = string.IsNullOrWhiteSpace(selectField) ? Request.Form["selectField"].ToString() : selectField;
            selectFieldArrow = string.IsNullOrWhiteSpace(selectFieldArrow) ? Request.Form["selectFieldArrow"].ToString() : selectFieldArrow;
            if (!int.TryParse(Request.Form["intPage"], out intPage)) intPage = 1;
            if (!int.TryParse(Request.Form["pageShowNum"], out pageShowNum)) pageShowNum = 16;
        }

        userType1 = string.IsNullOrWhiteSpace(userType1) ? "全部" : userType1.Trim();
        isEnabled1 = string.IsNullOrWhiteSpace(isEnabled1) ? "全部" : isEnabled1.Trim();
        isLocked1 = string.IsNullOrWhiteSpace(isLocked1) ? "全部" : isLocked1.Trim();
        bStatus1 = string.IsNullOrWhiteSpace(bStatus1) ? "全部" : bStatus1.Trim();
        searchField = string.IsNullOrWhiteSpace(searchField) ? "LoginId" : searchField.Trim();
        selectField = string.IsNullOrWhiteSpace(selectField) ? "-" : selectField.Trim();
        selectFieldArrow = string.IsNullOrWhiteSpace(selectFieldArrow) ? "0" : selectFieldArrow.Trim();

        var rows = await _context.EUsers.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync();
        rows = rows.Where(x =>
                MatchFilter(x.UserType, userType1)
                && MatchBitFilter(x.IsEnabled, isEnabled1)
                && MatchBitFilter(x.IsLocked, isLocked1)
                && MatchFilter(x.BStatus, bStatus1))
            .ToList();
        rows = ApplySearch(rows, searchField, searchContent).ToList();
        rows = SortRows(rows, selectField, selectFieldArrow).ToList();

        if (pageShowNum <= 0) pageShowNum = 16;
        if (pageShowNum > 200) pageShowNum = 200;
        if (intPage <= 0) intPage = 1;
        var totalRecords = rows.Count;
        var totalPages = totalRecords == 0 ? 1 : (int)Math.Ceiling(totalRecords / (double)pageShowNum);
        if (intPage > totalPages) intPage = totalPages;
        var pageRows = rows.Skip((intPage - 1) * pageShowNum).Take(pageShowNum).ToList();

        ViewBag.UserType1 = userType1;
        ViewBag.IsEnabled1 = isEnabled1;
        ViewBag.IsLocked1 = isLocked1;
        ViewBag.BStatus1 = bStatus1;
        ViewBag.SearchField = searchField;
        ViewBag.SearchContent = searchContent;
        ViewBag.SelectField = selectField;
        ViewBag.SelectFieldArrow = selectFieldArrow;
        ViewBag.IntPage = intPage;
        ViewBag.PageShowNum = pageShowNum;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = totalRecords;
        ViewBag.CanCreate = FunctionLimitUi.CanCreate(lim, false);
        ViewBag.CanUpdate = FunctionLimitUi.CanUpdate(lim, false);
        ViewBag.CanDelete = FunctionLimitUi.CanDelete(lim, false);
        var userTypeMap = await GetUserTypeMapAsync();
        ViewBag.UserTypeMap = userTypeMap;
        ViewBag.UserTypeOptions = LoadOptions(await GetUserTypeFormOptionsAsync(), userType1);
        ViewBag.IsEnabledOptions = EUserCodeMaps.WithAll(EUserCodeMaps.IsEnabledOptions);
        ViewBag.IsLockedOptions = EUserCodeMaps.WithAll(EUserCodeMaps.IsLockedOptions);
        ViewBag.BStatusOptions = EUserCodeMaps.WithAll(EUserCodeMaps.BStatusOptions);
        return View(pageRows);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        await PopulateFormOptionsAsync();
        return View(new EUsersFormVm
        {
            LoginCount = 0,
            MaxLoginCount = 99999,
            PwdErrorCount = 0,
            MaxPwdErrorCount = 5,
            IsLocked = "0",
            IsEnabled = "0",
            BStatus = "0",
            UserType = "EMPLOYEE"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EUsersFormVm model, string? selectNo)
    {
        if (!FunctionLimitUi.CanCreate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();

        model.LoginId = _service.Normalize(model.LoginId, 30);
        model.RealName = _service.Normalize(model.RealName, 20);
        model.UserType = _service.Normalize(model.UserType, 12);
        model.BStatus = _service.NormalizeCode(model.BStatus);
        model.IsEnabled = _service.NormalizeCode(model.IsEnabled);
        model.IsLocked = _service.NormalizeCode(model.IsLocked);
        model.ShareHolderCode = _service.Normalize(model.ShareHolderCode, 30);
        model.CustomerId = NormalizeId(model.CustomerId);
        model.PartnerId = NormalizeId(model.PartnerId);
        model.SupplierId = NormalizeId(model.SupplierId);

        if (string.IsNullOrWhiteSpace(model.InitialPassword))
            ModelState.AddModelError(nameof(model.InitialPassword), "初始密码不能为空");
        if (model.MaxLoginCount < 1)
            ModelState.AddModelError(nameof(model.MaxLoginCount), "最大登录次数必须大于等于1");
        if (model.MaxPwdErrorCount < 1)
            ModelState.AddModelError(nameof(model.MaxPwdErrorCount), "最大密码错误次数必须大于等于1");

        if (!ModelState.IsValid)
        {
            await PopulateFormOptionsAsync();
            return View(model);
        }

        var exists = await _context.EUsers.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.LoginId == model.LoginId);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.LoginId), "账号已存在，请更换账号");
            await PopulateFormOptionsAsync();
            return View(model);
        }

        var entity = _service.BuildCreateEntity(model, MemberId);
        _context.EUsers.Add(entity);
        await _context.SaveChangesAsync();

        if (string.Equals(selectNo, "1", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Create));
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? polistRt = null)
    {
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var row = await _context.EUsers.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted);
        if (row == null) return NotFound();
        ViewBag.PolistRt = polistRt;
        await PopulateFormOptionsAsync();
        var vm = _service.BuildFormVm(row);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EUsersFormVm model, string? selectNo, string? polistRt)
    {
        ViewBag.PolistRt = polistRt;
        if (!FunctionLimitUi.CanUpdate(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (id != model.DataId) return NotFound();

        model.RealName = _service.Normalize(model.RealName, 20);
        model.UserType = _service.Normalize(model.UserType, 12);
        model.BStatus = _service.NormalizeCode(model.BStatus);
        model.IsEnabled = _service.NormalizeCode(model.IsEnabled);
        model.IsLocked = _service.NormalizeCode(model.IsLocked);
        model.LoginId = _service.Normalize(model.LoginId, 30);
        model.ShareHolderCode = _service.Normalize(model.ShareHolderCode, 30);
        model.CustomerId = NormalizeId(model.CustomerId);
        model.PartnerId = NormalizeId(model.PartnerId);
        model.SupplierId = NormalizeId(model.SupplierId);

        if (model.MaxLoginCount < 1)
            ModelState.AddModelError(nameof(model.MaxLoginCount), "最大登录次数必须大于等于1");
        if (model.MaxPwdErrorCount < 1)
            ModelState.AddModelError(nameof(model.MaxPwdErrorCount), "最大密码错误次数必须大于等于1");
        if (!string.IsNullOrWhiteSpace(model.ResetPassword) &&
            !string.Equals(model.ResetPassword, model.ConfirmResetPassword, StringComparison.Ordinal))
            ModelState.AddModelError(nameof(model.ConfirmResetPassword), "两次输入的重置密码不一致");

        if (!ModelState.IsValid)
        {
            await PopulateFormOptionsAsync();
            return View(model);
        }

        var oldRow = await _context.EUsers.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted);
        if (oldRow == null) return NotFound();

        var (setList, ps) = _service.BuildUpdateSet(oldRow, model);
        if (setList.Count > 0)
        {
            await _context.Database.OpenConnectionAsync();
            try
            {
                await using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = $"UPDATE Tbl_E_Users SET {string.Join(",", setList)} WHERE DataID=@id";
                Add(cmd, "@id", id);
                foreach (var p in ps) Add(cmd, p.Key, p.Value);
                await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        if (string.Equals(selectNo, "1", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Edit), new { id, polistRt });
        return PolistReturnToken.RedirectToIndex(this, polistRt);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        var row = await _context.EUsers.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted);
        if (row == null) return RedirectToAction(nameof(Index));
        row.IsDeleted = true;
        row.AmendDate = DateTime.Now;
        row.OperatorName = _service.Normalize(MemberId, 30, "system");
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBatch([FromForm] int[]? id)
    {
        if (!FunctionLimitUi.CanDelete(HttpContext.Items["PubFunctionLimit"] as string, false)) return Forbid();
        if (id == null || id.Length == 0) return RedirectToAction(nameof(Index));
        var ids = id.Distinct().ToArray();
        var rows = await _context.EUsers.Where(x => ids.Contains(x.DataId) && !x.IsDeleted).ToListAsync();
        if (rows.Count > 0)
        {
            var now = DateTime.Now;
            var op = _service.Normalize(MemberId, 30, "system");
            foreach (var row in rows)
            {
                row.IsDeleted = true;
                row.AmendDate = now;
                row.OperatorName = op;
            }
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private static List<(string Value, string Text)> LoadOptions(
        IReadOnlyList<(string Value, string Text)> source,
        string selected)
    {
        var list = EUserCodeMaps.WithAll(source);
        if (!string.IsNullOrWhiteSpace(selected) &&
            !list.Any(x => string.Equals(x.Value, selected, StringComparison.OrdinalIgnoreCase)))
            list.Add((selected, selected));
        return list;
    }

    private static bool MatchFilter(string? value, string? selected)
        => string.IsNullOrWhiteSpace(selected)
           || string.Equals(selected, "全部", StringComparison.OrdinalIgnoreCase)
           || string.Equals((value ?? "").Trim(), selected.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool MatchBitFilter(bool value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected) || string.Equals(selected, "全部", StringComparison.OrdinalIgnoreCase))
            return true;
        return selected switch
        {
            "1" => value,
            "0" => !value,
            _ => true
        };
    }

    private static IEnumerable<EUser> ApplySearch(IEnumerable<EUser> rows, string? field, string? content)
    {
        if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(content) || !SearchWhitelist.Contains(field))
            return rows;
        var key = content.Trim();
        return field switch
        {
            "LoginId" => rows.Where(x => (x.LoginId ?? "").Contains(key, StringComparison.OrdinalIgnoreCase)),
            "RealName" => rows.Where(x => (x.RealName ?? "").Contains(key, StringComparison.OrdinalIgnoreCase)),
            "UserType" => rows.Where(x => (x.UserType ?? "").Contains(key, StringComparison.OrdinalIgnoreCase)),
            _ => rows
        };
    }

    private static IEnumerable<EUser> SortRows(IEnumerable<EUser> rows, string? field, string? arrow)
    {
        var desc = string.Equals(arrow, "1", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(field) || field == "-" || !SortWhitelist.Contains(field))
            return rows.OrderByDescending(x => x.DataId);
        return field switch
        {
            "LoginId" => desc ? rows.OrderByDescending(x => x.LoginId) : rows.OrderBy(x => x.LoginId),
            "RealName" => desc ? rows.OrderByDescending(x => x.RealName) : rows.OrderBy(x => x.RealName),
            "UserType" => desc ? rows.OrderByDescending(x => x.UserType) : rows.OrderBy(x => x.UserType),
            "LoginCount" => desc ? rows.OrderByDescending(x => x.LoginCount) : rows.OrderBy(x => x.LoginCount),
            "PwdErrorCount" => desc ? rows.OrderByDescending(x => x.PwdErrorCount) : rows.OrderBy(x => x.PwdErrorCount),
            "LastLoginTime" => desc ? rows.OrderByDescending(x => x.LastLoginTime) : rows.OrderBy(x => x.LastLoginTime),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus) : rows.OrderBy(x => x.BStatus),
            _ => rows.OrderByDescending(x => x.DataId)
        };
    }

    private static void Add(DbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    private int? NormalizeId(int? id) => id.GetValueOrDefault() <= 0 ? null : id;

    private async Task PopulateFormOptionsAsync()
    {
        ViewBag.UserTypeFormOptions = await GetUserTypeFormOptionsAsync();
        // 独立框架不包含客户/供应商/合作伙伴/股东主数据表；下拉为空，可在集成业务系统时恢复数据源。
        ViewBag.CustomerOptions = new List<(int DataId, string Text)>();
        ViewBag.SupplierOptions = new List<(int DataId, string Text)>();
        ViewBag.PartnerOptions = new List<(int DataId, string Text)>();
        ViewBag.ShareHolderOptions = new List<(string Code, string Text)>();
    }

    private async Task<List<(string Value, string Text)>> GetUserTypeFormOptionsAsync()
    {
        var opts = await _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.UserType, forFilter: false, formEmptyLabel: null,
            EUserCodeMaps.UserTypeOptions.ToList());
        return opts.Where(x => x.Value.Length > 0).ToList();
    }

    private Task<Dictionary<string, string>> GetUserTypeMapAsync() =>
        _dict.GetItemMapMergedAsync(DictCodes.UserType, EUserCodeMaps.BuiltinUserTypeLabels);
}

