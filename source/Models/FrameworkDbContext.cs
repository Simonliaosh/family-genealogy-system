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
    }
}
