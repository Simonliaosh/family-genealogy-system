using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FamilyTree.Helpers;

/// <summary>
/// 将列表页筛选/排序/分页状态编码为单个 query/form 参数，编辑保存后回到原列表位置。
/// </summary>
public static class PolistReturnToken
{
    private const int MaxPayloadBytes = 6144;

    private static readonly JsonSerializerOptions JsonOpt = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string? Encode(IReadOnlyDictionary<string, string?>? state)
    {
        if (state == null || state.Count == 0) return null;
        var dict = state
            .Where(kv => kv.Value != null)
            .ToDictionary(static kv => kv.Key, static kv => kv.Value ?? "", StringComparer.Ordinal);
        if (dict.Count == 0) return null;
        var json = JsonSerializer.Serialize(dict, JsonOpt);
        var bytes = Encoding.UTF8.GetBytes(json);
        if (bytes.Length > MaxPayloadBytes) return null;
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public static RouteValueDictionary? DecodeToRoute(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var bytes = WebEncoders.Base64UrlDecode(token.Trim());
            var json = Encoding.UTF8.GetString(bytes);
            var d = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOpt);
            if (d == null || d.Count == 0) return null;
            var r = new RouteValueDictionary();
            foreach (var kv in d)
                r[kv.Key] = kv.Value;
            return r;
        }
        catch
        {
            return null;
        }
    }

    public static IActionResult RedirectToIndex(Controller controller, string? polistRt)
    {
        var r = DecodeToRoute(polistRt);
        if ((r == null || r.Count == 0) && controller.Request.HasFormContentType)
            r = BuildRouteFromForm(controller.Request.Form);
        if (r != null && r.Count > 0)
            return controller.RedirectToAction("Index", r);
        return controller.RedirectToAction("Index");
    }

    /// <summary>供「关闭」链到 Index：有 token 则展开为路由值，否则空字典。</summary>
    public static RouteValueDictionary IndexRoute(string? polistRt) =>
        DecodeToRoute(polistRt) ?? new RouteValueDictionary();

    private static RouteValueDictionary? BuildRouteFromForm(IFormCollection form)
    {
        if (form == null || form.Count == 0) return null;
        var r = new RouteValueDictionary();
        foreach (var kv in form)
        {
            var key = kv.Key;
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (key.Equals("__RequestVerificationToken", StringComparison.OrdinalIgnoreCase)) continue;
            if (key.Equals("polistRt", StringComparison.OrdinalIgnoreCase)) continue;
            if (key.Equals("id", StringComparison.OrdinalIgnoreCase)) continue;
            if (key.Equals("ids", StringComparison.OrdinalIgnoreCase)) continue;
            if (key.Equals("selectNo", StringComparison.OrdinalIgnoreCase)) continue;

            var val = kv.Value.ToString();
            if (string.IsNullOrWhiteSpace(val)) continue;
            r[key] = val;
        }
        return r.Count == 0 ? null : r;
    }
}
