using System.Text;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>
/// E 登录日志查询（只读）。
/// 数据范围（写死）：<strong>仅本人相关</strong>——<c>LoginId</c> 与当前 <c>MemberID</c> 相同（忽略大小写），
/// 或 <c>UserID</c> 非空且等于当前账号在 <c>Tbl_E_Users</c> 中解析到的 <c>DataID</c>（失败记录 <c>UserID</c> 为空时仍可按 <c>LoginId</c> 命中本人尝试）。
/// 时间范围：未传起止时默认最近 31 天；若传了起止，跨度超过 366 天则在 Service 内将结束日截断到允许的最大跨度。
/// </summary>
public class ELoginLogService
{
    public const int DefaultWindowDays = 31;
    public const int MaxRangeDays = 366;
    public const int ExportMaxRows = 5000;

    private readonly FrameworkDbContext _db;

    public ELoginLogService(FrameworkDbContext db)
    {
        _db = db;
    }

    public static string ToLoginStatusText(string? status)
    {
        var s = (status ?? "").Trim();
        return s switch
        {
            "1" or "成功" or "SUCCESS" or "OK" => "成功",
            "0" or "失败" or "FAIL" or "FAILED" => "失败",
            _ => s.Length == 0 ? "-" : s
        };
    }

    public static List<(string Value, string Text)> BuildLoginStatusFilterOptions() =>
    [
        ("", "全部"),
        ("1", "成功"),
        ("0", "失败"),
        ("成功", "成功"),
        ("失败", "失败"),
        ("SUCCESS", "SUCCESS"),
        ("FAIL", "FAIL")
    ];

    public async Task<int?> ResolveEUserDataIdByMemberIdAsync(string? memberId, CancellationToken ct)
    {
        var m = (memberId ?? "").Trim();
        if (m.Length == 0) return null;
        return await _db.EUsers.AsNoTracking()
            .Where(x => x.LoginId == m)
            .Select(x => (int?)x.DataId)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>应用数据范围与时间窗，返回过滤后的行（内存排序前）。</summary>
    public async Task<List<ELoginLog>> QueryScopedRowsAsync(
        string memberId,
        int? currentEUserId,
        string? loginStatus1,
        string? loginIdFilter,
        string? loginTimeFrom,
        string? loginTimeTo,
        CancellationToken ct)
    {
        var mid = memberId.Trim();
        var midLower = mid.ToLowerInvariant();
        var (fromDt, toDtExclusive) = ResolveTimeRange(loginTimeFrom, loginTimeTo);

        var q = _db.ELoginLogs.AsNoTracking()
            .Where(x => x.LoginTime >= fromDt && x.LoginTime < toDtExclusive)
            .Where(x =>
                x.LoginId.ToLower() == midLower
                || (currentEUserId.HasValue && x.UserId == currentEUserId.Value));

        var st = (loginStatus1 ?? "").Trim();
        if (st.Length > 0)
            q = q.Where(x => x.LoginStatus != null && x.LoginStatus.ToLower() == st.ToLower());

        var lid = (loginIdFilter ?? "").Trim();
        if (lid.Length > 0)
            q = q.Where(x => x.LoginId.Contains(lid));

        return await q.ToListAsync(ct);
    }

    private static (DateTime fromInclusive, DateTime toExclusive) ResolveTimeRange(string? fromStr, string? toStr)
    {
        var now = DateTime.Now;
        DateTime? fromUser = ParseDate(fromStr);
        DateTime? toUser = ParseDate(toStr);

        DateTime fromDt;
        DateTime toExclusive;
        if (!fromUser.HasValue && !toUser.HasValue)
        {
            fromDt = now.Date.AddDays(-DefaultWindowDays);
            toExclusive = now.Date.AddDays(1);
            return (fromDt, toExclusive);
        }

        fromDt = fromUser?.Date ?? now.Date.AddDays(-DefaultWindowDays);
        var toEnd = toUser?.Date ?? fromDt.AddDays(MaxRangeDays);
        if (toEnd < fromDt) toEnd = fromDt;
        if ((toEnd - fromDt).TotalDays > MaxRangeDays)
            toEnd = fromDt.AddDays(MaxRangeDays);
        toExclusive = toEnd.AddDays(1);
        return (fromDt, toExclusive);
    }

    private static DateTime? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s.Trim(), out var d) ? d.Date : null;
    }

    public async Task<(IReadOnlyList<ELoginLogListRowVm> pageRows, int totalRecords, int totalPages, int page, DateTime rangeFrom, DateTime rangeToExclusive)> GetIndexPageAsync(
        string memberId,
        int? currentEUserId,
        string? loginStatus1,
        string? loginIdFilter,
        string? loginTimeFrom,
        string? loginTimeTo,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await QueryScopedRowsAsync(memberId, currentEUserId, loginStatus1, loginIdFilter, loginTimeFrom, loginTimeTo, ct);
        var (fromDt, toExclusive) = ResolveTimeRange(loginTimeFrom, loginTimeTo);

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            var userIds = await ResolveUserIdsByKeywordAsync(kw, ct);
            rows = (searchField ?? "LoginId") switch
            {
                "LoginId" => rows.Where(x => x.LoginId.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "IPAddress" => rows.Where(x => (x.IPAddress ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "FailReason" => rows.Where(x => (x.FailReason ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "UserID" => int.TryParse(kw, out var uid)
                    ? rows.Where(x => x.UserId == uid).ToList()
                    : rows.Where(x => x.UserId.HasValue && userIds.Contains(x.UserId.Value)).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow);

        var userIdsInPage = rows.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct().ToList();
        var users = await _db.EUsers.AsNoTracking().Where(x => userIdsInPage.Contains(x.DataId)).ToListAsync(ct);
        var userMap = users.ToDictionary(x => x.DataId, x => $"{x.LoginId} - {x.RealName}".Trim());

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new ELoginLogListRowVm
        {
            DataId = x.DataId,
            LoginTime = x.LoginTime,
            LoginId = x.LoginId,
            LoginStatusText = ToLoginStatusText(x.LoginStatus),
            FailReasonDisplay = string.IsNullOrWhiteSpace(x.FailReason) ? "-" : x.FailReason!,
            IpDisplay = string.IsNullOrWhiteSpace(x.IPAddress) ? "-" : x.IPAddress!,
            UserDisplay = x.UserId.HasValue && userMap.TryGetValue(x.UserId.Value, out var ud) ? (ud ?? "-") : "-"
        }).ToList();

        return (pageRows, total, totalPages, page, fromDt, toExclusive);
    }

    public async Task<ELoginLogDetailVm?> GetDetailForMemberAsync(long id, string memberId, int? currentEUserId, CancellationToken ct)
    {
        var rows = await _db.ELoginLogs.AsNoTracking().Where(x => x.DataId == id).ToListAsync(ct);
        var x = rows.FirstOrDefault();
        if (x == null) return null;
        var mid = memberId.Trim();
        var ok = string.Equals(x.LoginId, mid, StringComparison.OrdinalIgnoreCase)
                 || (currentEUserId.HasValue && x.UserId == currentEUserId.Value);
        if (!ok) return null;

        var u = x.UserId.HasValue
            ? await _db.EUsers.AsNoTracking().FirstOrDefaultAsync(e => e.DataId == x.UserId.Value, ct)
            : null;
        return new ELoginLogDetailVm
        {
            DataId = x.DataId,
            LoginTime = x.LoginTime,
            LoginId = x.LoginId,
            UserId = x.UserId,
            UserDisplay = u == null ? "-" : $"{u.LoginId} - {u.RealName}".Trim(),
            LoginStatus = x.LoginStatus,
            LoginStatusText = ToLoginStatusText(x.LoginStatus),
            FailReason = x.FailReason,
            IPAddress = x.IPAddress
        };
    }

    public async Task<IReadOnlyList<ELoginLog>> GetExportRowsAsync(
        string memberId,
        int? currentEUserId,
        string? loginStatus1,
        string? loginIdFilter,
        string? loginTimeFrom,
        string? loginTimeTo,
        string searchField,
        string searchContent,
        CancellationToken ct)
    {
        var rows = await QueryScopedRowsAsync(memberId, currentEUserId, loginStatus1, loginIdFilter, loginTimeFrom, loginTimeTo, ct);
        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            var userIds = await ResolveUserIdsByKeywordAsync(kw, ct);
            rows = (searchField ?? "LoginId") switch
            {
                "LoginId" => rows.Where(x => x.LoginId.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "IPAddress" => rows.Where(x => (x.IPAddress ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "FailReason" => rows.Where(x => (x.FailReason ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "UserID" => int.TryParse(kw, out var uid)
                    ? rows.Where(x => x.UserId == uid).ToList()
                    : rows.Where(x => x.UserId.HasValue && userIds.Contains(x.UserId.Value)).ToList(),
                _ => rows
            };
        }

        rows = rows.OrderByDescending(x => x.LoginTime).ThenByDescending(x => x.DataId).Take(ExportMaxRows).ToList();
        return rows;
    }

    public async Task<Dictionary<int, string>> GetUserDisplayMapAsync(IEnumerable<int> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, string>();
        var users = await _db.EUsers.AsNoTracking().Where(x => ids.Contains(x.DataId)).ToListAsync(ct);
        return users.ToDictionary(x => x.DataId, x => $"{x.LoginId} - {x.RealName}".Trim());
    }

    public static byte[] BuildCsv(IReadOnlyList<ELoginLog> rows, IReadOnlyDictionary<int, string> userDisplayById)
    {
        var sb = new StringBuilder();
        sb.AppendLine("DataID,LoginTime,LoginId,UserID,UserDisplay,LoginStatus,FailReason,IPAddress");
        foreach (var x in rows)
        {
            var ud = x.UserId.HasValue && userDisplayById.TryGetValue(x.UserId.Value, out var d) ? d : "";
            sb.Append(x.DataId).Append(',')
                .Append(Csv(x.LoginTime.ToString("yyyy-MM-dd HH:mm:ss"))).Append(',')
                .Append(Csv(x.LoginId)).Append(',')
                .Append(x.UserId?.ToString() ?? "").Append(',')
                .Append(Csv(ud)).Append(',')
                .Append(Csv(x.LoginStatus)).Append(',')
                .Append(Csv(x.FailReason ?? "")).Append(',')
                .Append(Csv(x.IPAddress ?? ""))
                .AppendLine();
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Csv(string? s)
    {
        var v = (s ?? "").Replace("\"", "\"\"", StringComparison.Ordinal);
        if (v.Contains(',') || v.Contains('"') || v.Contains('\n') || v.Contains('\r'))
            return $"\"{v}\"";
        return v;
    }

    private async Task<HashSet<int>> ResolveUserIdsByKeywordAsync(string kw, CancellationToken ct)
    {
        var users = await _db.EUsers.AsNoTracking()
            .Where(x => x.LoginId.Contains(kw) || x.RealName.Contains(kw))
            .Select(x => x.DataId)
            .ToListAsync(ct);
        return users.ToHashSet();
    }

    private static List<ELoginLog> SortRows(List<ELoginLog> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        IEnumerable<ELoginLog> ordered = field switch
        {
            "LoginTime" => desc ? rows.OrderByDescending(x => x.LoginTime) : rows.OrderBy(x => x.LoginTime),
            "LoginId" => desc ? rows.OrderByDescending(x => x.LoginId) : rows.OrderBy(x => x.LoginId),
            "LoginStatus" => desc ? rows.OrderByDescending(x => x.LoginStatus) : rows.OrderBy(x => x.LoginStatus),
            "DataID" => desc ? rows.OrderByDescending(x => x.DataId) : rows.OrderBy(x => x.DataId),
            _ => rows.OrderByDescending(x => x.LoginTime).ThenByDescending(x => x.DataId)
        };
        return ordered.ToList();
    }
}
