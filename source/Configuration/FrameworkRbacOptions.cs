namespace FamilyTree.Configuration;

/// <summary>框架权限过滤器配置。</summary>
public class FrameworkRbacOptions
{
    public const string SectionName = "FrameworkRbac";

    public bool PreferEUsersLogin { get; set; } = true;

    public HashSet<string> SkipPermissionControllers { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "Home", "Error", "Account", "Member", "MemberGuide", "FtCertify",
        "FtAuthApi", "FtProfileApi", "FtDraftApi", "FtTreeApi", "FtMessageApi", "FtBranchApplyApi", "FtPeerApi"
    };
}
