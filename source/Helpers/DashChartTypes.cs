namespace FamilyTree.Helpers;

/// <summary>仪表盘图表类型枚举 1~9 与显示名称。</summary>
public static class DashChartTypes
{
    private static readonly string[] Names =
    [
        "",
        "KPI数值卡片",
        "折线图",
        "柱状图",
        "饼图/环形图",
        "明细表格",
        "漏斗图",
        "进度仪表盘",
        "快捷菜单小组件",
        "标题滚动列表"
    ];

    public static string GetName(byte chartType) =>
        IsValid(chartType) ? Names[chartType] : "";

    public static bool IsValid(byte chartType) => chartType >= 1 && chartType <= 9;
}
