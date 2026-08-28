namespace FamilyTree.Helpers;

/// <summary>新架构统一业务状态与软删除判断（兼容 启用/1 与 停用/2）。</summary>
public static class EBStatusHelper
{
    public static bool IsActiveBStatus(string? bStatus)
    {
        var t = (bStatus ?? "").Trim();
        return t is "" or "启用" or "1" or "A" or "a";
    }

    public static bool IsDeletedRow(bool isDeleted) => isDeleted;

    public static bool IsUsableRow(string? bStatus, bool isDeleted) =>
        !isDeleted && IsActiveBStatus(bStatus);

    public static string NormalizeBStatusForSave(string? value)
    {
        var v = (value ?? "").Trim();
        return v switch
        {
            "启用" or "1" or "A" or "a" => "1",
            "停用" or "2" => "2",
            _ => string.IsNullOrEmpty(v) ? "1" : v
        };
    }
}
