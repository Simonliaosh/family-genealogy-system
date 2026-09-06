namespace FamilyTree.Configuration;

/// <summary>限流策略名。全站原先零限流，登录与匿名认证接口可被无限次尝试。</summary>
public static class RateLimitPolicies
{
    /// <summary>登录类端点：每 IP 每分钟 10 次。</summary>
    public const string Login = "ft-login";

    /// <summary>匿名可达的 API：每 IP 每分钟 30 次。</summary>
    public const string AnonymousApi = "ft-anon-api";
}
