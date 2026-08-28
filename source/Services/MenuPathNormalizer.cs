namespace FamilyTree.Services;

/// <summary>将资源 <c>MenuPath</c> 规范化为站内 MVC 路径。</summary>
public static class MenuPathNormalizer
{
    public static string Normalize(string? href)
    {
        if (string.IsNullOrWhiteSpace(href)) return href ?? "";
        var t = href.Trim();
        if (t == "#" || t.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            return t;
        if (!t.StartsWith('/'))
            t = "/" + t.TrimStart('/');
        return t;
    }
}
