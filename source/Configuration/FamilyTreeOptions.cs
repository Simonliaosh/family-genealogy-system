namespace FamilyTree.Configuration;

public sealed class FamilyTreeOptions
{
    public const string SectionName = "FamilyTree";
    public string IdCardHashSalt { get; set; } = "change-me-familytree-salt";
    /// <summary>本站唯一编号（GUID）。对接时写入邀请与接点。</summary>
    public string SiteId { get; set; } = "";
    /// <summary>对外可访问的根地址，如 http://host:5070。邀请二维码依赖此项。</summary>
    public string PublicBaseUrl { get; set; } = "";
    public FamilyTreeWeChatOptions WeChat { get; set; } = new();
    public int ApiTokenDays { get; set; } = 30;
    public int PeerInviteDays { get; set; } = 7;
    /// <summary>联邦对端白名单：允许对接的主机名，逐条精确匹配（不含 scheme/端口以外的路径）。空列表=禁用联邦外呼。</summary>
    public List<string> PeerAllowedHosts { get; set; } = new();
}

public sealed class FamilyTreeWeChatOptions
{
    /// <summary>小程序 AppId（jscode2session）。</summary>
    public string AppId { get; set; } = "";
    public string AppSecret { get; set; } = "";
    /// <summary>公众号 AppId（网页授权 oauth2）。与小程序通常不同。</summary>
    public string MpAppId { get; set; } = "";
    public string MpAppSecret { get; set; } = "";
    public string PublicBaseUrl { get; set; } = "";
    /// <summary>
    /// 仅当小程序/公众号 AppId 皆空时有效：用 mockOpenId 模拟微信登录。
    /// <strong>开发专用。</strong>提交受害者的 openid 即以其身份登录，等于完整的认证绕过；
    /// 因此 <c>Program.cs</c> 在生产环境检测到该值为 true 时直接 fail-fast，不允许启动。
    /// </summary>
    public bool AllowDevMock { get; set; }
}
