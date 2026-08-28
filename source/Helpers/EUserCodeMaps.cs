namespace FamilyTree.Helpers;

public static class EUserCodeMaps
{
    public static readonly IReadOnlyList<(string Value, string Text)> UserTypeOptions =
    [
        ("1", "员工"),
        ("2", "客户"),
        ("3", "供应商"),
        ("6", "合作伙伴"),
        ("8", "股东")
    ];

    public static readonly IReadOnlyList<(string Value, string Text)> IsEnabledOptions =
    [
        ("1", "启用"),
        ("0", "停用")
    ];

    public static readonly IReadOnlyList<(string Value, string Text)> IsLockedOptions =
    [
        ("1", "打开"),
        ("0", "锁定")
    ];

    public static readonly IReadOnlyList<(string Value, string Text)> BStatusOptions =
    [
        ("1", "正常"),
        ("0", "录入"),
        ("9", "取消")
    ];

    /// <summary>v1 数字编码 → 显示名（字典 USER_TYPE 未覆盖时补全）。</summary>
    public static IReadOnlyDictionary<string, string> BuiltinUserTypeLabels { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["1"] = "员工",
            ["2"] = "客户",
            ["3"] = "供应商",
            ["6"] = "合作伙伴",
            ["8"] = "股东"
        };

    public static string ToUserTypeText(string? code, IReadOnlyDictionary<string, string>? mergedMap = null)
    {
        var c = (code ?? "").Trim();
        if (c.Length == 0) return c;
        if (mergedMap != null && mergedMap.TryGetValue(c, out var name))
            return name;
        return FindText(UserTypeOptions, c);
    }

    public static string ToText(string? code, string field)
    {
        var c = (code ?? "").Trim();
        return field switch
        {
            "UserType" => ToUserTypeText(c),
            "IsEnabled" => FindText(IsEnabledOptions, c),
            "IsLocked" => FindText(IsLockedOptions, c),
            "BStatus" => FindText(BStatusOptions, c),
            _ => c
        };
    }

    public static List<(string Value, string Text)> WithAll(IReadOnlyList<(string Value, string Text)> options)
    {
        var list = new List<(string Value, string Text)> { ("全部", "全部") };
        list.AddRange(options);
        return list;
    }

    private static string FindText(IReadOnlyList<(string Value, string Text)> options, string code)
    {
        var hit = options.FirstOrDefault(x => string.Equals(x.Value, code, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(hit.Value) ? code : hit.Text;
    }
}
