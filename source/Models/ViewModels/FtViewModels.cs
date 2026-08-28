namespace FamilyTree.Models.ViewModels;

public sealed class FtPersonListRowVm
{
    public int DataId { get; set; }
    public string FullName { get; set; } = "";
    public string FatherName { get; set; } = "";
    public string? MotherName { get; set; }
    public string? BirthDate { get; set; }
    public string InMainText { get; set; } = "";
    public string GenerationText { get; set; } = "";
    public string LifeText { get; set; } = "";
    public string CertText { get; set; } = "";
    public DateTime AmendDate { get; set; }
}

public sealed class FtPersonFormVm
{
    public int DataId { get; set; }
    public string FullName { get; set; } = "";
    public string FatherName { get; set; } = "";
    public string? MotherName { get; set; }
    public string? BirthDate { get; set; }
    public byte Gender { get; set; } = 1;
    public int GenerationNo { get; set; }
    public string? WordOfGeneration { get; set; }
    public int? FatherPersonId { get; set; }
    public int? MotherPersonId { get; set; }
    public string? NickName { get; set; }
    public string? SelfIntro { get; set; }
    public string? WechatId { get; set; }
    public string? DeathInfo { get; set; }
    public bool IsDead { get; set; } = false;
    public bool IsCertified { get; set; } = false;
    public byte PrivacyLevel { get; set; } = 1;
    public bool ShowPhoto { get; set; }
    public bool ShowWechat { get; set; }
    public bool ShowSelfIntro { get; set; }
    public bool ShowBirthDetail { get; set; }
    public bool ShowResume { get; set; }
    public bool PrintAllow { get; set; } = true;
    public string? Remark { get; set; }
    public string BStatus { get; set; } = "1";
    public bool IncludeDescendants { get; set; }
    public bool InMainGenealogy { get; set; }
    public bool KeyLocked { get; set; }
    public bool NameLocked { get; set; }
}

public sealed class FtLineageLinkVm
{
    public int DataId { get; set; }
    public string FullName { get; set; } = "";
}

public sealed class FtLineageVm
{
    public int FromId { get; set; }
    public string FromName { get; set; } = "";
    public int? FatherId { get; set; }
    public string FatherLabel { get; set; } = "";
    public int? MotherId { get; set; }
    public string MotherLabel { get; set; } = "";
    public List<FtLineageLinkVm> Children { get; set; } = new();
}

public sealed class FtDraftListRowVm
{
    public int DataId { get; set; }
    public string FullName { get; set; } = "";
    public string RelationType { get; set; } = "";
    public string AuditStatus { get; set; } = "";
    public int? ResultPersonId { get; set; }
    public DateTime AmendDate { get; set; }
    /// <summary>已入档且已有他人挂入时为 false。</summary>
    public bool AllowDelete { get; set; } = true;
}

public sealed class FtDraftFormVm
{
    public int DataId { get; set; }
    public string FullName { get; set; } = "";
    public string FatherName { get; set; } = "";
    public string? MotherName { get; set; }
    public string? BirthDate { get; set; }
    public string RelationType { get; set; } = "SELF";
    public byte Gender { get; set; } = 1;
    public int GenerationNo { get; set; }
    public string? WordOfGeneration { get; set; }
    public string? Remark { get; set; }
}

/// <summary>草稿入档前：对照已入档（主谱）命中，询问是否按此人加入家族链。</summary>
public sealed class FtDraftArchiveConfirmVm
{
    public int DraftId { get; set; }
    public string FullName { get; set; } = "";
    public string FatherName { get; set; } = "";
    public string? MotherName { get; set; }
    public string? BirthDate { get; set; }
    public string RelationType { get; set; } = "";
    public string RelationLabel { get; set; } = "";
    public List<FtMatchHintVm> Hints { get; set; } = new();
}

public sealed class FtGenWordListRowVm
{
    public int DataId { get; set; }
    public int SeqNo { get; set; }
    public string Word { get; set; } = "";
    public string BStatusText { get; set; } = "";
    public string? Remark { get; set; }
    public DateTime AmendDate { get; set; }
}

public sealed class FtGenWordFormVm
{
    public int DataId { get; set; }
    public int SeqNo { get; set; }
    public string Word { get; set; } = "";
    public string BStatus { get; set; } = "1";
    public string? Remark { get; set; }
}

public sealed class FtLinkListRowVm
{
    public int DataId { get; set; }
    public string SourceName { get; set; } = "";
    public string TargetName { get; set; } = "";
    public string LinkStatus { get; set; } = "";
    public byte? MatchLevel { get; set; }
    public DateTime CreateDate { get; set; }
}

/// <summary>链入申请扫码审批页。</summary>
public sealed class FtLinkScanVm
{
    public int LinkId { get; set; }
    public string LinkStatus { get; set; } = "";
    public byte? MatchLevel { get; set; }
    public string LevelLabel { get; set; } = "";
    public int ApplyUserId { get; set; }
    public DateTime CreateDate { get; set; }
    public int SourceId { get; set; }
    public string SourceName { get; set; } = "";
    public string? SourceFather { get; set; }
    public string? SourceMother { get; set; }
    public string? SourceBirth { get; set; }
    public int TargetId { get; set; }
    public string TargetName { get; set; } = "";
    public string? TargetFather { get; set; }
    public string? TargetMother { get; set; }
    public string? TargetBirth { get; set; }
}

public sealed class FtConflictListRowVm
{
    public int DataId { get; set; }
    public string ConflictType { get; set; } = "";
    public string ResolveStatus { get; set; } = "";
    public string? ConflictDetail { get; set; }
    public DateTime CreateDate { get; set; }
}

public sealed class FtMarryListRowVm
{
    public int DataId { get; set; }
    public int PersonId { get; set; }
    public string PersonName { get; set; } = "";
    public string? SpouseName { get; set; }
    public string? SpouseBirth { get; set; }
    public string MarryType { get; set; } = "";
    public int HouseSeq { get; set; }
    public int? SpousePersonId { get; set; }
    public DateTime AmendDate { get; set; }
    public bool CanEdit { get; set; }
}

public sealed class FtMarryFormVm
{
    public int DataId { get; set; }
    public int PersonId { get; set; }
    public string? PersonName { get; set; }
    public string? SpouseName { get; set; }
    public string? SpouseBirth { get; set; }
    public string MarryType { get; set; } = "原配";
    public int HouseSeq { get; set; } = 99;
    public int? SpousePersonId { get; set; }
    public string? Remark { get; set; }
}

public sealed class FtOpLogListRowVm
{
    public int DataId { get; set; }
    public string OpType { get; set; } = "";
    public string ObjectType { get; set; } = "";
    public string ObjectKey { get; set; } = "";
    public string? Remark { get; set; }
    public DateTime CreateDate { get; set; }
}

public sealed class FtMatchHintVm
{
    public int PersonId { get; set; }
    public string FullName { get; set; } = "";
    public string FatherName { get; set; } = "";
    public string? MotherName { get; set; }
    public string? BirthDate { get; set; }
    public int? BirthYear { get; set; }
    public bool InMain { get; set; }
    public int Level { get; set; }
    /// <summary>命中说明，如「姓名+父母+出生」「姓名+出生年」。</summary>
    public string Reason { get; set; } = "";
}

public sealed class FtMatchScanRowVm
{
    public int SourceId { get; set; }
    public string SourceName { get; set; } = "";
    public string? SourceBirth { get; set; }
    public string? SourceFather { get; set; }
    public string? SourceMother { get; set; }
    public List<FtMatchHintVm> Hints { get; set; } = new();
}

/// <summary>确认链入对照页：源（我的小树）→ 目标（主谱同一人）。</summary>
public sealed class FtLinkConfirmVm
{
    public int SourceId { get; set; }
    public int TargetId { get; set; }
    public byte? MatchLevel { get; set; }
    public string LevelLabel { get; set; } = "";
    public FtLinkSideVm Source { get; set; } = new();
    public FtLinkSideVm Target { get; set; } = new();
    public bool HasAncestorConflict { get; set; }
    public string? ConflictDetail { get; set; }
    public List<string> ChildrenToAttach { get; set; } = new();
    public bool CanSubmit { get; set; }
    public string? BlockReason { get; set; }
}

public sealed class FtLinkSideVm
{
    public int PersonId { get; set; }
    public string FullName { get; set; } = "";
    public string? FatherName { get; set; }
    public string? MotherName { get; set; }
    public string? BirthDate { get; set; }
    public int? BirthYear { get; set; }
    public string? Gender { get; set; }
    public bool InMain { get; set; }
    public string? FatherOnTree { get; set; }
    public string? MotherOnTree { get; set; }
}

public sealed class FtTreeNodeVm
{
    public int Id { get; set; }
    /// <summary>local:123 或 peer:{bridgeId}:{remotePersonId}</summary>
    public string NodeKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Birth { get; set; }
    public bool IsRemote { get; set; }
    /// <summary>个人小树中引用的主谱父母（只读展示，非本人录入）。</summary>
    public bool IsMainRef { get; set; }
    public bool LazyExpand { get; set; }
    public int? BridgeId { get; set; }
    public int? RemotePersonId { get; set; }
    public string? PeerLabel { get; set; }
    public List<FtTreeNodeVm> Children { get; set; } = new();
}

public sealed class FtPeerBridgeRowVm
{
    public int DataId { get; set; }
    public int LocalPersonId { get; set; }
    public string LocalName { get; set; } = "";
    public string PeerBaseUrl { get; set; } = "";
    public string PeerSiteId { get; set; } = "";
    public int PeerPersonId { get; set; }
    public string? PeerLabel { get; set; }
    public string BridgeStatus { get; set; } = "";
    public DateTime CreateDate { get; set; }
}

public sealed class FtPeerAcceptVm
{
    public string InviteCode { get; set; } = "";
    public string PeerBaseUrl { get; set; } = "";
    public int LocalPersonId { get; set; }
    public string? PeerLabel { get; set; }
}

public sealed class FtPeerChildDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Birth { get; set; }
    public byte Gender { get; set; }
    public bool HasChildren { get; set; }
}

public sealed class FtMemberRegisterVm
{
    /// <summary>登录名：手机号或拼音等，至少 6 位字母/数字。</summary>
    public string LoginName { get; set; } = "";
    public string RealName { get; set; } = "";
    public string Password { get; set; } = "";
    public string Password2 { get; set; } = "";
    /// <summary>注册/登录成功后跳转地址（含邀请码落地页）。</summary>
    public string? ReturnUrl { get; set; }
}

public sealed class FtBranchAdminRowVm
{
    public int UserId { get; set; }
    public string LoginId { get; set; } = "";
    public string RealName { get; set; } = "";
    public bool IsBranchAdmin { get; set; }
}

public sealed class FtClanAdminRowVm
{
    public int ClanId { get; set; }
    public string ClanCode { get; set; } = "";
    public string ClanName { get; set; } = "";
    public int OwnerUserId { get; set; }
    public string OwnerName { get; set; } = "";
    public int MemberCount { get; set; }
    public string ClanAdminNames { get; set; } = "";
}

public sealed class FtClanMemberRowVm
{
    public int UserId { get; set; }
    public string LoginId { get; set; } = "";
    public string RealName { get; set; } = "";
    public DateTime JoinDate { get; set; }
    public bool IsClanAdmin { get; set; }
    public bool IsBranchAdmin { get; set; }
}

public sealed class FtProfileFormVm
{
    public int PersonId { get; set; }
    public string FullName { get; set; } = "";
    public string? NickName { get; set; }
    public string? SelfIntro { get; set; }
    public string? WechatId { get; set; }
    public string? Mobile { get; set; }
    public byte PrivacyLevel { get; set; } = 1;
    public bool ShowPhoto { get; set; }
    public bool ShowWechat { get; set; }
    public bool ShowSelfIntro { get; set; }
    public bool ShowBirthDetail { get; set; }
    public bool ShowResume { get; set; }
    public bool PrintAllow { get; set; } = true;
    public List<string> Photos { get; set; } = new();
}

public sealed class FtBranchApplyRowVm
{
    public int DataId { get; set; }
    public int ApplyUserId { get; set; }
    public string LoginId { get; set; } = "";
    public string RealName { get; set; } = "";
    public string? ApplyReason { get; set; }
    public string ApplyStatus { get; set; } = "";
    public string? Remark { get; set; }
    public DateTime CreateDate { get; set; }
}

public sealed class FtBranchApplyStatusVm
{
    public bool IsBranchAdmin { get; set; }
    public bool IsSuperAdmin { get; set; }
    public bool HasPending { get; set; }
    public int? PendingId { get; set; }
    public string? PendingReason { get; set; }
}
