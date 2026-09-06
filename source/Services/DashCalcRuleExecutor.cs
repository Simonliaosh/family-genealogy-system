using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FamilyTree.Helpers;
using FamilyTree.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>解析 CalcRule JSON 并执行内置指标或 SQL 统计。</summary>
public sealed class DashCalcRuleExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// <c>CalcRule.sqlText</c> 是存储在 <c>Tbl_Dash_Indicator</c> 里、由后台可编辑的自由 SQL，
    /// 执行时用的是应用自身的 SQL 身份——任何拿到指标增改权的中层账号都能借此读写全库。
    /// 该模块与族谱业务零引用，因此默认<strong>关闭</strong>自由 SQL 通道；
    /// 确需启用的部署必须显式配置 <c>Dashboard:AllowRawCalcSql=true</c>，且 SQL 仍须通过下面的只读白名单校验。
    /// </summary>
    public const string AllowRawSqlConfigKey = "Dashboard:AllowRawCalcSql";

    private static readonly string[] ForbiddenSqlTokens =
    {
        "insert", "update", "delete", "merge", "drop", "alter", "create", "truncate",
        "exec", "execute", "sp_", "xp_", "grant", "revoke", "deny", "backup", "restore",
        "shutdown", "openrowset", "opendatasource", "openquery", "bulk", "waitfor", "into"
    };

    private readonly FrameworkDbContext _db;
    private readonly bool _allowRawSql;

    public DashCalcRuleExecutor(FrameworkDbContext db, IConfiguration config)
    {
        _db = db;
        _allowRawSql = config.GetValue(AllowRawSqlConfigKey, false);
    }

    /// <summary>
    /// 自由 SQL 只允许单条只读 SELECT：不得含分号（阻断批处理）、不得含注释、
    /// 不得出现任何写入/执行类关键字。这是补丁式加固，不是安全边界——正确做法是把该模块整块删掉。
    /// </summary>
    internal static bool IsReadOnlySelect(string? sqlText)
    {
        var sql = (sqlText ?? "").Trim();
        if (sql.Length == 0) return false;
        if (sql.Contains(';') || sql.Contains("--") || sql.Contains("/*")) return false;
        if (!sql.TrimStart().StartsWith("select", StringComparison.OrdinalIgnoreCase)) return false;

        foreach (var tok in ForbiddenSqlTokens)
            if (Regex.IsMatch(sql, $@"(?<![\w@#]){Regex.Escape(tok)}", RegexOptions.IgnoreCase))
                return false;
        return true;
    }

    public DashCalcRuleModel? ParseCalcRule(string? calcRuleJson)
    {
        if (string.IsNullOrWhiteSpace(calcRuleJson))
            return new DashCalcRuleModel();

        try
        {
            return JsonSerializer.Deserialize<DashCalcRuleModel>(calcRuleJson, JsonOptions)
                   ?? new DashCalcRuleModel();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool IsValidCalcRuleJson(string? calcRuleJson)
    {
        if (string.IsNullOrWhiteSpace(calcRuleJson))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(calcRuleJson);
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public async Task<object?> ExecuteAsync(
        DashIndicator indicator,
        int userId,
        int deptId,
        byte timeType,
        CancellationToken ct)
    {
        var code = (indicator.IndicatorCode ?? "").Trim().ToUpperInvariant();
        if (IsBuiltInCode(code))
            return await ExecuteBuiltInAsync(code, userId, deptId, timeType, indicator, ct);

        var rule = ParseCalcRule(indicator.CalcRule);
        if (rule == null)
            return null;

        if (!string.IsNullOrWhiteSpace(rule.SqlText))
        {
            if (!_allowRawSql)
                throw new InvalidOperationException(
                    $"指标自由 SQL 通道已停用。如确需使用，请在配置里显式打开 {AllowRawSqlConfigKey}。");
            if (!IsReadOnlySelect(rule.SqlText))
                throw new InvalidOperationException("指标 SQL 只允许单条只读 SELECT，且不得含分号、注释或写入类语句。");
            return await ExecuteSqlAsync(rule.SqlText, userId, deptId, timeType, ct);
        }

        if (indicator.ChartType == 8 && rule.MenuList is { Count: > 0 })
            return BuildQuickMenuData(rule.MenuList, userId, deptId, timeType);

        return null;
    }

    public static bool IsBuiltInCode(string indicatorCode)
    {
        var code = indicatorCode.Trim().ToUpperInvariant();
        return code is "FRAME_TODO_COUNT" or "FRAME_TODO_LIST" or "FRAME_EVENT_RECENT"
            or "FRAME_ORG_STATS" or "FRAME_QUICK_MENU"
            or "FRAME_DEMO_LINE" or "FRAME_DEMO_BAR" or "FRAME_DEMO_PIE"
            or "FRAME_DEMO_TABLE" or "FRAME_DEMO_FUNNEL" or "FRAME_DEMO_GAUGE";
    }

    public async Task<object?> ExecuteBuiltInAsync(
        string indicatorCode,
        int userId,
        int deptId,
        byte timeType,
        DashIndicator? indicator,
        CancellationToken ct)
    {
        var code = indicatorCode.Trim().ToUpperInvariant();
        return code switch
        {
            "FRAME_TODO_COUNT" => await BuiltInTodoCountAsync(userId, ct),
            "FRAME_TODO_LIST" => await BuiltInTodoListAsync(userId, deptId, timeType, ct),
            "FRAME_EVENT_RECENT" => await BuiltInEventRecentAsync(userId, deptId, timeType, ct),
            "FRAME_ORG_STATS" => await BuiltInOrgStatsAsync(ct),
            "FRAME_QUICK_MENU" => BuiltInQuickMenu(indicator, userId, deptId, timeType),
            "FRAME_DEMO_LINE" => await BuiltInDemoLineAsync(ct),
            "FRAME_DEMO_BAR" => await BuiltInDemoBarAsync(ct),
            "FRAME_DEMO_PIE" => await BuiltInDemoPieAsync(ct),
            "FRAME_DEMO_TABLE" => await BuiltInDemoTableAsync(ct),
            "FRAME_DEMO_FUNNEL" => await BuiltInDemoFunnelAsync(ct),
            "FRAME_DEMO_GAUGE" => await BuiltInDemoGaugeAsync(userId, ct),
            _ => null
        };
    }

    public async Task<object?> ExecuteSqlAsync(
        string sqlText,
        int userId,
        int deptId,
        byte timeType,
        CancellationToken ct)
    {
        // 这里拿到的是 DbContext 自己的连接，不能 await using——那会在方法结束时把它处置掉，
        // 破坏同一个 scoped context 上后续的所有数据库访问。开是我们开的，就由我们关。
        var conn = _db.Database.GetDbConnection();
        var openedHere = conn.State != System.Data.ConnectionState.Open;
        if (openedHere) await conn.OpenAsync(ct);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sqlText;

            AddParam(cmd, "@userId", userId);
            AddParam(cmd, "@deptId", deptId);
            AddParam(cmd, "@timeType", timeType);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                values[name] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            return values.Count == 1 ? values.Values.First() : values;
        }
        finally
        {
            if (openedHere) await conn.CloseAsync();
        }
    }

    public static string ReplaceRoutePlaceholders(string? routePath, IReadOnlyDictionary<string, string?> parameters)
    {
        if (string.IsNullOrWhiteSpace(routePath))
            return "";

        var path = routePath;
        foreach (var kv in parameters)
        {
            if (!string.IsNullOrEmpty(kv.Value))
                path = path.Replace("{" + kv.Key + "}", kv.Value, StringComparison.OrdinalIgnoreCase);
        }

        path = Regex.Replace(path, @"\{[^}]+\}", "");

        var qIndex = path.IndexOf('?', StringComparison.Ordinal);
        if (qIndex < 0)
            return path.TrimEnd('?', '&');

        var basePath = path[..qIndex];
        var query = path[(qIndex + 1)..];
        var kept = new List<string>();
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Contains('{', StringComparison.Ordinal))
                continue;
            var eq = part.IndexOf('=');
            if (eq >= 0 && string.IsNullOrWhiteSpace(part[(eq + 1)..]))
                continue;
            kept.Add(part);
        }

        return kept.Count == 0 ? basePath : basePath + "?" + string.Join("&", kept);
    }

    public static Dictionary<string, string?> BuildRouteParameters(int userId, int deptId, byte timeType) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["userId"] = userId.ToString(),
            ["deptId"] = deptId.ToString(),
            ["timeType"] = timeType.ToString()
        };

    public string? ResolveGlobalRoute(DashCalcRuleModel? rule, int userId, int deptId, byte timeType)
    {
        var route = rule?.GlobalLink?.RoutePath;
        if (string.IsNullOrWhiteSpace(route))
            return null;
        return ReplaceRoutePlaceholders(route, BuildRouteParameters(userId, deptId, timeType));
    }

    public Dictionary<string, string> ResolveItemRoutes(
        DashCalcRuleModel? rule,
        IEnumerable<IDictionary<string, object?>> rows,
        int userId,
        int deptId,
        byte timeType)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var cfg = rule?.ItemLinkConfig;
        if (cfg == null || string.IsNullOrWhiteSpace(cfg.ItemRoute))
            return result;

        var bindFields = cfg.BindDataField ?? [];
        foreach (var row in rows)
        {
            var keyParts = bindFields.Select(f => row.TryGetValue(f, out var v) ? v?.ToString() ?? "" : "").ToArray();
            if (keyParts.Any(string.IsNullOrWhiteSpace))
                continue;
            var key = string.Join("|", keyParts);

            var parameters = BuildRouteParameters(userId, deptId, timeType);
            foreach (var f in bindFields)
            {
                if (row.TryGetValue(f, out var v) && v != null)
                    parameters[f] = v.ToString();
            }
            foreach (var p in cfg.PublicParam ?? [])
            {
                if (parameters.TryGetValue(p, out var pv))
                    parameters[p] = pv;
            }

            result[key] = ReplaceRoutePlaceholders(cfg.ItemRoute, parameters);
        }

        return result;
    }

    private object BuiltInQuickMenu(DashIndicator? indicator, int userId, int deptId, byte timeType)
    {
        var rule = ParseCalcRule(indicator?.CalcRule);
        var menus = rule?.MenuList ?? [];
        return BuildQuickMenuData(menus, userId, deptId, timeType);
    }

    private object BuildQuickMenuData(
        IReadOnlyList<DashCalcMenuItemModel> menus,
        int userId,
        int deptId,
        byte timeType)
    {
        var parameters = BuildRouteParameters(userId, deptId, timeType);
        var menuList = menus.Select(m => new
        {
            icon = m.Icon ?? "",
            name = m.Name ?? "",
            routePath = ReplaceRoutePlaceholders(m.RoutePath, parameters),
            bgColor = m.BgColor ?? "#409eff"
        }).ToList();

        return new { menuList };
    }

    private async Task<object> BuiltInTodoCountAsync(int userId, CancellationToken ct)
    {
        var count = await _db.ETodoTasks.AsNoTracking()
            .CountAsync(x => x.UserId == userId && x.Status == 0, ct);

        return new
        {
            value = count,
            unit = "件",
            targetValue = (decimal?)null,
            finishRate = (decimal?)null,
            lastPeriodValue = (decimal?)null,
            changeRate = (decimal?)null,
            trend = count > 0 ? "up" : "flat",
            warnLevel = count > 0 ? 1 : 0
        };
    }

    private async Task<object> BuiltInTodoListAsync(int userId, int deptId, byte timeType, CancellationToken ct)
    {
        var todos = await _db.ETodoTasks.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == 0)
            .OrderByDescending(x => x.CreateTime)
            .Take(10)
            .ToListAsync(ct);

        var sortNum = 1;
        var list = todos.Select(t =>
        {
            var route = EventObjectRefHelper.ResolveBusinessUrl(t.ObjectType, t.ObjectKey, t.ObjectUrl)
                        ?? $"/ETodoTask/Details/{t.DataId}";
            return new
            {
                sortNum = sortNum++,
                title = string.IsNullOrWhiteSpace(t.TodoTitle)
                    ? $"{t.EventCode} · {EventObjectRefHelper.FormatDisplay(t.ObjectType, t.ObjectKey)}"
                    : t.TodoTitle,
                remark = "待办",
                createTime = t.CreateTime.ToString("yyyy-MM-dd"),
                itemRoute = ReplaceRoutePlaceholders(route, BuildRouteParameters(userId, deptId, timeType))
            };
        }).ToList();

        if (list.Count == 0)
        {
            list.Add(new
            {
                sortNum = 1,
                title = "暂无待处理待办",
                remark = "",
                createTime = DateTime.Today.ToString("yyyy-MM-dd"),
                itemRoute = ReplaceRoutePlaceholders("/ETodoTask/Index", BuildRouteParameters(userId, deptId, timeType))
            });
        }

        return new { isScroll = true, list };
    }

    private async Task<object> BuiltInEventRecentAsync(int userId, int deptId, byte timeType, CancellationToken ct)
    {
        var instances = await _db.EEventInstances.AsNoTracking()
            .OrderByDescending(x => x.OccurredTime)
            .Take(10)
            .ToListAsync(ct);

        var sortNum = 1;
        var list = instances.Select(i => new
        {
            sortNum = sortNum++,
            title = $"{i.AppCode}.{i.EventCode} · {EventObjectRefHelper.FormatDisplay(i.ObjectType, i.ObjectKey)} ({i.EventStatus})",
            remark = "事件",
            createTime = i.OccurredTime.ToString("yyyy-MM-dd"),
            itemRoute = ReplaceRoutePlaceholders(
                $"/EEventInstanceQuery/Details/{i.DataId}",
                BuildRouteParameters(userId, deptId, timeType))
        }).ToList();

        if (list.Count == 0)
        {
            list.Add(new
            {
                sortNum = 1,
                title = "尚无事件实例",
                remark = "",
                createTime = DateTime.Today.ToString("yyyy-MM-dd"),
                itemRoute = ReplaceRoutePlaceholders("/EEventInstanceQuery/Index", BuildRouteParameters(userId, deptId, timeType))
            });
        }

        return new { isScroll = true, list };
    }

    private async Task<object> BuiltInOrgStatsAsync(CancellationToken ct)
    {
        var active = new[] { "1", "启用" };

        var totalUsers = await _db.EUsers.AsNoTracking()
            .CountAsync(x => !x.IsDeleted && active.Contains(x.BStatus ?? ""), ct);

        var totalDepartments = await _db.EDepartments.AsNoTracking()
            .CountAsync(x => !x.IsDeleted && active.Contains(x.BStatus ?? ""), ct);

        var totalDuties = await _db.EEDuties.AsNoTracking()
            .CountAsync(x => !x.IsDeleted && active.Contains(x.BStatus ?? ""), ct);

        return new
        {
            value = totalUsers,
            unit = "人",
            targetValue = (decimal?)null,
            finishRate = (decimal?)null,
            lastPeriodValue = (decimal?)totalDepartments,
            changeRate = totalDepartments > 0 ? Math.Round((decimal)totalUsers / totalDepartments, 2) : (decimal?)null,
            trend = "flat",
            warnLevel = 0,
            extStats = new { totalDepartments, totalDuties }
        };
    }

    private async Task<object> BuiltInDemoLineAsync(CancellationToken ct)
    {
        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
        var logs = await _db.ELoginLogs.AsNoTracking()
            .Where(x => x.LoginTime >= start && x.LoginStatus == "成功")
            .ToListAsync(ct);

        var xAxis = new List<string>();
        var data = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            var month = start.AddMonths(i);
            xAxis.Add($"{month.Month}月");
            data.Add(logs.Count(x => x.LoginTime.Year == month.Year && x.LoginTime.Month == month.Month));
        }

        if (logs.Count == 0)
        {
            xAxis = ["1月", "2月", "3月", "4月", "5月", "6月"];
            data = [12, 18, 15, 22, 28, 35];
        }

        return new
        {
            xAxis,
            series = new[]
            {
                new { seriesName = "登录次数", data }
            }
        };
    }

    private async Task<object> BuiltInDemoBarAsync(CancellationToken ct)
    {
        var active = new[] { "1", "启用" };
        var rows = await _db.EResources.AsNoTracking()
            .Where(x => !x.IsDeleted && active.Contains(x.BStatus ?? ""))
            .ToListAsync(ct);

        var groups = rows
            .GroupBy(x => string.IsNullOrWhiteSpace(x.MenuGroupCode) ? "OTHER" : x.MenuGroupCode!.Trim())
            .OrderByDescending(g => g.Count())
            .Take(8)
            .ToList();

        if (groups.Count == 0)
        {
            return new
            {
                xAxis = new[] { "SYS", "ORG", "EVT", "HR" },
                series = new[] { new { seriesName = "菜单数", data = new[] { 8, 6, 5, 4 } } }
            };
        }

        return new
        {
            xAxis = groups.Select(g => g.Key).ToArray(),
            series = new[] { new { seriesName = "菜单数", data = groups.Select(g => g.Count()).ToArray() } }
        };
    }

    private async Task<object> BuiltInDemoPieAsync(CancellationToken ct)
    {
        var active = new[] { "1", "启用" };
        var rows = await _db.EUsers.AsNoTracking()
            .Where(x => !x.IsDeleted && active.Contains(x.BStatus ?? ""))
            .ToListAsync(ct);

        var slices = rows
            .GroupBy(x => string.IsNullOrWhiteSpace(x.UserType) ? "OTHER" : x.UserType.Trim())
            .Select(g => new { name = g.Key, value = g.Count() })
            .OrderByDescending(x => x.value)
            .ToList();

        if (slices.Count == 0)
        {
            return new object[]
            {
                new { name = "EMPLOYEE", value = 5 },
                new { name = "ADMIN", value = 2 }
            };
        }

        return slices;
    }

    private async Task<object> BuiltInDemoTableAsync(CancellationToken ct)
    {
        var rows = await _db.ELoginLogs.AsNoTracking()
            .OrderByDescending(x => x.LoginTime)
            .Take(10)
            .ToListAsync(ct);

        var tableData = rows.Select(x => new
        {
            loginId = x.LoginId,
            loginStatus = x.LoginStatus,
            loginTime = x.LoginTime.ToString("yyyy-MM-dd HH:mm"),
            ipAddress = x.IPAddress ?? ""
        }).ToList();

        if (tableData.Count == 0)
        {
            tableData.Add(new
            {
                loginId = "cfadmin",
                loginStatus = "成功",
                loginTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                ipAddress = "127.0.0.1"
            });
        }

        return new
        {
            columns = new object[]
            {
                new { key = "loginId", title = "登录名", width = 120 },
                new { key = "loginStatus", title = "状态", width = 80 },
                new { key = "loginTime", title = "登录时间", width = 160 },
                new { key = "ipAddress", title = "IP", width = 120 }
            },
            tableData,
            pageInfo = new { total = tableData.Count, current = 1, pageSize = 10 }
        };
    }

    private async Task<object> BuiltInDemoFunnelAsync(CancellationToken ct)
    {
        var rows = await _db.EEventInstances.AsNoTracking().ToListAsync(ct);
        var slices = rows
            .GroupBy(x => string.IsNullOrWhiteSpace(x.EventStatus) ? "NEW" : x.EventStatus.Trim())
            .Select(g => new { name = g.Key, value = g.Count() })
            .OrderByDescending(x => x.value)
            .ToList();

        if (slices.Count == 0)
        {
            return new object[]
            {
                new { name = "NEW", value = 10 },
                new { name = "PROCESSING", value = 6 },
                new { name = "DONE", value = 4 }
            };
        }

        return slices;
    }

    private async Task<object> BuiltInDemoGaugeAsync(int userId, CancellationToken ct)
    {
        var pending = await _db.ETodoTasks.AsNoTracking()
            .CountAsync(x => x.UserId == userId && x.Status == 0, ct);
        var done = await _db.ETodoTasks.AsNoTracking()
            .CountAsync(x => x.UserId == userId && x.Status != 0, ct);
        var total = pending + done;
        var rate = total == 0 ? 0m : Math.Round((decimal)done * 100m / total, 2);

        return new
        {
            value = rate,
            max = 100m,
            unit = "%",
            title = "待办处理率"
        };
    }

    private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}

public sealed class DashCalcRuleModel
{
    [JsonPropertyName("sqlText")]
    public string? SqlText { get; set; }

    [JsonPropertyName("globalLink")]
    public DashCalcGlobalLinkModel? GlobalLink { get; set; }

    [JsonPropertyName("itemLinkConfig")]
    public DashCalcItemLinkConfigModel? ItemLinkConfig { get; set; }

    [JsonPropertyName("filterDefault")]
    public Dictionary<string, JsonElement>? FilterDefault { get; set; }

    [JsonPropertyName("menuList")]
    public List<DashCalcMenuItemModel>? MenuList { get; set; }
}

public sealed class DashCalcGlobalLinkModel
{
    [JsonPropertyName("routePath")]
    public string? RoutePath { get; set; }

    [JsonPropertyName("linkType")]
    public byte LinkType { get; set; } = 1;

    [JsonPropertyName("paramPlaceholder")]
    public List<string>? ParamPlaceholder { get; set; }
}

public sealed class DashCalcItemLinkConfigModel
{
    [JsonPropertyName("itemRoute")]
    public string? ItemRoute { get; set; }

    [JsonPropertyName("bindDataField")]
    public List<string>? BindDataField { get; set; }

    [JsonPropertyName("publicParam")]
    public List<string>? PublicParam { get; set; }
}

public sealed class DashCalcMenuItemModel
{
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("routePath")]
    public string? RoutePath { get; set; }

    [JsonPropertyName("bgColor")]
    public string? BgColor { get; set; }
}
