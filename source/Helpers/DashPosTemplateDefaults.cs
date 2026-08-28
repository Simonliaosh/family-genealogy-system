namespace FamilyTree.Helpers;

/// <summary>岗位仪表盘默认模板（与 25-Seed_Dashboard.sql 一致）。</summary>
public static class DashPosTemplateDefaults
{
    public sealed record PresetCard(
        string IndicatorCode,
        int LayoutRow,
        int LayoutCol,
        byte ColSpan,
        bool IsLock,
        string CardTitle);

    /// <summary>CF_ADMIN：覆盖 ChartType 1~9 的完整演示布局。</summary>
    public static readonly PresetCard[] AdminFull =
    [
        new("FRAME_QUICK_MENU", 1, 1, 4, false, "快捷入口(8)"),
        new("FRAME_TODO_COUNT", 2, 1, 1, true, "待办数量(1)"),
        new("FRAME_DEMO_GAUGE", 2, 2, 1, false, "待办完成率(7)"),
        new("FRAME_ORG_STATS", 2, 3, 1, false, "组织概览(1)"),
        new("FRAME_DEMO_PIE", 2, 4, 1, false, "用户类型(4)"),
        new("FRAME_DEMO_LINE", 3, 1, 2, false, "登录趋势(2)"),
        new("FRAME_DEMO_BAR", 3, 3, 2, false, "菜单组资源(3)"),
        new("FRAME_DEMO_TABLE", 4, 1, 2, false, "最近登录(5)"),
        new("FRAME_DEMO_FUNNEL", 4, 3, 2, false, "事件漏斗(6)"),
        new("FRAME_TODO_LIST", 5, 1, 2, false, "我的待办(9)"),
        new("FRAME_EVENT_RECENT", 5, 3, 2, false, "近期事件(9)")
    ];

    /// <summary>CF_STAFF：精简布局。</summary>
    public static readonly PresetCard[] StaffBasic =
    [
        new("FRAME_QUICK_MENU", 1, 1, 4, false, "快捷入口"),
        new("FRAME_TODO_COUNT", 2, 1, 1, false, "待办数量"),
        new("FRAME_DEMO_GAUGE", 2, 2, 1, false, "待办完成率"),
        new("FRAME_TODO_LIST", 3, 1, 2, false, "我的待办")
    ];

    public static PresetCard[] ResolvePreset(string? postCode) =>
        AdminFull;
}
