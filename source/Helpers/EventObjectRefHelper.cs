namespace FamilyTree.Helpers;

/// <summary>事件/待办业务对象引用展示与业务页跳转。</summary>
public static class EventObjectRefHelper
{
    public static string FormatDisplay(string? objectType, string? objectKey)
    {
        var t = (objectType ?? "").Trim();
        var k = (objectKey ?? "").Trim();
        if (t.Length == 0 && k.Length == 0) return "-";
        if (t.Length == 0) return k;
        if (k.Length == 0) return t;
        return $"{t} / {k}";
    }

    /// <summary>解析站内业务详情相对路径（优先 ObjectUrl，再按 ObjectType 约定）。</summary>
    public static string? ResolveBusinessUrl(string? objectType, string? objectKey, string? objectUrl)
    {
        var url = (objectUrl ?? "").Trim();
        if (url.StartsWith('/'))
            return url;

        var t = (objectType ?? "").Trim();
        var k = (objectKey ?? "").Trim();
        if (t.Length == 0 || k.Length == 0)
            return null;

        if (string.Equals(t, "HrLeaveRequest", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(k, out var leaveId))
            return $"/HrLeave/Details/{leaveId}";

        if (string.Equals(t, "BranchAdminApply", StringComparison.OrdinalIgnoreCase))
            return "/FtBranchAdmin/Index";

        if (string.Equals(t, "PersonLink", StringComparison.OrdinalIgnoreCase))
            return "/FtLinkAudit/Index";

        return null;
    }

    public static string BusinessLinkLabel(string? objectType)
    {
        var t = (objectType ?? "").Trim();
        if (string.Equals(t, "HrLeaveRequest", StringComparison.OrdinalIgnoreCase))
            return "查看请假单";
        if (string.Equals(t, "BranchAdminApply", StringComparison.OrdinalIgnoreCase))
            return "查看分支管理员申请";
        if (string.Equals(t, "PersonLink", StringComparison.OrdinalIgnoreCase))
            return "查看链入单";
        return "打开业务单据";
    }
}
