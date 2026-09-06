using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Helpers;

public static class FtText
{
    public static string NormName(string? value)
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) return "";
        return Regex.Replace(v, @"\s+", "");
    }

    public static int? ParseBirthYear(string? birthDate)
    {
        var s = (birthDate ?? "").Trim();
        if (s.Length < 4) return null;
        var m = Regex.Match(s, @"^\D*(\d{4})");
        if (!m.Success) return null;
        if (!int.TryParse(m.Groups[1].Value, out var y)) return null;
        if (y < 1000 || y > 2200) return null;
        return y;
    }

    public static string? Clip(string? value, int max)
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) return null;
        return v.Length <= max ? v : v[..max];
    }

    public static string ClipReq(string? value, int max, string fallback = "")
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) v = fallback;
        return v.Length <= max ? v : v[..max];
    }

    public static bool IsIdCard(string? raw)
    {
        var s = NormalizeIdCard(raw);
        if (s.Length != 18) return false;
        return Regex.IsMatch(s, @"^\d{17}[\dX]$");
    }

    public static byte GenderFromIdCard(string? idCard)
    {
        var s = NormalizeIdCard(idCard);
        if (s.Length < 17) return 1;
        return (s[16] - '0') % 2 == 0 ? (byte)0 : (byte)1;
    }

    public static string? BirthDateFromIdCard(string? idCard)
    {
        var s = NormalizeIdCard(idCard);
        if (s.Length < 14) return null;
        var y = s.Substring(6, 4);
        var mo = s.Substring(10, 2);
        var d = s.Substring(12, 2);
        if (!int.TryParse(y, out var yi) || yi < 1900 || yi > 2200) return null;
        return y + "-" + mo + "-" + d;
    }

    public static string NormalizeIdCard(string? raw)
    {
        var s = (raw ?? "").Trim().Replace(" ", "").ToUpperInvariant();
        return s;
    }

    /// <summary>族人登录名：字母/数字，至少 6 位（手机号或拼音均可）。</summary>
    public static string NormalizeLoginName(string? raw)
    {
        var s = (raw ?? "").Trim().ToLowerInvariant();
        return s;
    }

    public static bool IsMemberLoginName(string? raw)
    {
        var s = NormalizeLoginName(raw);
        if (s.Length < 6 || s.Length > 32) return false;
        return Regex.IsMatch(s, @"^[a-z0-9]+$");
    }

    public static string HashIdCard(string idCard, string salt)
    {
        var n = NormalizeIdCard(idCard);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes((salt ?? "") + n));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// 登录标识脱敏，供日志与登录日志表使用。身份证登录是一等流程，
    /// 原先 18 位号码会完整落进 <c>Tbl_E_LoginLog</c> 并展示给有权限者——
    /// 这把「身份证只存哈希」的设计意图整个抵消了。保留前 3 后 4，中间打点。
    /// </summary>
    public static string MaskLoginId(string? raw)
    {
        var s = (raw ?? "").Trim();
        if (s.Length == 0) return "";
        if (!IsIdCard(s)) return s;
        return s[..3] + new string('*', s.Length - 7) + s[^4..];
    }
}

public static class FtPaging
{
    /// <summary>
    /// 库侧分页：把 <c>Count</c> / <c>Skip</c> / <c>Take</c> 下推到 SQL。
    /// 与内存版 <see cref="Page{T}"/> 返回同样的四元组，便于逐个列表迁移。
    /// </summary>
    public static async Task<(IReadOnlyList<T> pageRows, int total, int totalPages, int page)> PageAsync<T>(
        IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        if (pageSize <= 0) pageSize = 16;
        if (pageSize > 200) pageSize = 200;

        // 四元组语义必须与内存版 Page 完全一致（含 totalPages 至少为 1），
        // 否则逐个列表迁移时分页条的渲染会静默变样。
        var total = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var rows = total == 0
            ? new List<T>()
            : await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (rows, total, totalPages, page);
    }

    public static (IReadOnlyList<T> pageRows, int total, int totalPages, int page) Page<T>(
        IList<T> rows, int page, int pageSize)
    {
        if (pageSize <= 0) pageSize = 16;
        if (pageSize > 200) pageSize = 200;
        var total = rows.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;
        var skip = (page - 1) * pageSize;
        var slice = rows.Skip(skip).Take(pageSize).ToList();
        return (slice, total, totalPages, page);
    }
}

public static class FtClaims
{
    public static int? UserId(System.Security.Claims.ClaimsPrincipal user)
    {
        var raw = user.FindFirst(FamilyTree.Services.FrameworkClaimTypes.EUserId)?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }

    public static string Operator(System.Security.Claims.ClaimsPrincipal user) =>
        user.FindFirst(FamilyTree.Services.FrameworkClaimTypes.MemberId)?.Value
        ?? user.Identity?.Name
        ?? "system";
}
