using FamilyTree.Helpers;
using FamilyTree.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>字典读取（Tbl_E_DictType / Tbl_E_DictItem）。</summary>
public sealed class DictService
{
    private readonly FrameworkDbContext _db;

    public DictService(FrameworkDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<(string Code, string Name)>> GetItemsAsync(
        string dictTypeCode,
        string? appCode = null,
        CancellationToken ct = default)
    {
        var typeCode = (dictTypeCode ?? "").Trim();
        if (typeCode.Length == 0) return Array.Empty<(string, string)>();

        if (!await IsActiveDictTypeAsync(typeCode, appCode, ct))
            return Array.Empty<(string, string)>();

        var rows = await WhereActiveDictItem(_db.EDictItems.AsNoTracking()
                .Where(i => i.DictTypeCode == typeCode && !i.IsDeleted))
            .OrderBy(i => i.DispSeq)
            .ThenBy(i => i.ItemCode)
            .Select(i => new { i.ItemCode, i.ItemName })
            .ToListAsync(ct);

        return rows.Select(x => (x.ItemCode, x.ItemName)).ToList();
    }

    public async Task<string?> GetItemNameAsync(
        string dictTypeCode,
        string itemCode,
        string? appCode = null,
        CancellationToken ct = default)
    {
        var code = (itemCode ?? "").Trim();
        if (code.Length == 0) return null;
        var typeCode = (dictTypeCode ?? "").Trim();
        if (typeCode.Length == 0 || !await IsActiveDictTypeAsync(typeCode, appCode, ct))
            return null;

        return await WhereActiveDictItem(_db.EDictItems.AsNoTracking()
                .Where(i => i.DictTypeCode == typeCode && i.ItemCode == code && !i.IsDeleted))
            .Select(i => i.ItemName)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>下拉选项：Value=ItemCode，Text=ItemName。</summary>
    public async Task<List<(string Value, string Text)>> GetSelectOptionsAsync(
        string dictTypeCode,
        bool forFilter,
        string? formEmptyLabel = null,
        string? appCode = "FRAME",
        CancellationToken ct = default)
    {
        var items = await GetItemsAsync(dictTypeCode, appCode, ct);
        var list = new List<(string, string)>();
        if (forFilter)
            list.Add(("", "全部"));
        else if (formEmptyLabel != null)
            list.Add(("", formEmptyLabel));

        list.AddRange(items.Select(x => (x.Code, x.Name)));
        return list;
    }

    public async Task<Dictionary<string, string>> GetItemMapAsync(
        string dictTypeCode,
        string? appCode = "FRAME",
        CancellationToken ct = default)
    {
        var items = await GetItemsAsync(dictTypeCode, appCode, ct);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, name) in items)
        {
            if (code.Length == 0) continue;
            map[code] = name;
        }
        return map;
    }

    /// <summary>字典有项则用字典下拉，否则回退内置选项。</summary>
    public async Task<List<(string Value, string Text)>> GetSelectOptionsOrFallbackAsync(
        string dictTypeCode,
        bool forFilter,
        string? formEmptyLabel,
        IReadOnlyList<(string Value, string Text)> fallback,
        string? appCode = "FRAME",
        CancellationToken ct = default)
    {
        var opts = await GetSelectOptionsAsync(dictTypeCode, forFilter, formEmptyLabel, appCode, ct);
        var minCount = forFilter ? 1 : (formEmptyLabel != null ? 1 : 0);
        if (opts.Count > minCount) return opts;

        var list = new List<(string Value, string Text)>();
        if (forFilter)
            list.Add(("", "全部"));
        else if (formEmptyLabel != null)
            list.Add(("", formEmptyLabel));
        list.AddRange(fallback);
        return list;
    }

    /// <summary>字典项优先，未覆盖的 fallback 键补入（兼容旧库内编码）。</summary>
    public async Task<Dictionary<string, string>> GetItemMapMergedAsync(
        string dictTypeCode,
        IReadOnlyDictionary<string, string> fallback,
        string? appCode = "FRAME",
        CancellationToken ct = default)
    {
        var map = await GetItemMapAsync(dictTypeCode, appCode, ct);
        foreach (var kv in fallback)
        {
            if (!map.ContainsKey(kv.Key))
                map[kv.Key] = kv.Value;
        }
        return map;
    }

    /// <summary>字典类型存在且启用；指定 appCode 时校验类型归属（纯 EF 可翻译谓词，勿嵌套 Any）。</summary>
    private async Task<bool> IsActiveDictTypeAsync(string dictTypeCode, string? appCode, CancellationToken ct)
    {
        var q = _db.EDictTypes.AsNoTracking()
            .Where(t => t.DictTypeCode == dictTypeCode && !t.IsDeleted)
            .Where(t => t.BStatus == null || t.BStatus == "" || t.BStatus == "启用" || t.BStatus == "1"
                || t.BStatus == "A" || t.BStatus == "a");
        if (!string.IsNullOrWhiteSpace(appCode))
            q = q.Where(t => t.AppCode == appCode.Trim());
        return await q.AnyAsync(ct);
    }

    private static IQueryable<EDictItem> WhereActiveDictItem(IQueryable<EDictItem> q) =>
        q.Where(i => i.BStatus == null || i.BStatus == "" || i.BStatus == "启用" || i.BStatus == "1"
            || i.BStatus == "A" || i.BStatus == "a");
}
