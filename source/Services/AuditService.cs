namespace FamilyTree.Services;

/// <summary>审计日志：当前写入应用日志，可替换为数据库/外部系统。</summary>
public sealed class AuditService
{
    private readonly ILogger<AuditService> _logger;

    public AuditService(ILogger<AuditService> logger)
    {
        _logger = logger;
    }

    public Task LogLoginAsync(
        string userKey,
        string displayName,
        bool success,
        string? ip,
        string? userAgent,
        string? detail,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "审计-登录 {Outcome} 用户={UserKey} 显示名={DisplayName} IP={Ip} UA={UserAgent} 说明={Detail}",
            success ? "成功" : "失败",
            userKey,
            displayName,
            ip ?? "-",
            Truncate(userAgent, 120),
            detail ?? "-");
        return Task.CompletedTask;
    }

    private static string Truncate(string? value, int maxLen)
    {
        var v = value ?? "";
        return v.Length <= maxLen ? v : v[..maxLen] + "...";
    }
}
