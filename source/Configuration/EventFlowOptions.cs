namespace FamilyTree.Configuration;

/// <summary>事件流转扩展配置（CALL_API 回调等）。</summary>
public sealed class EventFlowOptions
{
    public const string SectionName = "EventFlow";

    /// <summary>本站点对外基址，如 http://localhost:5280；与规则 Remark 中路径拼接。</summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>可选：/EventDemo/ApiCallback 校验头 X-EventFlow-Secret。</summary>
    public string? CallbackSecret { get; set; }

    public int CallApiTimeoutSeconds { get; set; } = 15;

    /// <summary>按 RuleCode 配置完整 URL 或相对路径（优先于 Remark）。</summary>
    public Dictionary<string, string> CallApiUrls { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
