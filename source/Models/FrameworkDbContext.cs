using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Models;

/// <summary>事件订阅与人员岗位职责体系（Tbl_E_*）。</summary>
public class FrameworkDbContext : DbContext
{
    public FrameworkDbContext(DbContextOptions<FrameworkDbContext> options) : base(options)
    {
    }

    public DbSet<EUser> EUsers { get; set; } = null!;
    public DbSet<EDepartment> EDepartments { get; set; } = null!;
    public DbSet<EPosition> EPositions { get; set; } = null!;
    public DbSet<EEDuty> EEDuties { get; set; } = null!;
    public DbSet<EUserPosition> EUserPositions { get; set; } = null!;
    public DbSet<EPositionDuty> EPositionDuties { get; set; } = null!;
    public DbSet<EEventConfig> EEventConfigs { get; set; } = null!;
    public DbSet<EMenuGroup> EMenuGroups { get; set; } = null!;
    public DbSet<EResource> EResources { get; set; } = null!;
    public DbSet<ESubscription> ESubscriptions { get; set; } = null!;
    public DbSet<EManagerSubordinate> EManagerSubordinates { get; set; } = null!;
    public DbSet<EResourcePermission> EResourcePermissions { get; set; } = null!;
    public DbSet<EEventFlowRule> EEventFlowRules { get; set; } = null!;
    public DbSet<EEventLog> EEventLogs { get; set; } = null!;
    public DbSet<ETodoTask> ETodoTasks { get; set; } = null!;
    public DbSet<ELoginLog> ELoginLogs { get; set; } = null!;
    public DbSet<EUserHandover> EUserHandovers { get; set; } = null!;
    public DbSet<EMember> EMembers { get; set; } = null!;

    public DbSet<EAppModule> EAppModules { get; set; } = null!;
    public DbSet<EDictType> EDictTypes { get; set; } = null!;
    public DbSet<EDictItem> EDictItems { get; set; } = null!;
    public DbSet<EEventInstance> EEventInstances { get; set; } = null!;
    public DbSet<EEventReceiver> EEventReceivers { get; set; } = null!;
    public DbSet<ETodoGroup> ETodoGroups { get; set; } = null!;
    public DbSet<EHrLeaveRequest> HrLeaveRequests { get; set; } = null!;

    public DbSet<FtPerson> FtPersons { get; set; } = null!;
    public DbSet<FtClan> FtClans { get; set; } = null!;
    public DbSet<FtUserClan> FtUserClans { get; set; } = null!;
    public DbSet<FtClanCreateInvite> FtClanCreateInvites { get; set; } = null!;
    public DbSet<FtPersonMarry> FtPersonMarrys { get; set; } = null!;
    public DbSet<FtPersonDraft> FtPersonDrafts { get; set; } = null!;
    public DbSet<FtPersonLink> FtPersonLinks { get; set; } = null!;
    public DbSet<FtMatchConflict> FtMatchConflicts { get; set; } = null!;
    public DbSet<FtMatchExclude> FtMatchExcludes { get; set; } = null!;
    public DbSet<FtGenerationWord> FtGenerationWords { get; set; } = null!;
    public DbSet<FtAccountBind> FtAccountBinds { get; set; } = null!;
    public DbSet<FtApiToken> FtApiTokens { get; set; } = null!;
    public DbSet<FtBranchAdminApply> FtBranchAdminApplies { get; set; } = null!;
    public DbSet<FtPeerInvite> FtPeerInvites { get; set; } = null!;
    public DbSet<FtPeerBridge> FtPeerBridges { get; set; } = null!;
    public DbSet<FtOpLog> FtOpLogs { get; set; } = null!;

    public DbSet<DashIndicator> DashIndicators { get; set; } = null!;
    public DbSet<DashPosTemplate> DashPosTemplates { get; set; } = null!;
    public DbSet<DashUserSetting> DashUserSettings { get; set; } = null!;
    public DbSet<DashUserCard> DashUserCards { get; set; } = null!;
    public DbSet<DashPosIndicatorPerm> DashPosIndicatorPerms { get; set; } = null!;
    public DbSet<DashUserOperLog> DashUserOperLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ETodoTask>()
            .Property(t => t.TodoGroupId)
            .IsRequired(false);

        modelBuilder.Entity<EHrLeaveRequest>(e =>
        {
            e.Property(x => x.Days).HasPrecision(5, 1);
        });

        ConfigureFamilyTree(modelBuilder);
    }

    /// <summary>
    /// 族谱核心表的关系与索引配置。
    /// </summary>
    /// <remarks>
    /// 这里刻意<strong>不</strong>配 <c>HasForeignKey</c>：库里现存数据已有悬空父边
    /// （项目为此专门写了 <c>FtTreeHealthService</c> 的 BROKEN_EDGE 探测器），
    /// 贸然加外键会让既有库无法写入。正确顺序是先跑一次健康扫描清断边，再补外键约束。
    /// 索引部分与 <c>scripts/29-CreateTbl_FamilyTree_Core.sql</c> 一一对应——
    /// 该项目不用 EF Migrations，索引由 SQL 脚本实际创建，这里的声明用于让模型自描述、
    /// 并让将来引入迁移工具时能直接生成。
    /// </remarks>
    private static void ConfigureFamilyTree(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FtPerson>(e =>
        {
            e.HasIndex(x => x.FatherPersonId).HasDatabaseName("IX_FamilyTree_Person_FatherPersonId");
            e.HasIndex(x => x.MotherPersonId).HasDatabaseName("IX_FamilyTree_Person_MotherPersonId");
            e.HasIndex(x => x.SameAsPersonId).HasDatabaseName("IX_FamilyTree_Person_SameAsPersonId");
            e.HasIndex(x => x.OwnerUserId).HasDatabaseName("IX_FamilyTree_Person_OwnerUserId");
            e.HasIndex(x => x.ClanId).HasDatabaseName("IX_FamilyTree_Person_ClanId");
            e.HasIndex(x => new { x.InMainGenealogy, x.IsDeleted }).HasDatabaseName("IX_FamilyTree_Person_InMainGenealogy");
            // 一个用户只能绑定一条「本人」记录
            e.HasIndex(x => x.BindUserId).IsUnique().HasDatabaseName("UX_FamilyTree_Person_BindUserId")
                .HasFilter("[BindUserId] IS NOT NULL AND [IsDeleted] = 0");
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<FtPersonLink>(e =>
        {
            e.HasIndex(x => new { x.SourcePersonId, x.LinkStatus }).HasDatabaseName("IX_FamilyTree_PersonLink_Source");
            e.HasIndex(x => new { x.LinkStatus, x.IsDeleted }).HasDatabaseName("IX_FamilyTree_PersonLink_Status");
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<FtPersonDraft>(e =>
        {
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<FtOpLog>(e =>
        {
            e.HasIndex(x => x.CreateDate).HasDatabaseName("IX_FamilyTree_OpLog_CreateDate");
        });

        modelBuilder.Entity<FtApiToken>(e =>
        {
            e.HasIndex(x => new { x.TokenHash, x.ExpireDate }).HasDatabaseName("IX_FamilyTree_ApiToken_TokenHash");
        });

        modelBuilder.Entity<FtAccountBind>(e =>
        {
            e.HasIndex(x => x.IdCardHash).HasDatabaseName("IX_FamilyTree_AccountBind_IdCardHash");
            e.HasIndex(x => x.WechatOpenId).HasDatabaseName("IX_FamilyTree_AccountBind_WechatOpenId");
        });
    }
}
