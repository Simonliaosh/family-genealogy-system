using System.Text.Json;

namespace FamilyTree.Helpers;

/// <summary>流转规则 ConditionExpr 最小求值（基于 PayloadJson）。空、*、true 视为匹配。</summary>
public static class EventFlowConditionHelper
{
    public static bool Matches(string? conditionExpr, string? payloadJson)
    {
        var e = (conditionExpr ?? "").Trim();
        if (e.Length == 0 || e == "*" || e.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (e.Equals("false", StringComparison.OrdinalIgnoreCase))
            return false;

        var sepIdx = e.IndexOf('=');
        if (sepIdx < 0) sepIdx = e.IndexOf(':');
        if (sepIdx <= 0)
            return true;

        var key = e[..sepIdx].Trim();
        var want = e[(sepIdx + 1)..].Trim().Trim('"', '\'');
        if (key.Length == 0 || want.Length == 0)
            return true;

        var payload = (payloadJson ?? "").Trim();
        if (payload.Length == 0)
            return false;

        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (TryGetJsonValue(doc.RootElement, key, out var actual))
                return string.Equals(actual, want, StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return payload.Contains($"\"{key}\":\"{want}\"", StringComparison.OrdinalIgnoreCase)
                   || payload.Contains($"\"{key}\": \"{want}\"", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool TryGetJsonValue(JsonElement root, string key, out string value)
    {
        value = "";
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out var direct))
        {
            value = direct.ToString();
            return true;
        }
        foreach (var prop in root.EnumerateObject())
        {
            if (!string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase)) continue;
            value = prop.Value.ToString();
            return true;
        }
        return false;
    }
}
