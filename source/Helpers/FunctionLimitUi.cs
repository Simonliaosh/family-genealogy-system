namespace FamilyTree.Helpers;

/// <summary>
/// 六位 <c>PubFunctionLimit</c> 按钮权限（1-based 位：查/增/改/删）。
/// </summary>
public static class FunctionLimitUi
{
    private static bool BitAt(string? pubFunctionLimit, int oneBasedIndex, bool fallback)
    {
        if (string.IsNullOrEmpty(pubFunctionLimit) || pubFunctionLimit.Length < oneBasedIndex)
            return fallback;
        return pubFunctionLimit[oneBasedIndex - 1] == '1';
    }

    public static bool MidIsOne(string? pubFunctionLimit, int oneBasedIndex) =>
        BitAt(pubFunctionLimit, oneBasedIndex, false);

    public static bool CanView(string? pubFunctionLimit, bool permissionFallback) =>
        BitAt(pubFunctionLimit, 1, permissionFallback);

    public static bool CanCreate(string? pubFunctionLimit, bool permissionFallback) =>
        BitAt(pubFunctionLimit, 2, permissionFallback);

    public static bool CanUpdate(string? pubFunctionLimit, bool permissionFallback) =>
        BitAt(pubFunctionLimit, 3, permissionFallback);

    public static bool CanDelete(string? pubFunctionLimit, bool permissionFallback) =>
        BitAt(pubFunctionLimit, 4, permissionFallback);
}
