using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>字典只读查询（类型 + 条目）。</summary>
public sealed class EDictQueryService
{
    private readonly FrameworkDbContext _db;

    public EDictQueryService(FrameworkDbContext db)
    {
        _db = db;
    }

    public static List<(string Value, string Text)> BuildAppCodeFilterOptions() =>
    [
        ("", "全部"),
        ("FRAME", "FRAME"),
        ("CRM", "CRM"),
        ("OA", "OA")
    ];

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

    public async Task<(IReadOnlyList<EDictTypeListRowVm> pageRows, int total, int totalPages, int page)> GetTypeIndexPageAsync(
        string? appCode1,
        string? bStatus1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EDictTypes.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(appCode1))
            rows = rows.Where(x => string.Equals(x.AppCode, appCode1.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

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
            rows = (searchField ?? "DictTypeCode") switch
            {
                "DictTypeName" => rows.Where(x => (x.DictTypeName ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows.Where(x => (x.DictTypeCode ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList()
            };
        }

        var itemCounts = await _db.EDictItems.AsNoTracking()
            .Where(i => !i.IsDeleted)
            .GroupBy(i => i.DictTypeCode)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var countMap = itemCounts.ToDictionary(x => x.Key, x => x.Count, StringComparer.OrdinalIgnoreCase);

        rows = SortRows(rows, sortField, sortArrow, countMap);
        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EDictTypeListRowVm
        {
            DictTypeCode = x.DictTypeCode,
            DictTypeName = x.DictTypeName,
            AppCode = x.AppCode,
            BStatusText = ToStatusDisplay(x.BStatus),
            IsSystem = x.IsSystem,
            IsEditable = x.IsEditable,
            ItemCount = countMap.TryGetValue(x.DictTypeCode, out var c) ? c : 0
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    private static List<EDictType> SortRows(
        List<EDictType> rows,
        string field,
        string arrow,
        IReadOnlyDictionary<string, int> countMap)
    {
        var desc = arrow == "1";
        return field switch
        {
            "DictTypeCode" => desc ? rows.OrderByDescending(x => x.DictTypeCode).ToList() : rows.OrderBy(x => x.DictTypeCode).ToList(),
            "DictTypeName" => desc ? rows.OrderByDescending(x => x.DictTypeName).ToList() : rows.OrderBy(x => x.DictTypeName).ToList(),
            "AppCode" => desc ? rows.OrderByDescending(x => x.AppCode).ToList() : rows.OrderBy(x => x.AppCode).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            "IsSystem" => desc ? rows.OrderByDescending(x => x.IsSystem).ToList() : rows.OrderBy(x => x.IsSystem).ToList(),
            "IsEditable" => desc ? rows.OrderByDescending(x => x.IsEditable).ToList() : rows.OrderBy(x => x.IsEditable).ToList(),
            "ItemCount" => desc
                ? rows.OrderByDescending(x => countMap.TryGetValue(x.DictTypeCode, out var c) ? c : 0).ToList()
                : rows.OrderBy(x => countMap.TryGetValue(x.DictTypeCode, out var c) ? c : 0).ToList(),
            _ => rows.OrderBy(x => x.AppCode).ThenBy(x => x.DictTypeCode).ToList()
        };
    }

    public async Task<EDictTypeListRowVm?> GetTypeHeaderAsync(string dictTypeCode, CancellationToken ct)
    {
        var code = (dictTypeCode ?? "").Trim();
        if (code.Length == 0) return null;
        var x = await _db.EDictTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.DictTypeCode == code && !t.IsDeleted, ct);
        if (x == null) return null;
        var itemCount = await _db.EDictItems.AsNoTracking()
            .CountAsync(i => i.DictTypeCode == code && !i.IsDeleted, ct);
        return new EDictTypeListRowVm
        {
            DictTypeCode = x.DictTypeCode,
            DictTypeName = x.DictTypeName,
            AppCode = x.AppCode,
            BStatusText = ToStatusDisplay(x.BStatus),
            IsSystem = x.IsSystem,
            IsEditable = x.IsEditable,
            ItemCount = itemCount
        };
    }

    public async Task<(IReadOnlyList<EDictItemListRowVm> pageRows, int total, int totalPages, int page)> GetItemsPageAsync(
        string dictTypeCode,
        string? bStatus1,
        string searchContent,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var code = (dictTypeCode ?? "").Trim();
        var rows = await _db.EDictItems.AsNoTracking()
            .Where(i => i.DictTypeCode == code && !i.IsDeleted)
            .ToListAsync(ct);

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
            rows = rows.Where(x =>
                (x.ItemCode ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)
                || (x.ItemName ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();

        rows = rows.OrderBy(x => x.DispSeq).ThenBy(x => x.ItemCode).ToList();
        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EDictItemListRowVm
        {
            DataId = x.DataId,
            ItemCode = x.ItemCode,
            ItemName = x.ItemName,
            ItemNameEn = x.ItemNameEn,
            ParentItemCode = x.ParentItemCode,
            DispSeq = x.DispSeq,
            BStatusText = ToStatusDisplay(x.BStatus),
            IsSystem = x.IsSystem
        }).ToList();

        return (pageRows, total, totalPages, page);
    }
}
