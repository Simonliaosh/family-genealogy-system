using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>应用模块只读查询。</summary>
public sealed class EAppModuleQueryService
{
    private readonly FrameworkDbContext _db;
    private readonly DictService _dict;

    public EAppModuleQueryService(FrameworkDbContext db, DictService dict)
    {
        _db = db;
        _dict = dict;
    }

    public Task<List<(string Value, string Text)>> GetAppTypeFilterOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.AppType, forFilter: true, formEmptyLabel: null,
            EAppModuleService.AppTypeFallback.Select(x => (x.Key, x.Value)).ToList(), ct: ct);

    public static List<(string Value, string Text)> BuildBStatusFilterOptions() =>
    [
        ("", "全部"),
        ("1", "启用"),
        ("2", "停用")
    ];

    public static string ToStatusDisplay(string? value)
    {
        var v = (value ?? "").Trim();
        return v switch
        {
            "1" or "启用" => "启用",
            "2" or "停用" => "停用",
            _ => string.IsNullOrEmpty(v) ? "-" : v
        };
    }

    public async Task<(IReadOnlyList<EAppModuleListRowVm> pageRows, int total, int totalPages, int page)> GetIndexPageAsync(
        string? appType1,
        string? bStatus1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EAppModules.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(appType1))
            rows = rows.Where(x => string.Equals(x.AppType, appType1.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(bStatus1))
        {
            rows = bStatus1 switch
            {
                "1" => rows.Where(x => EBStatusHelper.IsActiveBStatus(x.BStatus)).ToList(),
                "2" => rows.Where(x => !EBStatusHelper.IsActiveBStatus(x.BStatus)).ToList(),
                _ => rows
            };
        }

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            rows = (searchField ?? "AppCode") switch
            {
                "AppName" => rows.Where(x => (x.AppName ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows.Where(x => (x.AppCode ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList()
            };
        }

        rows = SortRows(rows, sortField, sortArrow);
        var appTypeMap = await _dict.GetItemMapMergedAsync(DictCodes.AppType, EAppModuleService.AppTypeFallback, ct: ct);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EAppModuleListRowVm
        {
            DataId = x.DataId,
            AppCode = x.AppCode,
            AppName = x.AppName,
            AppType = x.AppType,
            AppTypeText = EAppModuleService.ToAppTypeText(x.AppType, appTypeMap),
            DispSeq = x.DispSeq,
            BStatusText = ToStatusDisplay(x.BStatus)
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    private static List<EAppModule> SortRows(List<EAppModule> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        return field switch
        {
            "AppCode" => desc ? rows.OrderByDescending(x => x.AppCode).ToList() : rows.OrderBy(x => x.AppCode).ToList(),
            "AppName" => desc ? rows.OrderByDescending(x => x.AppName).ToList() : rows.OrderBy(x => x.AppName).ToList(),
            "AppType" => desc ? rows.OrderByDescending(x => x.AppType).ToList() : rows.OrderBy(x => x.AppType).ToList(),
            "DispSeq" => desc ? rows.OrderByDescending(x => x.DispSeq).ToList() : rows.OrderBy(x => x.DispSeq).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            _ => rows.OrderBy(x => x.DispSeq).ThenBy(x => x.AppCode).ToList()
        };
    }

    public async Task<EAppModuleDetailVm?> GetDetailAsync(int id, CancellationToken ct)
    {
        var x = await _db.EAppModules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.DataId == id && !m.IsDeleted, ct);
        if (x == null) return null;

        var appTypeMap = await _dict.GetItemMapMergedAsync(DictCodes.AppType, EAppModuleService.AppTypeFallback, ct: ct);

        return new EAppModuleDetailVm
        {
            DataId = x.DataId,
            AppCode = x.AppCode,
            AppName = x.AppName,
            AppType = x.AppType,
            AppTypeText = EAppModuleService.ToAppTypeText(x.AppType, appTypeMap),
            BaseUrl = x.BaseUrl,
            Icon = x.Icon,
            DispSeq = x.DispSeq,
            BStatusText = ToStatusDisplay(x.BStatus),
            Remark = x.Remark,
            CreateDate = x.CreateDate,
            AmendDate = x.AmendDate,
            OperatorName = x.OperatorName ?? ""
        };
    }
}
