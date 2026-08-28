using FamilyTree.Helpers;
using FamilyTree.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>
/// E 体系登录用户：由「用户→岗位→职责→订阅→资源」生成侧栏菜单，
/// 并由「职责→资源按钮权限」与订阅上的 <c>FunctionLimit</c> 合成页面 <c>PubFunctionLimit</c>（与 <see cref="FunctionLimitUi"/> 位序一致）。
/// </summary>
public sealed class EPrincipalAccessService
{
    private const string DefaultMenuGroupName = "系统功能";
    private readonly FrameworkDbContext _db;

    public EPrincipalAccessService(FrameworkDbContext db)
    {
        _db = db;
    }

    public static bool IsActiveBStatus(string? bStatus) => EBStatusHelper.IsActiveBStatus(bStatus);

    /// <summary>订阅类型 RESOURCE / MIXED（仅内存判断，避免 EF 无法翻译带 StringComparison 的 Equals）。</summary>
    private static bool IsResourceOrMixedSubscription(string? subType)
    {
        var t = (subType ?? "").Trim();
        return t.Equals("RESOURCE", StringComparison.OrdinalIgnoreCase)
               || t.Equals("MIXED", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>账号业务状态：排除取消/停用等。</summary>
    public static bool IsEUserAccountUsable(string? bStatus)
    {
        var t = (bStatus ?? "").Trim();
        if (t.Length == 0) return true;
        if (t is "9" or "取消" or "2" or "停用") return false;
        return true;
    }

    public async Task<IReadOnlyList<int>> GetDutyIdsForUserAsync(int eUserId, CancellationToken ct)
    {
        var positionsRaw = await _db.EUserPositions.AsNoTracking()
            .Where(x => x.UserId == eUserId)
            .Select(x => new { x.PosId, x.DeptId, x.BStatus })
            .ToListAsync(ct);
        var positions = positionsRaw.Where(x => IsActiveBStatus(x.BStatus))
            .Select(x => new { x.PosId, x.DeptId })
            .ToList();
        if (positions.Count == 0) return Array.Empty<int>();

        var posIds = positions.Select(x => x.PosId).Distinct().ToList();
        var dutiesRaw = await _db.EPositionDuties.AsNoTracking()
            .Where(pd => posIds.Contains(pd.PosId))
            .Select(pd => new { pd.PosId, pd.DutyId, pd.DeptId, pd.BStatus })
            .ToListAsync(ct);
        var duties = dutiesRaw.Where(pd => IsActiveBStatus(pd.BStatus))
            .Select(pd => new { pd.PosId, pd.DutyId, pd.DeptId })
            .ToList();

        var set = new HashSet<int>();
        foreach (var up in positions)
        {
            foreach (var pd in duties.Where(d => d.PosId == up.PosId))
            {
                if (pd.DeptId == null || pd.DeptId == up.DeptId)
                    set.Add(pd.DutyId);
            }
        }
        return set.ToList();
    }

    public async Task<List<MenuGroupModel>> GetSidebarMenuAsync(int eUserId, CancellationToken ct)
    {
        var dutyIds = await GetDutyIdsForUserAsync(eUserId, ct);
        if (dutyIds.Count == 0)
            return new List<MenuGroupModel>();

        // 超管不合并族人/支链/族谱管理业务菜单，仍保留超管岗与其它框架岗订阅
        var dutyCodes = await _db.EEDuties.AsNoTracking()
            .Where(d => dutyIds.Contains(d.DataId) && !d.IsDeleted)
            .Select(d => new { d.DataId, d.DutyCode })
            .ToListAsync(ct);
        if (dutyCodes.Any(d => string.Equals(d.DutyCode, "FT_SUPER_ADMIN", StringComparison.OrdinalIgnoreCase)))
        {
            static bool IsFtBusinessDuty(string? code) =>
                string.Equals(code, "FT_MEMBER", StringComparison.OrdinalIgnoreCase)
                || string.Equals(code, "FT_BRANCH_ADMIN", StringComparison.OrdinalIgnoreCase)
                || string.Equals(code, "FT_CLAN_ADMIN", StringComparison.OrdinalIgnoreCase);
            dutyIds = dutyCodes.Where(d => !IsFtBusinessDuty(d.DutyCode)).Select(d => d.DataId).ToList();
        }

        var userDeptIds = (await _db.EUserPositions.AsNoTracking()
                .Where(x => x.UserId == eUserId)
                .ToListAsync(ct))
            .Where(x => IsActiveBStatus(x.BStatus))
            .Select(x => x.DeptId)
            .Distinct()
            .ToList();

        var subs = (await _db.ESubscriptions.AsNoTracking()
                .Where(s => dutyIds.Contains(s.DutyId))
                .Where(s => !s.IsDeleted)
                .Where(s => s.ResourceId != null && s.ResourceId != "")
                .Where(s => s.SubType != null)
                .Where(s => s.DeptId == null || userDeptIds.Contains(s.DeptId.Value))
                .ToListAsync(ct))
            .Where(s => IsResourceOrMixedSubscription(s.SubType))
            .Where(s => IsActiveBStatus(s.BStatus))
            .ToList();

        var resIds = subs.Select(s => s.ResourceId!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (resIds.Count == 0) return new List<MenuGroupModel>();

        var resources = (await _db.EResources.AsNoTracking()
                .Where(r => resIds.Contains(r.ResourceId))
                .Where(r => !r.IsDeleted)
                .ToListAsync(ct))
            .Where(r => IsActiveBStatus(r.BStatus) || string.Equals((r.BStatus ?? "").Trim(), "启用", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var resById = resources.ToDictionary(x => x.ResourceId, x => x, StringComparer.OrdinalIgnoreCase);

        var orderedHits = subs
            .Where(s => resById.ContainsKey(s.ResourceId!))
            .OrderBy(s => s.DispSeq)
            .ThenBy(s => s.DataId)
            .Select(s => (Sub: s, Res: resById[s.ResourceId!]))
            .ToList();

        var groupResources = new List<(ESubscription Sub, EResource Res)>();
        var leafResources = new List<(ESubscription Sub, EResource Res)>();
        foreach (var hit in orderedHits)
        {
            if (IsMenuGroup(hit.Res))
                groupResources.Add((hit.Sub, hit.Res));
            else
                leafResources.Add((hit.Sub, hit.Res));
        }

        var groups = new List<MenuGroupModel>();
        var usedLeaves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var activeMenuGroups = await _db.EMenuGroups.AsNoTracking()
            .Where(g => !g.IsDeleted)
            .ToListAsync(ct);
        var menuGroupMap = activeMenuGroups
            .Where(g => IsActiveBStatus(g.BStatus))
            .ToDictionary(g => (g.MenuGroupCode ?? "").Trim(), g => g, StringComparer.OrdinalIgnoreCase);
        var eventConfigs = await _db.EEventConfigs.AsNoTracking()
            .Where(e => !e.IsDeleted)
            .ToListAsync(ct);
        var eventMenuMap = eventConfigs
            .Where(e => IsActiveBStatus(e.BStatus))
            .Where(e => !string.IsNullOrWhiteSpace(e.MenuGroupCode))
            .Where(e => !string.IsNullOrWhiteSpace(e.PageUrl))
            .OrderBy(e => e.DispSeq)
            .ThenBy(e => e.DataId)
            .ToList();
        var pageToMenuGroup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ev in eventMenuMap)
        {
            var key = CanonicalMenuPath(ev.PageUrl);
            var code = (ev.MenuGroupCode ?? "").Trim();
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(code)) continue;
            if (pageToMenuGroup.ContainsKey(key)) continue;
            pageToMenuGroup[key] = code;
        }

        // 菜单归属优先取 Resource.MenuGroupCode；仅在为空时回退 EventConfig.MenuGroupCode。
        var explicitGroupedItems = new Dictionary<string, List<MenuItemModel>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, r2) in leafResources.OrderBy(x => x.Sub.DispSeq).ThenBy(x => x.Res.ResourceName))
        {
            var rid = r2.ResourceId.Trim();
            if (usedLeaves.Contains(rid)) continue;
            var href = MapHref(r2.MenuPath);
            if (string.IsNullOrWhiteSpace(href) || href == "#") continue;
            var code = ResolveMenuGroupCodeForResource(r2, pageToMenuGroup);
            if (string.IsNullOrWhiteSpace(code) || !menuGroupMap.ContainsKey(code)) continue;

            if (!explicitGroupedItems.TryGetValue(code, out var list))
            {
                list = new List<MenuItemModel>();
                explicitGroupedItems[code] = list;
            }
            list.Add(new MenuItemModel((r2.ResourceName ?? rid).Trim(), href.Trim(), null));
            usedLeaves.Add(rid);
        }
        foreach (var kv in explicitGroupedItems
                     .OrderBy(x => menuGroupMap[x.Key].DispSeq)
                     .ThenBy(x => menuGroupMap[x.Key].MenuGroupName))
        {
            var mg = menuGroupMap[kv.Key];
            if (kv.Value.Count == 0) continue;
            groups.Add(new MenuGroupModel((mg.MenuGroupName ?? kv.Key).Trim(), kv.Value));
        }

        foreach (var (sub, res) in groupResources.DistinctBy(x => x.Res.ResourceId, StringComparer.OrdinalIgnoreCase))
        {
            var prefix = res.ResourceId.Trim();
            var children = new List<MenuItemModel>();
            foreach (var (s2, r2) in leafResources)
            {
                var rid = r2.ResourceId.Trim();
                if (usedLeaves.Contains(rid)) continue;
                if (rid.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase) ||
                    rid.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase))
                {
                    var href = MapHref(r2.MenuPath);
                    if (string.IsNullOrWhiteSpace(href) || href == "#") continue;
                    children.Add(new MenuItemModel((r2.ResourceName ?? rid).Trim(), href.Trim(), null));
                    usedLeaves.Add(rid);
                }
            }
            if (children.Count > 0)
                groups.Add(new MenuGroupModel((res.ResourceName ?? prefix).Trim(), children));
        }

        var orphanLeaves = leafResources
            .Where(x => !usedLeaves.Contains(x.Res.ResourceId.Trim()))
            .DistinctBy(x => x.Res.ResourceId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (orphanLeaves.Count > 0)
        {
            var items = new List<MenuItemModel>();
            foreach (var (_, r2) in orphanLeaves.OrderBy(x => x.Sub.DispSeq).ThenBy(x => x.Res.ResourceName))
            {
                var href = MapHref(r2.MenuPath);
                if (string.IsNullOrWhiteSpace(href) || href == "#") continue;
                items.Add(new MenuItemModel((r2.ResourceName ?? r2.ResourceId).Trim(), href.Trim(), null));
            }
            if (items.Count > 0)
                groups.Add(new MenuGroupModel(DefaultMenuGroupName, items));
        }

        return groups;
    }

    public async Task<(string FunctionLimit, string BusinessLimit)?> TryAuthorizeMvcAsync(
        int eUserId,
        string controller,
        string action,
        CancellationToken ct,
        string? requestPath = null)
    {
        var dutyIds = (await GetDutyIdsForUserAsync(eUserId, ct)).ToHashSet();
        if (dutyIds.Count == 0) return null;

        var userDeptIds = (await _db.EUserPositions.AsNoTracking()
                .Where(x => x.UserId == eUserId)
                .ToListAsync(ct))
            .Where(x => IsActiveBStatus(x.BStatus))
            .Select(x => x.DeptId)
            .Distinct()
            .ToList();

        var requestPaths = BuildMvcPathAliases(controller, action, requestPath);
        if (requestPaths.Count == 0) return null;
        var ctrlReq = (controller ?? "").Trim();

        var subs = (await _db.ESubscriptions.AsNoTracking()
                .Where(s => dutyIds.Contains(s.DutyId))
                .Where(s => !s.IsDeleted)
                .Where(s => s.ResourceId != null && s.ResourceId != "")
                .Where(s => s.SubType != null)
                .Where(s => s.DeptId == null || userDeptIds.Contains(s.DeptId.Value))
                .ToListAsync(ct))
            .Where(s => IsResourceOrMixedSubscription(s.SubType))
            .Where(s => IsActiveBStatus(s.BStatus))
            .ToList();

        var resIds = subs.Select(s => s.ResourceId!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (resIds.Count == 0) return null;

        var resources = (await _db.EResources.AsNoTracking()
                .Where(r => resIds.Contains(r.ResourceId))
                .Where(r => !r.IsDeleted)
                .ToListAsync(ct))
            .Where(r => IsActiveBStatus(r.BStatus) || string.Equals((r.BStatus ?? "").Trim(), "启用", StringComparison.OrdinalIgnoreCase))
            .ToList();

        EResource? best = null;
        var bestLen = -1;
        foreach (var r in resources)
        {
            if (IsMenuGroup(r)) continue;
            var mp = CanonicalMenuPath(r.MenuPath);
            if (string.IsNullOrEmpty(mp)) continue;
            var compactMenu = CompactPathKey(mp);
            var pathMatch = requestPaths.Any(requestPath =>
                requestPath.Equals(mp, StringComparison.OrdinalIgnoreCase) ||
                requestPath.StartsWith(mp.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase) ||
                CompactPathKey(requestPath).Equals(compactMenu, StringComparison.OrdinalIgnoreCase) ||
                CompactPathKey(requestPath).StartsWith(compactMenu, StringComparison.OrdinalIgnoreCase));
            // 菜单只配到 /Xxx/Index 时，Create/Edit/Details 等同控制器下动作无法靠前缀匹配，按控制器名归并
            if (!pathMatch && ctrlReq.Length > 0)
            {
                var mpCtrl = MvcControllerSegmentFromPath(mp);
                if (mpCtrl != null &&
                    string.Equals(mpCtrl, ctrlReq, StringComparison.OrdinalIgnoreCase))
                    pathMatch = true;
            }

            if (pathMatch && mp.Length > bestLen)
            {
                bestLen = mp.Length;
                best = r;
            }
        }

        if (best == null) return null;

        var rid = best.ResourceId.Trim();
        var dutiesForResource = subs
            .Where(s => string.Equals(s.ResourceId?.Trim(), rid, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.DutyId)
            .Distinct()
            .ToList();

        var fn = await MergeFunctionLimitForResourceAsync(rid, dutiesForResource, subs, ct);
        return (fn, "000000");
    }

    private async Task<string> MergeFunctionLimitForResourceAsync(
        string resourceId,
        IReadOnlyList<int> dutyIds,
        IReadOnlyList<ESubscription> subsForContext,
        CancellationToken ct)
    {
        var subFn = subsForContext
            .Where(s => string.Equals(s.ResourceId?.Trim(), resourceId, StringComparison.OrdinalIgnoreCase))
            .Where(s => dutyIds.Contains(s.DutyId))
            .Select(s => (s.FunctionLimit ?? "").Trim())
            .FirstOrDefault(x => x.Length > 0);

        var perms = (await _db.EResourcePermissions.AsNoTracking()
                .Where(p => dutyIds.Contains(p.DutyId))
                .ToListAsync(ct))
            .Where(p => string.Equals(p.ResourceId.Trim(), resourceId, StringComparison.OrdinalIgnoreCase))
            .Where(p => IsActiveBStatus(p.BStatus) || string.Equals((p.BStatus ?? "").Trim(), "启用", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var view = perms.Any(p => p.CanQuery);
        var create = perms.Any(p => p.CanCreate);
        var update = perms.Any(p => p.CanUpdate);
        var del = perms.Any(p => p.CanDelete);

        var fromBits = ToFunctionLimit(view, create, update, del);
        if (fromBits == "000000" && string.IsNullOrEmpty(subFn))
            fromBits = "100000";
        if (!string.IsNullOrEmpty(subFn))
            return MergeFunctionLimitStrings(fromBits, subFn);
        return fromBits;
    }

    private static string ToFunctionLimit(bool view, bool create, bool update, bool delete)
    {
        // FunctionLimitUi：1=查看 2=添加 3=修改 4=删除
        Span<char> s = stackalloc char[6];
        for (var i = 0; i < 6; i++) s[i] = '0';
        if (view) s[0] = '1';
        if (create) s[1] = '1';
        if (update) s[2] = '1';
        if (delete) s[3] = '1';
        return new string(s);
    }

    /// <summary>与订阅串按位取较大权限（任一为 1 则 1）。</summary>
    private static string MergeFunctionLimitStrings(string fromPerms, string fromSub)
    {
        var a = PadSix(fromPerms);
        var b = PadSix(fromSub);
        Span<char> o = stackalloc char[6];
        for (var i = 0; i < 6; i++)
            o[i] = (a[i] == '1' || b[i] == '1') ? '1' : '0';
        return new string(o);
    }

    private static string PadSix(string? s)
    {
        var t = (s ?? "").Trim();
        if (t.Length >= 6) return t[..6];
        return t.PadRight(6, '0');
    }

    private static bool IsMenuGroup(EResource r)
    {
        var typ = (r.ResourceType ?? "").Trim().ToUpperInvariant();
        if (typ is "GROUP" or "MODULE" or "FOLDER") return true;
        var path = (r.MenuPath ?? "").Trim();
        return path.Length == 0 || path == "#" || path.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase);
    }

    private static string MapHref(string? menuPath)
    {
        var c = CanonicalMenuPath(menuPath);
        return string.IsNullOrEmpty(c) ? "#" : MenuPathNormalizer.Normalize(c);
    }

    private static string CanonicalMvcPath(string controller, string action)
    {
        var c = (controller ?? "").Trim();
        var a = (action ?? "").Trim();
        if (c.Length == 0) return "";
        if (a.Length == 0) return "/" + c;
        return "/" + c + "/" + a;
    }

    /// <summary>
    /// MVC 动作路径别名：兼容约定路由与 PM 属性路由（/PM/Contract 等）。
    /// </summary>
    private static List<string> BuildMvcPathAliases(string controller, string action, string? requestPath)
    {
        var list = new List<string>();
        var c = (controller ?? "").Trim();
        var a = (action ?? "").Trim();
        if (c.Length == 0) return list;

        static void AddAlias(List<string> aliases, string? path)
        {
            var p = CanonicalMenuPath(path);
            if (string.IsNullOrWhiteSpace(p)) return;
            if (!aliases.Contains(p, StringComparer.OrdinalIgnoreCase))
                aliases.Add(p);
        }

        AddAlias(list, CanonicalMvcPath(c, a));
        AddAlias(list, requestPath);
        if (a.Equals("Index", StringComparison.OrdinalIgnoreCase))
            AddAlias(list, "/" + c);

        // PM 模块使用 [Route("PM/Contract")] 风格；控制器名为 PMContractController。
        if (c.StartsWith("PM", StringComparison.OrdinalIgnoreCase) && c.Length > 2)
        {
            var leaf = c[2..];
            AddAlias(list, "/PM/" + leaf);
            if (a.Length > 0)
                AddAlias(list, "/PM/" + leaf + "/" + a);
        }

        return list;
    }

    private static string? CanonicalMenuPath(string? menuPath)
    {
        if (string.IsNullOrWhiteSpace(menuPath)) return null;
        var t = menuPath.Trim().Replace('\\', '/');
        if (t.StartsWith("~/", StringComparison.Ordinal)) t = t[2..];
        if (!t.StartsWith('/')) t = "/" + t;
        return t;
    }

    /// <summary>从规范路径取 MVC 控制器段，如 <c>/EUsers/Index</c> → <c>EUsers</c>。</summary>
    private static string? MvcControllerSegmentFromPath(string canonicalPath)
    {
        var t = (canonicalPath ?? "").Trim().TrimStart('/').Replace('\\', '/');
        if (t.Length == 0) return null;
        var slash = t.IndexOf('/');
        return slash < 0 ? t : t[..slash];
    }

    /// <summary>
    /// 路径紧凑键：去除分隔符并转小写，兼容 /PM/Partner 与 /PMPartner/Index。
    /// </summary>
    private static string CompactPathKey(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        var chars = path.Trim()
            .Where(ch => char.IsLetterOrDigit(ch))
            .Select(char.ToLowerInvariant)
            .ToArray();
        return new string(chars);
    }

    private static string? ResolveMenuGroupCodeForResource(
        EResource resource,
        IReadOnlyDictionary<string, string> pageToMenuGroup)
    {
        var codeFromResource = (resource.MenuGroupCode ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(codeFromResource))
            return codeFromResource;

        var menuPath = resource.MenuPath;
        var path = CanonicalMenuPath(menuPath);
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (pageToMenuGroup.TryGetValue(path, out var exact)) return exact;

        var ctrl = MvcControllerSegmentFromPath(path);
        if (string.IsNullOrWhiteSpace(ctrl)) return null;
        foreach (var kv in pageToMenuGroup)
        {
            var evCtrl = MvcControllerSegmentFromPath(kv.Key);
            if (!string.IsNullOrWhiteSpace(evCtrl) &&
                string.Equals(evCtrl, ctrl, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }
        return null;
    }
}
