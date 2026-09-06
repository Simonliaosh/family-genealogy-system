/*
================================================================================
  10-EFrame.sql  —  框架基础库（33 张表、72 个索引、15 个外键）

  三处修正，让本脚本能在 Linux/macOS/Docker 上的 SQL Server 与 Azure SQL 上执行：
  ① 库名统一为 FamilyTree。原先本脚本建的是 [EFrame]，而 29~50 号脚本一律 USE [FamilyTree]，
     20~27 号则完全没有 USE，落在 SSMS 当时选中的库上——按 README 顺序执行必然在 29 报
     "Cannot open database FamilyTree"，或者静默把框架种进错误的库。
  ② CREATE DATABASE 去掉硬编码的 D:\webdata 绝对路径，交给实例的默认数据目录；
     并加幂等守卫，库已存在时跳过。
  ③ CREATE USER [pubeasydo] 去掉——该服务器登录在别处不存在，脚本会直接失败。
     需要独立的应用登录时另行按环境创建。

  执行方式：sqlcmd -S <server> -i 10-EFrame.sql
================================================================================
*/
USE [master]
GO
IF DB_ID(N'FamilyTree') IS NULL
    CREATE DATABASE [FamilyTree];
GO
ALTER DATABASE [FamilyTree] SET COMPATIBILITY_LEVEL = 100
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [FamilyTree].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [FamilyTree] SET ANSI_NULL_DEFAULT OFF
GO
ALTER DATABASE [FamilyTree] SET ANSI_NULLS OFF
GO
ALTER DATABASE [FamilyTree] SET ANSI_PADDING OFF
GO
ALTER DATABASE [FamilyTree] SET ANSI_WARNINGS OFF
GO
ALTER DATABASE [FamilyTree] SET ARITHABORT OFF
GO
ALTER DATABASE [FamilyTree] SET AUTO_CLOSE OFF
GO
ALTER DATABASE [FamilyTree] SET AUTO_CREATE_STATISTICS ON
GO
ALTER DATABASE [FamilyTree] SET AUTO_SHRINK OFF
GO
ALTER DATABASE [FamilyTree] SET AUTO_UPDATE_STATISTICS ON
GO
ALTER DATABASE [FamilyTree] SET CURSOR_CLOSE_ON_COMMIT OFF
GO
ALTER DATABASE [FamilyTree] SET CURSOR_DEFAULT  GLOBAL
GO
ALTER DATABASE [FamilyTree] SET CONCAT_NULL_YIELDS_NULL OFF
GO
ALTER DATABASE [FamilyTree] SET NUMERIC_ROUNDABORT OFF
GO
ALTER DATABASE [FamilyTree] SET QUOTED_IDENTIFIER OFF
GO
ALTER DATABASE [FamilyTree] SET RECURSIVE_TRIGGERS OFF
GO
ALTER DATABASE [FamilyTree] SET  DISABLE_BROKER
GO
ALTER DATABASE [FamilyTree] SET AUTO_UPDATE_STATISTICS_ASYNC OFF
GO
ALTER DATABASE [FamilyTree] SET DATE_CORRELATION_OPTIMIZATION OFF
GO
ALTER DATABASE [FamilyTree] SET TRUSTWORTHY OFF
GO
ALTER DATABASE [FamilyTree] SET ALLOW_SNAPSHOT_ISOLATION OFF
GO
ALTER DATABASE [FamilyTree] SET PARAMETERIZATION SIMPLE
GO
ALTER DATABASE [FamilyTree] SET READ_COMMITTED_SNAPSHOT OFF
GO
ALTER DATABASE [FamilyTree] SET HONOR_BROKER_PRIORITY OFF
GO
ALTER DATABASE [FamilyTree] SET  READ_WRITE
GO
ALTER DATABASE [FamilyTree] SET RECOVERY FULL
GO
ALTER DATABASE [FamilyTree] SET  MULTI_USER
GO
ALTER DATABASE [FamilyTree] SET PAGE_VERIFY CHECKSUM
GO
ALTER DATABASE [FamilyTree] SET DB_CHAINING OFF
GO
EXEC sys.sp_db_vardecimal_storage_format N'FamilyTree', N'ON'
GO
USE [FamilyTree]
GO
/* 原有 CREATE USER [pubeasydo] FOR LOGIN [pubeasydo] 已删除：
   该服务器登录只存在于原开发机，任何其他环境执行到这里都会失败。
   需要独立应用登录的部署请按自己的环境单独创建并授权。 */
/****** Object:  Table [dbo].[Tbl_E_UserHandover]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_UserHandover', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_UserHandover](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[SourceUserID] [int] NOT NULL,
	[TargetUserID] [int] NOT NULL,
	[HandoverType] [varchar](30) NOT NULL,
	[AppCode] [varchar](50) NULL,
	[ObjectType] [varchar](50) NULL,
	[HandoverTime] [datetime] NOT NULL,
	[OperatorUserID] [int] NOT NULL,
	[Remark] [nvarchar](max) NULL,
 CONSTRAINT [PK_Tbl_E_UserHandover] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_UserHandover_Source' AND object_id = OBJECT_ID(N'dbo.Tbl_E_UserHandover'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_UserHandover_Source] ON [dbo].[Tbl_E_UserHandover] 
(
	[SourceUserID] ASC,
	[HandoverTime] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_UserHandover_Target' AND object_id = OBJECT_ID(N'dbo.Tbl_E_UserHandover'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_UserHandover_Target] ON [dbo].[Tbl_E_UserHandover] 
(
	[TargetUserID] ASC,
	[HandoverTime] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserHandover', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'交出方用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserHandover', @level2type=N'COLUMN',@level2name=N'SourceUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'接收方用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserHandover', @level2type=N'COLUMN',@level2name=N'TargetUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'交接类型；LEAVE、TRANSFER、RESIGN、TEMP' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserHandover', @level2type=N'COLUMN',@level2name=N'HandoverType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'限定应用；为空表示全部应用' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserHandover', @level2type=N'COLUMN',@level2name=N'AppCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'限定对象类型；为空表示全部对象' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserHandover', @level2type=N'COLUMN',@level2name=N'ObjectType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'交接时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserHandover', @level2type=N'COLUMN',@level2name=N'HandoverTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作人用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserHandover', @level2type=N'COLUMN',@level2name=N'OperatorUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserHandover', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户交接表；记录离职、调岗、临时交接等操作。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserHandover'
GO
/****** Object:  Table [dbo].[Tbl_E_UserDelegate]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_UserDelegate', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_UserDelegate](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[SourceUserID] [int] NOT NULL,
	[AgentUserID] [int] NOT NULL,
	[DelegateType] [varchar](30) NOT NULL,
	[AppCode] [varchar](50) NULL,
	[EventCode] [varchar](100) NULL,
	[BeginTime] [datetime] NOT NULL,
	[EndTime] [datetime] NOT NULL,
	[Reason] [nvarchar](300) NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_UserDelegate] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_UserDelegate_Agent' AND object_id = OBJECT_ID(N'dbo.Tbl_E_UserDelegate'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_UserDelegate_Agent] ON [dbo].[Tbl_E_UserDelegate] 
(
	[AgentUserID] ASC,
	[BStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_UserDelegate_SourceTime' AND object_id = OBJECT_ID(N'dbo.Tbl_E_UserDelegate'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_UserDelegate_SourceTime] ON [dbo].[Tbl_E_UserDelegate] 
(
	[SourceUserID] ASC,
	[BeginTime] ASC,
	[EndTime] ASC,
	[BStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'原处理人用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'SourceUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'代理处理人用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'AgentUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'委托类型；TODO、APPROVAL、NOTICE、ALL' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'DelegateType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'限定应用；为空表示不限应用' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'AppCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'限定事件；为空表示不限事件' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'EventCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'委托开始时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'BeginTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'委托结束时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'EndTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'委托原因' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'Reason'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户代理/委托表；用于请假、出差、临时代办等场景。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserDelegate'
GO
/****** Object:  Table [dbo].[Tbl_E_Users]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_Users', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_Users](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[LoginId] [nvarchar](50) NOT NULL,
	[RealName] [nvarchar](50) NOT NULL,
	[PwdHash] [nvarchar](200) NOT NULL,
	[PwdSalt] [nvarchar](100) NULL,
	[PasswordAlgo] [varchar](30) NOT NULL,
	[PasswordVersion] [int] NOT NULL,
	[UserType] [varchar](30) NOT NULL,
	[LoginCount] [int] NOT NULL,
	[MaxLoginCount] [int] NOT NULL,
	[PwdErrorCount] [int] NOT NULL,
	[MaxPwdErrorCount] [int] NOT NULL,
	[IsLocked] [bit] NOT NULL,
	[IsEnabled] [bit] NOT NULL,
	[LastLoginTime] [datetime] NULL,
	[LastPwdErrorTime] [datetime] NULL,
	[LastPwdChangedTime] [datetime] NULL,
	[ExternalRefType] [varchar](30) NULL,
	[ExternalRefID] [nvarchar](100) NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_Users] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_Users_Status' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Users'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_Users_Status] ON [dbo].[Tbl_E_Users] 
(
	[BStatus] ASC,
	[IsEnabled] ASC,
	[IsLocked] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_Users_LoginId' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Users'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_Users_LoginId] ON [dbo].[Tbl_E_Users] 
(
	[LoginId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'登录账号，唯一' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'LoginId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户显示姓名' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'RealName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'密码哈希值；兼容旧 MD5，建议逐步升级' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'PwdHash'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'密码盐，新算法使用' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'PwdSalt'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'密码算法；MD5_16、PBKDF2、BCrypt' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'PasswordAlgo'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'密码版本，用于渐进升级' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'PasswordVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户类型；EMPLOYEE、CUSTOMER、SUPPLIER、PARTNER' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'UserType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'累计登录次数' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'LoginCount'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最大允许登录次数，保留历史字段' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'MaxLoginCount'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'连续密码错误次数' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'PwdErrorCount'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最大允许错误次数' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'MaxPwdErrorCount'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否锁定' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'IsLocked'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否启用账号' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'IsEnabled'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后登录时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'LastLoginTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后密码错误时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'LastPwdErrorTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改密码时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'LastPwdChangedTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'外部身份类型；CUSTOMER、SUPPLIER、PARTNER 等' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'ExternalRefType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'外部身份标识，避免框架绑定具体业务表' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'ExternalRefID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户账号表；框架账号主体，支持历史 MD5 密码兼容与新算法渐进升级。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Users'
GO
/****** Object:  Table [dbo].[Tbl_E_EventConfig]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_EventConfig', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_EventConfig](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[AppCode] [varchar](50) NOT NULL,
	[EventCode] [varchar](100) NOT NULL,
	[EventName] [nvarchar](100) NOT NULL,
	[EventType] [varchar](50) NULL,
	[PageUrl] [nvarchar](300) NULL,
	[MenuGroupCode] [varchar](50) NULL,
	[ExecType] [varchar](20) NOT NULL,
	[IsGenerateTodo] [bit] NOT NULL,
	[TodoTitle] [nvarchar](200) NULL,
	[HandleMode] [varchar](30) NOT NULL,
	[DefaultDueMinutes] [int] NULL,
	[DispSeq] [int] NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_EventConfig] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_EventConfig_AppType' AND object_id = OBJECT_ID(N'dbo.Tbl_E_EventConfig'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_EventConfig_AppType] ON [dbo].[Tbl_E_EventConfig] 
(
	[AppCode] ASC,
	[EventType] ASC,
	[BStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_EventConfig_App_Event' AND object_id = OBJECT_ID(N'dbo.Tbl_E_EventConfig'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_EventConfig_App_Event] ON [dbo].[Tbl_E_EventConfig] 
(
	[AppCode] ASC,
	[EventCode] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'应用编码；如 FRAME、CRM' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'AppCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件编码，建议应用内唯一；如 CRM.QUOTE.SUBMIT' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'EventCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件名称' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'EventName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件分类；APPROVAL、NOTICE、TASK、SYSTEM' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'EventType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'默认跳转页面；优先使用事件实例 ObjectUrl' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'PageUrl'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'所属菜单组' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'MenuGroupCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'执行类型；SYNC、ASYNC、MANUAL' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'ExecType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否默认生成待办' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'IsGenerateTodo'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'待办标题模板' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'TodoTitle'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'处理模式；SINGLE、ALL、ANY、CLAIM' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'HandleMode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'默认处理时限，分钟' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'DefaultDueMinutes'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'排序' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'DispSeq'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件配置表；只描述通用事件，不理解业务表。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventConfig'
GO
/****** Object:  Table [dbo].[Tbl_E_DutyResourceAction]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_DutyResourceAction', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_DutyResourceAction](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[DutyID] [int] NOT NULL,
	[ResourceID] [varchar](100) NOT NULL,
	[ActionCode] [varchar](50) NOT NULL,
	[IsAllowed] [bit] NOT NULL,
	[ConditionExpr] [nvarchar](500) NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_DutyResourceAction] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_DutyResourceAction_Duty_Resource_Action' AND object_id = OBJECT_ID(N'dbo.Tbl_E_DutyResourceAction'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_DutyResourceAction_Duty_Resource_Action] ON [dbo].[Tbl_E_DutyResourceAction] 
(
	[DutyID] ASC,
	[ResourceID] ASC,
	[ActionCode] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'DutyID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'资源编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'ResourceID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'动作编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'ActionCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否允许' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'IsAllowed'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'权限条件表达式，基于上下文判断' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'ConditionExpr'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责资源动作权限表；用于表达审批、导出、领取、分配等非 CRUD 权限。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DutyResourceAction'
GO
/****** Object:  Table [dbo].[Tbl_E_Duty]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_Duty', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_Duty](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[DutyCode] [nvarchar](30) NOT NULL,
	[DutyCName] [nvarchar](100) NOT NULL,
	[DutyEName] [nvarchar](100) NULL,
	[DutyCategory] [varchar](50) NULL,
	[DutyDispSeq] [int] NOT NULL,
	[DDescription] [nvarchar](max) NULL,
	[DutyFlow] [nvarchar](max) NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_Duty] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_Duty_StatusSeq' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Duty'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_Duty_StatusSeq] ON [dbo].[Tbl_E_Duty] 
(
	[BStatus] ASC,
	[DutyDispSeq] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_Duty_DutyCode' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Duty'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_Duty_DutyCode] ON [dbo].[Tbl_E_Duty] 
(
	[DutyCode] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责编码，唯一' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'DutyCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责中文名称' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'DutyCName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责英文名称' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'DutyEName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责分类；APPROVAL、SALES、SERVICE、FINANCE' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'DutyCategory'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'显示顺序' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'DutyDispSeq'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责描述' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'DDescription'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责流程定义，保留扩展' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'DutyFlow'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责表；职责是岗位承担的业务责任，可关联资源权限和事件订阅。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Duty'
GO
/****** Object:  Table [dbo].[Tbl_E_DictType]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_DictType', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_DictType](
	[DictTypeCode] [varchar](50) NOT NULL,
	[DictTypeName] [nvarchar](100) NOT NULL,
	[AppCode] [varchar](50) NOT NULL,
	[IsSystem] [bit] NOT NULL,
	[IsEditable] [bit] NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[AmendDate] [datetime] NOT NULL,
 CONSTRAINT [PK_Tbl_E_DictType] PRIMARY KEY CLUSTERED 
(
	[DictTypeCode] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_DictType_App' AND object_id = OBJECT_ID(N'dbo.Tbl_E_DictType'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_DictType_App] ON [dbo].[Tbl_E_DictType] 
(
	[AppCode] ASC,
	[BStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Tbl_E_Department]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_Department', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_Department](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[DeptCode] [nvarchar](30) NOT NULL,
	[DeptCName] [nvarchar](100) NOT NULL,
	[DeptEName] [nvarchar](100) NULL,
	[ParentDeptID] [int] NULL,
	[DeptLevel] [int] NOT NULL,
	[DeptPath] [varchar](500) NULL,
	[DeptType] [varchar](30) NULL,
	[LeaderUserID] [int] NULL,
	[DispSeq] [int] NOT NULL,
	[DDescription] [nvarchar](max) NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_Department] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_Department_Parent' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Department'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_Department_Parent] ON [dbo].[Tbl_E_Department] 
(
	[ParentDeptID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_Department_Path' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Department'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_Department_Path] ON [dbo].[Tbl_E_Department] 
(
	[DeptPath] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_Department_StatusSeq' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Department'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_Department_StatusSeq] ON [dbo].[Tbl_E_Department] 
(
	[BStatus] ASC,
	[DispSeq] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_Department_DeptCode' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Department'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_Department_DeptCode] ON [dbo].[Tbl_E_Department] 
(
	[DeptCode] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'部门主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'部门编码，唯一' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'DeptCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'部门中文名称' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'DeptCName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'部门英文名称' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'DeptEName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'上级部门 ID，自关联' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'ParentDeptID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'部门层级，根节点为 1' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'DeptLevel'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'部门路径，如 /1/3/8/，便于查下级部门' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'DeptPath'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'部门类型；COMPANY、DEPT、TEAM、STORE' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'DeptType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'部门负责人用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'LeaderUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'显示顺序' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'DispSeq'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'部门描述' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'DDescription'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'部门表；支持部门树、部门路径、部门负责人。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Department'
GO
/****** Object:  Table [dbo].[Tbl_E_DataScopeRule]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_DataScopeRule', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_DataScopeRule](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[RuleCode] [varchar](100) NOT NULL,
	[RuleName] [nvarchar](100) NOT NULL,
	[SubjectType] [varchar](30) NOT NULL,
	[SubjectID] [int] NOT NULL,
	[AppCode] [varchar](50) NOT NULL,
	[ObjectType] [varchar](50) NOT NULL,
	[ScopeType] [varchar](30) NOT NULL,
	[ConditionExpr] [nvarchar](1000) NULL,
	[Priority] [int] NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_DataScopeRule] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_DataScopeRule_Subject' AND object_id = OBJECT_ID(N'dbo.Tbl_E_DataScopeRule'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_DataScopeRule_Subject] ON [dbo].[Tbl_E_DataScopeRule] 
(
	[SubjectType] ASC,
	[SubjectID] ASC,
	[AppCode] ASC,
	[ObjectType] ASC,
	[BStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_DataScopeRule_RuleCode' AND object_id = OBJECT_ID(N'dbo.Tbl_E_DataScopeRule'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_DataScopeRule_RuleCode] ON [dbo].[Tbl_E_DataScopeRule] 
(
	[RuleCode] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'规则编码，唯一' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'RuleCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'规则名称' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'RuleName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'授权主体类型；USER、POSITION、DUTY、DEPT' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'SubjectType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'授权主体 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'SubjectID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'应用编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'AppCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务对象类型，由业务模块定义' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'ObjectType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'范围类型；SELF、DEPT、DEPT_TREE、TEAM、ALL、CUSTOM' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'ScopeType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'自定义条件表达式' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'ConditionExpr'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'优先级，数值越小越优先' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'Priority'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'数据范围规则表；统一表达本人、本部门、下级部门、团队、公海、自定义条件等数据权限。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_DataScopeRule'
GO
/****** Object:  Table [dbo].[Tbl_E_AppModule]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_AppModule', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_AppModule](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[AppCode] [varchar](50) NOT NULL,
	[AppName] [nvarchar](100) NOT NULL,
	[AppType] [varchar](30) NOT NULL,
	[BaseUrl] [nvarchar](300) NULL,
	[Icon] [nvarchar](100) NULL,
	[DispSeq] [int] NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_AppModule] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_AppModule_StatusSeq' AND object_id = OBJECT_ID(N'dbo.Tbl_E_AppModule'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_AppModule_StatusSeq] ON [dbo].[Tbl_E_AppModule] 
(
	[BStatus] ASC,
	[DispSeq] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_AppModule_AppCode' AND object_id = OBJECT_ID(N'dbo.Tbl_E_AppModule'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_AppModule_AppCode] ON [dbo].[Tbl_E_AppModule] 
(
	[AppCode] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'应用编码，唯一；如 FRAME、CRM、OA' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'AppCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'应用名称' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'AppName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'应用类型；FRAMEWORK、BUSINESS、PLUGIN' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'AppType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'应用基础地址，用于跨模块跳转' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'BaseUrl'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'图标标识' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'Icon'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'显示顺序' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'DispSeq'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'应用模块注册表；用于注册接入 EFrame 的业务模块，框架不理解模块内部业务表。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_AppModule'
GO
/****** Object:  Table [dbo].[Tbl_E_EventInstance]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_EventInstance', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_EventInstance](
	[DataID] [bigint] IDENTITY(1,1) NOT NULL,
	[AppCode] [varchar](50) NOT NULL,
	[EventCode] [varchar](100) NOT NULL,
	[ObjectType] [varchar](50) NOT NULL,
	[ObjectKey] [nvarchar](100) NOT NULL,
	[ObjectCode] [nvarchar](100) NULL,
	[ObjectTitle] [nvarchar](300) NULL,
	[ObjectUrl] [nvarchar](500) NULL,
	[TriggerUserID] [int] NULL,
	[TriggerDeptID] [int] NULL,
	[TriggerPosID] [int] NULL,
	[PayloadJson] [nvarchar](max) NULL,
	[IdempotencyKey] [varchar](200) NULL,
	[EventStatus] [varchar](30) NOT NULL,
	[OccurredTime] [datetime] NOT NULL,
	[ProcessTime] [datetime] NULL,
	[LastError] [nvarchar](max) NULL,
	[RetryCount] [int] NOT NULL,
	[CreateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Tbl_E_EventInstance] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_EventInstance_AppEventTime' AND object_id = OBJECT_ID(N'dbo.Tbl_E_EventInstance'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_EventInstance_AppEventTime] ON [dbo].[Tbl_E_EventInstance] 
(
	[AppCode] ASC,
	[EventCode] ASC,
	[OccurredTime] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_EventInstance_Object' AND object_id = OBJECT_ID(N'dbo.Tbl_E_EventInstance'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_EventInstance_Object] ON [dbo].[Tbl_E_EventInstance] 
(
	[ObjectType] ASC,
	[ObjectKey] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_EventInstance_Status' AND object_id = OBJECT_ID(N'dbo.Tbl_E_EventInstance'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_EventInstance_Status] ON [dbo].[Tbl_E_EventInstance] 
(
	[EventStatus] ASC,
	[CreateTime] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_EventInstance_IdempotencyKey' AND object_id = OBJECT_ID(N'dbo.Tbl_E_EventInstance'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_EventInstance_IdempotencyKey] ON [dbo].[Tbl_E_EventInstance] 
(
	[IdempotencyKey] ASC
)
WHERE ([IdempotencyKey] IS NOT NULL)
WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件实例主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'应用编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'AppCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'EventCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务对象类型，由业务模块定义' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'ObjectType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务对象主键字符串' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'ObjectKey'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务对象编码或单号' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'ObjectCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务对象标题' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'ObjectTitle'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务对象跳转地址' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'ObjectUrl'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'触发事件的用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'TriggerUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'触发事件时的部门 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'TriggerDeptID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'触发事件时的岗位 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'TriggerPosID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件上下文 JSON' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'PayloadJson'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'幂等键，防止重复事件' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'IdempotencyKey'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件状态；NEW、PROCESSING、DONE、FAILED、CANCELLED' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'EventStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务事件发生时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'OccurredTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件处理时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'ProcessTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后错误信息' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'LastError'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'重试次数' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'RetryCount'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'写入时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance', @level2type=N'COLUMN',@level2name=N'CreateTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件实例表；记录某个应用发生了一次事件，是事件订阅架构核心表。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventInstance'
GO
/****** Object:  Table [dbo].[Tbl_E_EventFlowRule]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_EventFlowRule', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_EventFlowRule](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[RuleCode] [varchar](100) NOT NULL,
	[RuleName] [nvarchar](100) NOT NULL,
	[AppCode] [varchar](50) NOT NULL,
	[CurrentEvent] [varchar](100) NOT NULL,
	[NextEvent] [varchar](100) NULL,
	[ConditionExpr] [nvarchar](1000) NULL,
	[ActionType] [varchar](30) NOT NULL,
	[TargetResolveType] [varchar](50) NULL,
	[TargetDutyID] [int] NULL,
	[TargetPosID] [int] NULL,
	[TargetDeptID] [int] NULL,
	[TargetUserID] [int] NULL,
	[HandleMode] [varchar](30) NOT NULL,
	[TimeoutMinutes] [int] NULL,
	[EscalateEventCode] [varchar](100) NULL,
	[DispSeq] [int] NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_EventFlowRule] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_EventFlowRule_Current' AND object_id = OBJECT_ID(N'dbo.Tbl_E_EventFlowRule'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_EventFlowRule_Current] ON [dbo].[Tbl_E_EventFlowRule] 
(
	[AppCode] ASC,
	[CurrentEvent] ASC,
	[BStatus] ASC,
	[DispSeq] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_EventFlowRule_RuleCode' AND object_id = OBJECT_ID(N'dbo.Tbl_E_EventFlowRule'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_EventFlowRule_RuleCode] ON [dbo].[Tbl_E_EventFlowRule] 
(
	[RuleCode] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'规则编码，唯一' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'RuleCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'规则名称' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'RuleName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'应用编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'AppCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'当前事件编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'CurrentEvent'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'下一事件编码；可为空' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'NextEvent'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'条件表达式，基于事件信封和 PayloadJson' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'ConditionExpr'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'动作类型；CREATE_TODO、SEND_NOTICE、TRIGGER_EVENT、CALL_API' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'ActionType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'处理人解析方式；DUTY、POSITION、DEPT_MANAGER、OWNER_MANAGER、FIXED_USER' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'TargetResolveType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'目标职责 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'TargetDutyID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'目标岗位 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'TargetPosID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'目标部门 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'TargetDeptID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'固定目标用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'TargetUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'处理模式；SINGLE、ALL、ANY、CLAIM' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'HandleMode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'处理超时时限' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'TimeoutMinutes'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'超时升级事件编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'EscalateEventCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'排序' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'DispSeq'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件流转规则表；基于事件信封和 PayloadJson 判断，不引用业务表。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventFlowRule'
GO
/****** Object:  Table [dbo].[Tbl_E_ManagerSubordinate]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_ManagerSubordinate', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_ManagerSubordinate](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[ManagerUserID] [int] NOT NULL,
	[SubUserID] [int] NOT NULL,
	[ManagerPostID] [int] NULL,
	[SubPostID] [int] NULL,
	[DeptID] [int] NULL,
	[RelationType] [varchar](30) NOT NULL,
	[BeginDate] [date] NULL,
	[EndDate] [date] NULL,
	[DispSeq] [int] NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_ManagerSubordinate] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_ManagerSubordinate_Manager' AND object_id = OBJECT_ID(N'dbo.Tbl_E_ManagerSubordinate'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_ManagerSubordinate_Manager] ON [dbo].[Tbl_E_ManagerSubordinate] 
(
	[ManagerUserID] ASC,
	[BStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_ManagerSubordinate_Sub' AND object_id = OBJECT_ID(N'dbo.Tbl_E_ManagerSubordinate'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_ManagerSubordinate_Sub] ON [dbo].[Tbl_E_ManagerSubordinate] 
(
	[SubUserID] ASC,
	[BStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'上级用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'ManagerUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'下级用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'SubUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'上级岗位上下文' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'ManagerPostID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'下级岗位上下文' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'SubPostID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'关系所属部门' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'DeptID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'关系类型；DIRECT、MATRIX、TEMP' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'RelationType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'生效开始日期' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'BeginDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'生效结束日期' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'EndDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'显示顺序' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'DispSeq'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'上下级关系表；支持直接汇报、矩阵汇报、临时汇报。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ManagerSubordinate'
GO
/****** Object:  Table [dbo].[Tbl_E_LoginLog]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_LoginLog', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_LoginLog](
	[DataID] [bigint] IDENTITY(1,1) NOT NULL,
	[LoginId] [varchar](50) NOT NULL,
	[UserID] [int] NULL,
	[LoginStatus] [nvarchar](20) NOT NULL,
	[FailReason] [nvarchar](200) NULL,
	[IPAddress] [varchar](100) NULL,
	[UserAgent] [nvarchar](500) NULL,
	[LoginTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Tbl_E_LoginLog] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_LoginLog_Time' AND object_id = OBJECT_ID(N'dbo.Tbl_E_LoginLog'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_LoginLog_Time] ON [dbo].[Tbl_E_LoginLog] 
(
	[LoginTime] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_LoginLog_User' AND object_id = OBJECT_ID(N'dbo.Tbl_E_LoginLog'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_LoginLog_User] ON [dbo].[Tbl_E_LoginLog] 
(
	[UserID] ASC,
	[LoginTime] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_LoginLog', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'登录账号' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_LoginLog', @level2type=N'COLUMN',@level2name=N'LoginId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户 ID；登录失败时可为空' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_LoginLog', @level2type=N'COLUMN',@level2name=N'UserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'登录状态；成功/失败' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_LoginLog', @level2type=N'COLUMN',@level2name=N'LoginStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'失败原因' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_LoginLog', @level2type=N'COLUMN',@level2name=N'FailReason'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'IP 地址' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_LoginLog', @level2type=N'COLUMN',@level2name=N'IPAddress'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'浏览器或客户端信息' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_LoginLog', @level2type=N'COLUMN',@level2name=N'UserAgent'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'登录时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_LoginLog', @level2type=N'COLUMN',@level2name=N'LoginTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'登录日志表；记录成功和失败登录。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_LoginLog'
GO
/****** Object:  Table [dbo].[Tbl_E_HrLeaveRequest]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_HrLeaveRequest', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_HrLeaveRequest](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[RequestNo] [nvarchar](30) NOT NULL,
	[ApplicantUserID] [int] NOT NULL,
	[LeaveType] [nvarchar](20) NOT NULL,
	[StartDate] [date] NOT NULL,
	[EndDate] [date] NOT NULL,
	[Days] [decimal](5, 1) NOT NULL,
	[Reason] [nvarchar](500) NULL,
	[Status] [varchar](20) NOT NULL,
	[EventInstanceID] [bigint] NULL,
	[ApproveUserID] [int] NULL,
	[ApproveTime] [datetime] NULL,
	[ApproveRemark] [nvarchar](200) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[AmendDate] [datetime] NOT NULL,
	[Operator] [varchar](30) NULL,
 CONSTRAINT [PK_Tbl_E_HrLeaveRequest] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_HrLeaveRequest_Applicant' AND object_id = OBJECT_ID(N'dbo.Tbl_E_HrLeaveRequest'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_HrLeaveRequest_Applicant] ON [dbo].[Tbl_E_HrLeaveRequest] 
(
	[ApplicantUserID] ASC,
	[Status] ASC
)
WHERE ([IsDeleted]=(0))
WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_HrLeaveRequest_No' AND object_id = OBJECT_ID(N'dbo.Tbl_E_HrLeaveRequest'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_HrLeaveRequest_No] ON [dbo].[Tbl_E_HrLeaveRequest] 
(
	[RequestNo] ASC
)
WHERE ([IsDeleted]=(0))
WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Tbl_E_Position]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_Position', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_Position](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[PostCode] [nvarchar](30) NOT NULL,
	[PostCName] [nvarchar](100) NOT NULL,
	[PostEName] [nvarchar](100) NULL,
	[PositionType] [varchar](30) NULL,
	[DataScope] [varchar](30) NOT NULL,
	[DispSeq] [int] NOT NULL,
	[DDescription] [nvarchar](max) NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_Position] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_Position_StatusSeq' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Position'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_Position_StatusSeq] ON [dbo].[Tbl_E_Position] 
(
	[BStatus] ASC,
	[DispSeq] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_Position_PostCode' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Position'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_Position_PostCode] ON [dbo].[Tbl_E_Position] 
(
	[PostCode] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'岗位主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'岗位编码，唯一' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'PostCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'岗位中文名称' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'PostCName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'岗位英文名称' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'PostEName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'岗位类型；MANAGER、SALES、SERVICE、FINANCE' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'PositionType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'默认数据范围；SELF、DEPT、DEPT_TREE、ALL、CUSTOM' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'DataScope'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'显示顺序' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'DispSeq'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'岗位描述' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'DDescription'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'岗位表；岗位是职责、权限、事件订阅和数据范围的主要承载对象之一。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Position'
GO
/****** Object:  Table [dbo].[Tbl_E_OperationLog]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_OperationLog', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_OperationLog](
	[DataID] [bigint] IDENTITY(1,1) NOT NULL,
	[AppCode] [varchar](50) NOT NULL,
	[ObjectType] [varchar](50) NULL,
	[ObjectKey] [nvarchar](100) NULL,
	[ActionCode] [varchar](50) NOT NULL,
	[ActionName] [nvarchar](100) NOT NULL,
	[UserID] [int] NULL,
	[DeptID] [int] NULL,
	[PosID] [int] NULL,
	[BeforeJson] [nvarchar](max) NULL,
	[AfterJson] [nvarchar](max) NULL,
	[IPAddress] [varchar](100) NULL,
	[CreateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Tbl_E_OperationLog] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_OperationLog_AppObject' AND object_id = OBJECT_ID(N'dbo.Tbl_E_OperationLog'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_OperationLog_AppObject] ON [dbo].[Tbl_E_OperationLog] 
(
	[AppCode] ASC,
	[ObjectType] ASC,
	[ObjectKey] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_OperationLog_UserTime' AND object_id = OBJECT_ID(N'dbo.Tbl_E_OperationLog'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_OperationLog_UserTime] ON [dbo].[Tbl_E_OperationLog] 
(
	[UserID] ASC,
	[CreateTime] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_OperationLog', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'应用编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_OperationLog', @level2type=N'COLUMN',@level2name=N'AppCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作对象类型' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_OperationLog', @level2type=N'COLUMN',@level2name=N'ObjectType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作对象主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_OperationLog', @level2type=N'COLUMN',@level2name=N'ObjectKey'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作编码；CREATE、UPDATE、DELETE、APPROVE' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_OperationLog', @level2type=N'COLUMN',@level2name=N'ActionCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作名称' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_OperationLog', @level2type=N'COLUMN',@level2name=N'ActionName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_OperationLog', @level2type=N'COLUMN',@level2name=N'UserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作时部门 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_OperationLog', @level2type=N'COLUMN',@level2name=N'DeptID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作时岗位 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_OperationLog', @level2type=N'COLUMN',@level2name=N'PosID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作前数据摘要' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_OperationLog', @level2type=N'COLUMN',@level2name=N'BeforeJson'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作后数据摘要' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_OperationLog', @level2type=N'COLUMN',@level2name=N'AfterJson'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'IP 地址' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_OperationLog', @level2type=N'COLUMN',@level2name=N'IPAddress'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_OperationLog', @level2type=N'COLUMN',@level2name=N'CreateTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作日志表；按 AppCode/ObjectType/ObjectKey 记录通用操作审计。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_OperationLog'
GO
/****** Object:  Table [dbo].[Tbl_E_MenuGroup]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_MenuGroup', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_MenuGroup](
	[MenuGroupCode] [varchar](50) NOT NULL,
	[AppCode] [varchar](50) NOT NULL,
	[MenuGroupName] [nvarchar](100) NOT NULL,
	[Icon] [nvarchar](100) NULL,
	[DispSeq] [int] NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_MenuGroup] PRIMARY KEY CLUSTERED 
(
	[MenuGroupCode] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_MenuGroup_App_StatusSeq' AND object_id = OBJECT_ID(N'dbo.Tbl_E_MenuGroup'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_MenuGroup_App_StatusSeq] ON [dbo].[Tbl_E_MenuGroup] 
(
	[AppCode] ASC,
	[BStatus] ASC,
	[DispSeq] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'菜单组编码，主键；如 SYS、ORG、CRM' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup', @level2type=N'COLUMN',@level2name=N'MenuGroupCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'所属应用编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup', @level2type=N'COLUMN',@level2name=N'AppCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'菜单组名称' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup', @level2type=N'COLUMN',@level2name=N'MenuGroupName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'图标标识' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup', @level2type=N'COLUMN',@level2name=N'Icon'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'显示顺序' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup', @level2type=N'COLUMN',@level2name=N'DispSeq'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'菜单组表；按应用归属菜单组，主键为 MenuGroupCode。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_MenuGroup'
GO
/****** Object:  Table [dbo].[Tbl_E_ResourcePermission]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_ResourcePermission', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_ResourcePermission](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[DutyID] [int] NOT NULL,
	[ResourceID] [varchar](100) NOT NULL,
	[CanCreate] [bit] NOT NULL,
	[CanUpdate] [bit] NOT NULL,
	[CanDelete] [bit] NOT NULL,
	[CanQuery] [bit] NOT NULL,
	[CanExport] [bit] NOT NULL,
	[CanImport] [bit] NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_ResourcePermission] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_ResourcePermission_Duty_Resource' AND object_id = OBJECT_ID(N'dbo.Tbl_E_ResourcePermission'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_ResourcePermission_Duty_Resource] ON [dbo].[Tbl_E_ResourcePermission] 
(
	[DutyID] ASC,
	[ResourceID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'DutyID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'资源编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'ResourceID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否允许新增' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'CanCreate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否允许修改' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'CanUpdate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否允许删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'CanDelete'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否允许查看' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'CanQuery'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否允许导出' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'CanExport'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否允许导入' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'CanImport'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责资源权限表；保留 CRUD 权限兼容，复杂动作建议使用 Tbl_E_DutyResourceAction。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourcePermission'
GO
/****** Object:  Table [dbo].[Tbl_E_ResourceAction]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_ResourceAction', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_ResourceAction](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[ResourceID] [varchar](100) NOT NULL,
	[ActionCode] [varchar](50) NOT NULL,
	[ActionName] [nvarchar](100) NOT NULL,
	[DispSeq] [int] NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_ResourceAction] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_ResourceAction_Resource_Action' AND object_id = OBJECT_ID(N'dbo.Tbl_E_ResourceAction'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_ResourceAction_Resource_Action] ON [dbo].[Tbl_E_ResourceAction] 
(
	[ResourceID] ASC,
	[ActionCode] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'资源编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction', @level2type=N'COLUMN',@level2name=N'ResourceID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'动作编码；QUERY、CREATE、UPDATE、DELETE、APPROVE、EXPORT' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction', @level2type=N'COLUMN',@level2name=N'ActionCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'动作名称' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction', @level2type=N'COLUMN',@level2name=N'ActionName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'显示顺序' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction', @level2type=N'COLUMN',@level2name=N'DispSeq'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'资源动作表；表达查询、新增、审批、导出、领取、分配等动作权限。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_ResourceAction'
GO
/****** Object:  Table [dbo].[Tbl_E_Resource]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_Resource', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_Resource](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[AppCode] [varchar](50) NOT NULL,
	[ResourceID] [varchar](100) NOT NULL,
	[ResourceName] [nvarchar](100) NOT NULL,
	[ResourceType] [varchar](30) NULL,
	[MenuPath] [nvarchar](300) NULL,
	[MenuGroupCode] [varchar](50) NULL,
	[ParentResourceID] [varchar](100) NULL,
	[Icon] [nvarchar](100) NULL,
	[DispSeq] [int] NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_Resource] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_Resource_AppType' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Resource'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_Resource_AppType] ON [dbo].[Tbl_E_Resource] 
(
	[AppCode] ASC,
	[ResourceType] ASC,
	[BStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_Resource_MenuGroup' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Resource'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_Resource_MenuGroup] ON [dbo].[Tbl_E_Resource] 
(
	[MenuGroupCode] ASC,
	[BStatus] ASC,
	[DispSeq] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_Resource_ResourceID' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Resource'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_Resource_ResourceID] ON [dbo].[Tbl_E_Resource] 
(
	[ResourceID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'所属应用编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'AppCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'资源编码，唯一；如 RES.CRM.ACCOUNT' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'ResourceID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'资源名称' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'ResourceName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'资源类型；PAGE、GROUP、MENU、BUTTON、API' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'ResourceType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'页面或接口路径' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'MenuPath'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'所属菜单组' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'MenuGroupCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'上级资源编码，用于菜单树' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'ParentResourceID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'图标标识' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'Icon'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'显示顺序' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'DispSeq'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'功能资源表；用于页面、菜单、按钮、API 等资源注册。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Resource'
GO
/****** Object:  Table [dbo].[Tbl_E_TodoGroup]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_TodoGroup', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_TodoGroup](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[TenantID] [int] NOT NULL,
	[GroupCode] [varchar](100) NOT NULL,
	[EventInstanceID] [bigint] NULL,
	[AppCode] [varchar](50) NOT NULL,
	[EventCode] [varchar](100) NULL,
	[ObjectType] [varchar](50) NOT NULL,
	[ObjectKey] [nvarchar](100) NOT NULL,
	[HandleMode] [varchar](30) NOT NULL,
	[TotalCount] [int] NOT NULL,
	[DoneCount] [int] NOT NULL,
	[GroupStatus] [varchar](30) NOT NULL,
	[DueTime] [datetime] NULL,
	[EscalateUserID] [int] NULL,
	[Remark] [nvarchar](max) NULL,
	[CreateTime] [datetime] NOT NULL,
	[UpdateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Tbl_E_TodoGroup] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_TodoGroup_Instance' AND object_id = OBJECT_ID(N'dbo.Tbl_E_TodoGroup'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_TodoGroup_Instance] ON [dbo].[Tbl_E_TodoGroup] 
(
	[EventInstanceID] ASC
)
WHERE ([EventInstanceID] IS NOT NULL)
WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_TodoGroup_Object' AND object_id = OBJECT_ID(N'dbo.Tbl_E_TodoGroup'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_TodoGroup_Object] ON [dbo].[Tbl_E_TodoGroup] 
(
	[TenantID] ASC,
	[AppCode] ASC,
	[ObjectType] ASC,
	[ObjectKey] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_TodoGroup_Status' AND object_id = OBJECT_ID(N'dbo.Tbl_E_TodoGroup'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_TodoGroup_Status] ON [dbo].[Tbl_E_TodoGroup] 
(
	[TenantID] ASC,
	[GroupStatus] ASC,
	[CreateTime] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_TodoGroup_Code' AND object_id = OBJECT_ID(N'dbo.Tbl_E_TodoGroup'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_TodoGroup_Code] ON [dbo].[Tbl_E_TodoGroup] 
(
	[TenantID] ASC,
	[GroupCode] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Tbl_E_TodoCandidate]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_TodoCandidate', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_TodoCandidate](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[TenantID] [int] NOT NULL,
	[TodoGroupID] [int] NOT NULL,
	[CandidateUserID] [int] NOT NULL,
	[CandidateDeptID] [int] NULL,
	[CandidatePosID] [int] NULL,
	[CandidateDutyID] [int] NULL,
	[CandidateStatus] [varchar](30) NOT NULL,
	[ClaimTime] [datetime] NULL,
	[TodoTaskID] [int] NULL,
	[CreateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Tbl_E_TodoCandidate] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_TodoCandidate_Group' AND object_id = OBJECT_ID(N'dbo.Tbl_E_TodoCandidate'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_TodoCandidate_Group] ON [dbo].[Tbl_E_TodoCandidate] 
(
	[TodoGroupID] ASC,
	[CandidateStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_TodoCandidate_User' AND object_id = OBJECT_ID(N'dbo.Tbl_E_TodoCandidate'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_TodoCandidate_User] ON [dbo].[Tbl_E_TodoCandidate] 
(
	[TenantID] ASC,
	[CandidateUserID] ASC,
	[CandidateStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_TodoCandidate_Group_User' AND object_id = OBJECT_ID(N'dbo.Tbl_E_TodoCandidate'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_TodoCandidate_Group_User] ON [dbo].[Tbl_E_TodoCandidate] 
(
	[TodoGroupID] ASC,
	[CandidateUserID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Tbl_E_Subscription]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_Subscription', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_Subscription](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[DutyID] [int] NOT NULL,
	[AppCode] [varchar](50) NULL,
	[SubType] [varchar](20) NOT NULL,
	[EventCode] [varchar](100) NULL,
	[ResourceID] [varchar](100) NULL,
	[DeptID] [int] NULL,
	[IsPrimary] [bit] NOT NULL,
	[FunctionLimit] [varchar](50) NULL,
	[ConditionExpr] [nvarchar](500) NULL,
	[NotifyMode] [varchar](100) NULL,
	[DispSeq] [int] NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_Subscription] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_Subscription_Duty' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Subscription'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_Subscription_Duty] ON [dbo].[Tbl_E_Subscription] 
(
	[DutyID] ASC,
	[SubType] ASC,
	[BStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_Subscription_Event' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Subscription'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_Subscription_Event] ON [dbo].[Tbl_E_Subscription] 
(
	[AppCode] ASC,
	[EventCode] ASC,
	[BStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_Subscription_Resource' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Subscription'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_Subscription_Resource] ON [dbo].[Tbl_E_Subscription] 
(
	[ResourceID] ASC,
	[BStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'DutyID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'应用编码；事件订阅时建议填写' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'AppCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'订阅类型；RESOURCE、EVENT、MIXED' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'SubType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件编码；SubType=EVENT 时必填' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'EventCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'资源编码；SubType=RESOURCE 时必填' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'ResourceID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'部门限定；为空表示不限部门' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'DeptID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否主订阅' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'IsPrimary'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'兼容旧 6 位功能限制字符串' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'FunctionLimit'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'订阅条件表达式，基于事件上下文' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'ConditionExpr'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'通知方式；TODO、MESSAGE、EMAIL 等，可逗号分隔' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'NotifyMode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'排序' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'DispSeq'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责订阅表；职责订阅资源或事件，不包含任何业务表外键。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Subscription'
GO
/****** Object:  Table [dbo].[Tbl_E_PositionDuty]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_PositionDuty', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_PositionDuty](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[PosID] [int] NOT NULL,
	[DutyID] [int] NOT NULL,
	[DeptID] [int] NULL,
	[BusinessLimit] [varchar](100) NULL,
	[DispSeq] [int] NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_PositionDuty] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_PositionDuty_Duty' AND object_id = OBJECT_ID(N'dbo.Tbl_E_PositionDuty'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_PositionDuty_Duty] ON [dbo].[Tbl_E_PositionDuty] 
(
	[DutyID] ASC,
	[BStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_PositionDuty_Pos' AND object_id = OBJECT_ID(N'dbo.Tbl_E_PositionDuty'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_PositionDuty_Pos] ON [dbo].[Tbl_E_PositionDuty] 
(
	[PosID] ASC,
	[BStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'岗位 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'PosID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'职责 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'DutyID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'部门限定；为空表示所有部门适用' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'DeptID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务范围标记，兼容旧字段；复杂范围建议使用 Tbl_E_DataScopeRule' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'BusinessLimit'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'显示顺序' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'DispSeq'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'岗位职责表；表达某岗位在某部门范围内拥有某职责。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_PositionDuty'
GO
/****** Object:  Table [dbo].[Tbl_E_Member]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_Member', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_Member](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[MemberID] [nvarchar](50) NOT NULL,
	[MemberName] [nvarchar](50) NOT NULL,
	[UserID] [int] NULL,
	[Sex] [nvarchar](10) NULL,
	[Nation] [nvarchar](30) NULL,
	[NativePlace] [nvarchar](100) NULL,
	[IDNo] [nvarchar](50) NULL,
	[Birthday] [date] NULL,
	[ELevel] [nvarchar](50) NULL,
	[Degree] [nvarchar](50) NULL,
	[School] [nvarchar](100) NULL,
	[Speciality] [nvarchar](100) NULL,
	[ComputerAbility] [nvarchar](100) NULL,
	[WorkDate] [date] NULL,
	[PerGrade] [nvarchar](50) NULL,
	[DefaultDeptID] [int] NULL,
	[DefaultPosID] [int] NULL,
	[HomeAddr] [nvarchar](300) NULL,
	[Mobile] [nvarchar](50) NULL,
	[HomePhone] [nvarchar](50) NULL,
	[WorkPhone] [nvarchar](50) NULL,
	[WXNum] [nvarchar](100) NULL,
	[QQNum] [nvarchar](50) NULL,
	[EMail] [nvarchar](100) NULL,
	[Health] [nvarchar](100) NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_Member] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_Member_DefaultDeptPos' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Member'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_Member_DefaultDeptPos] ON [dbo].[Tbl_E_Member] 
(
	[DefaultDeptID] ASC,
	[DefaultPosID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_Member_MemberID' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Member'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_Member_MemberID] ON [dbo].[Tbl_E_Member] 
(
	[MemberID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_Member_UserID_NotNull' AND object_id = OBJECT_ID(N'dbo.Tbl_E_Member'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_Member_UserID_NotNull] ON [dbo].[Tbl_E_Member] 
(
	[UserID] ASC
)
WHERE ([UserID] IS NOT NULL)
WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'人员档案主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'工号，唯一' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'MemberID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'人员姓名' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'MemberName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'绑定的登录账号 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'UserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'性别' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'Sex'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'民族' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'Nation'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'籍贯' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'NativePlace'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'证件号码；建议加密或脱敏' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'IDNo'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'生日' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'Birthday'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'学历层次' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'ELevel'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'学位' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'Degree'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'毕业学校' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'School'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'专业' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'Speciality'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'计算机能力' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'ComputerAbility'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'参加工作日期' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'WorkDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'人员等级' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'PerGrade'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'默认部门 ID；真正任岗以 Tbl_E_UserPosition 为准' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'DefaultDeptID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'默认岗位 ID；真正任岗以 Tbl_E_UserPosition 为准' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'DefaultPosID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'家庭地址' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'HomeAddr'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'手机号；建议脱敏' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'Mobile'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'家庭电话' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'HomePhone'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'工作电话' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'WorkPhone'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'微信号' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'WXNum'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'QQ 号' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'QQNum'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'邮箱' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'EMail'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'健康状况' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'Health'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'人员档案表；用于员工自然人信息，UserID 明确绑定登录账号。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_Member'
GO
/****** Object:  Table [dbo].[Tbl_E_EventReceiver]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_EventReceiver', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_EventReceiver](
	[DataID] [bigint] IDENTITY(1,1) NOT NULL,
	[EventInstanceID] [bigint] NOT NULL,
	[ReceiverUserID] [int] NOT NULL,
	[ReceiverMemberID] [int] NULL,
	[ReceiverDeptID] [int] NULL,
	[ReceiverPosID] [int] NULL,
	[ReceiverDutyID] [int] NULL,
	[SubscriptionID] [int] NULL,
	[ResolveType] [varchar](50) NOT NULL,
	[ResolveReason] [nvarchar](500) NULL,
	[CreateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Tbl_E_EventReceiver] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_EventReceiver_Instance' AND object_id = OBJECT_ID(N'dbo.Tbl_E_EventReceiver'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_EventReceiver_Instance] ON [dbo].[Tbl_E_EventReceiver] 
(
	[EventInstanceID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_EventReceiver_User' AND object_id = OBJECT_ID(N'dbo.Tbl_E_EventReceiver'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_EventReceiver_User] ON [dbo].[Tbl_E_EventReceiver] 
(
	[ReceiverUserID] ASC,
	[CreateTime] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventReceiver', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件实例 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventReceiver', @level2type=N'COLUMN',@level2name=N'EventInstanceID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'接收人用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventReceiver', @level2type=N'COLUMN',@level2name=N'ReceiverUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'接收人人员档案 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventReceiver', @level2type=N'COLUMN',@level2name=N'ReceiverMemberID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'接收人部门快照' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventReceiver', @level2type=N'COLUMN',@level2name=N'ReceiverDeptID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'接收人岗位快照' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventReceiver', @level2type=N'COLUMN',@level2name=N'ReceiverPosID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'命中的职责 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventReceiver', @level2type=N'COLUMN',@level2name=N'ReceiverDutyID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'命中的订阅规则 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventReceiver', @level2type=N'COLUMN',@level2name=N'SubscriptionID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'解析方式；DUTY、POSITION、MANAGER、DELEGATE' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventReceiver', @level2type=N'COLUMN',@level2name=N'ResolveType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'命中原因说明' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventReceiver', @level2type=N'COLUMN',@level2name=N'ResolveReason'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventReceiver', @level2type=N'COLUMN',@level2name=N'CreateTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件接收人解析表；记录事件为什么分发给某个用户。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventReceiver'
GO
/****** Object:  Table [dbo].[Tbl_E_EventLog]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_EventLog', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_EventLog](
	[DataID] [bigint] IDENTITY(1,1) NOT NULL,
	[EventInstanceID] [bigint] NULL,
	[AppCode] [varchar](50) NOT NULL,
	[EventCode] [varchar](100) NOT NULL,
	[ObjectType] [varchar](50) NULL,
	[ObjectKey] [nvarchar](100) NULL,
	[UserID] [int] NULL,
	[ActionType] [varchar](50) NOT NULL,
	[HandleResult] [nvarchar](50) NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[CreateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Tbl_E_EventLog] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_EventLog_Event' AND object_id = OBJECT_ID(N'dbo.Tbl_E_EventLog'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_EventLog_Event] ON [dbo].[Tbl_E_EventLog] 
(
	[AppCode] ASC,
	[EventCode] ASC,
	[CreateTime] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_EventLog_Instance' AND object_id = OBJECT_ID(N'dbo.Tbl_E_EventLog'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_EventLog_Instance] ON [dbo].[Tbl_E_EventLog] 
(
	[EventInstanceID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventLog', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件实例 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventLog', @level2type=N'COLUMN',@level2name=N'EventInstanceID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'应用编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventLog', @level2type=N'COLUMN',@level2name=N'AppCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventLog', @level2type=N'COLUMN',@level2name=N'EventCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务对象类型' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventLog', @level2type=N'COLUMN',@level2name=N'ObjectType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务对象主键字符串' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventLog', @level2type=N'COLUMN',@level2name=N'ObjectKey'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventLog', @level2type=N'COLUMN',@level2name=N'UserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作类型；RAISE、RESOLVE、DELIVER、HANDLE' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventLog', @level2type=N'COLUMN',@level2name=N'ActionType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'处理结果；成功、失败、跳过' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventLog', @level2type=N'COLUMN',@level2name=N'HandleResult'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注或错误信息' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventLog', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventLog', @level2type=N'COLUMN',@level2name=N'CreateTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件处理日志表；记录事件发布、解析、投递、处理过程，不承担业务对象外键职责。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventLog'
GO
/****** Object:  Table [dbo].[Tbl_E_EventDelivery]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_EventDelivery', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_EventDelivery](
	[DataID] [bigint] IDENTITY(1,1) NOT NULL,
	[EventInstanceID] [bigint] NOT NULL,
	[ReceiverUserID] [int] NOT NULL,
	[Channel] [varchar](30) NOT NULL,
	[DeliveryStatus] [varchar](30) NOT NULL,
	[RetryCount] [int] NOT NULL,
	[LastError] [nvarchar](max) NULL,
	[SentTime] [datetime] NULL,
	[ReadTime] [datetime] NULL,
	[CreateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Tbl_E_EventDelivery] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_EventDelivery_Status' AND object_id = OBJECT_ID(N'dbo.Tbl_E_EventDelivery'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_EventDelivery_Status] ON [dbo].[Tbl_E_EventDelivery] 
(
	[DeliveryStatus] ASC,
	[CreateTime] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_EventDelivery_User' AND object_id = OBJECT_ID(N'dbo.Tbl_E_EventDelivery'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_EventDelivery_User] ON [dbo].[Tbl_E_EventDelivery] 
(
	[ReceiverUserID] ASC,
	[Channel] ASC,
	[CreateTime] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventDelivery', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件实例 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventDelivery', @level2type=N'COLUMN',@level2name=N'EventInstanceID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'接收人用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventDelivery', @level2type=N'COLUMN',@level2name=N'ReceiverUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'投递渠道；TODO、MESSAGE、EMAIL、WECHAT、SMS' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventDelivery', @level2type=N'COLUMN',@level2name=N'Channel'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'投递状态；PENDING、SENT、FAILED、READ' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventDelivery', @level2type=N'COLUMN',@level2name=N'DeliveryStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'重试次数' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventDelivery', @level2type=N'COLUMN',@level2name=N'RetryCount'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后错误信息' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventDelivery', @level2type=N'COLUMN',@level2name=N'LastError'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'发送时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventDelivery', @level2type=N'COLUMN',@level2name=N'SentTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'已读时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventDelivery', @level2type=N'COLUMN',@level2name=N'ReadTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventDelivery', @level2type=N'COLUMN',@level2name=N'CreateTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件投递表；记录待办、站内信、邮件、企业微信等投递状态。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_EventDelivery'
GO
/****** Object:  Table [dbo].[Tbl_E_DictItem]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_DictItem', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_DictItem](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[DictTypeCode] [varchar](50) NOT NULL,
	[ItemCode] [varchar](50) NOT NULL,
	[ItemName] [nvarchar](100) NOT NULL,
	[ItemNameEn] [nvarchar](100) NULL,
	[ParentItemCode] [varchar](50) NULL,
	[DispSeq] [int] NOT NULL,
	[ExtJson] [nvarchar](500) NULL,
	[IsSystem] [bit] NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[AmendDate] [datetime] NOT NULL,
 CONSTRAINT [PK_Tbl_E_DictItem] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_DictItem_Type_Seq' AND object_id = OBJECT_ID(N'dbo.Tbl_E_DictItem'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_DictItem_Type_Seq] ON [dbo].[Tbl_E_DictItem] 
(
	[DictTypeCode] ASC,
	[BStatus] ASC,
	[DispSeq] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_E_DictItem_Type_Code' AND object_id = OBJECT_ID(N'dbo.Tbl_E_DictItem'))
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Tbl_E_DictItem_Type_Code] ON [dbo].[Tbl_E_DictItem] 
(
	[DictTypeCode] ASC,
	[ItemCode] ASC
)
WHERE ([IsDeleted]=(0))
WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Tbl_E_UserPosition]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_UserPosition', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_UserPosition](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[MemberID] [int] NULL,
	[DeptID] [int] NOT NULL,
	[PosID] [int] NOT NULL,
	[IsPrimary] [bit] NOT NULL,
	[BeginDate] [date] NULL,
	[EndDate] [date] NULL,
	[Remark] [nvarchar](max) NULL,
	[BStatus] [varchar](20) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[CreateUserID] [int] NULL,
	[AmendDate] [datetime] NOT NULL,
	[AmendUserID] [int] NULL,
	[Operator] [varchar](30) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Tbl_E_UserPosition] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_UserPosition_DeptPos' AND object_id = OBJECT_ID(N'dbo.Tbl_E_UserPosition'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_UserPosition_DeptPos] ON [dbo].[Tbl_E_UserPosition] 
(
	[DeptID] ASC,
	[PosID] ASC,
	[BStatus] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_UserPosition_User' AND object_id = OBJECT_ID(N'dbo.Tbl_E_UserPosition'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_UserPosition_User] ON [dbo].[Tbl_E_UserPosition] 
(
	[UserID] ASC,
	[BStatus] ASC,
	[IsPrimary] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'UserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'人员档案 ID，逻辑关联 Tbl_E_Member.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'MemberID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'部门 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'DeptID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'岗位 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'PosID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否主岗位' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'IsPrimary'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'任岗开始日期' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'BeginDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'任岗结束日期；为空表示当前有效' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'EndDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务状态；新数据统一 1=启用、2=停用，服务层兼容旧值' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'BStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'软删除标记；0=未删除，1=已删除' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'IsDeleted'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'CreateDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'CreateUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'AmendDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改人用户 ID，逻辑关联 Tbl_E_Users.DataID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'AmendUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'操作员代号；兼容原 Operator 字段，长度由 8 建议扩展为 30' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'Operator'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server 行版本号，用于并发控制' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition', @level2type=N'COLUMN',@level2name=N'RowVersion'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户任岗表；支持一人多岗、主岗、兼岗、历史追溯。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_UserPosition'
GO
/****** Object:  Table [dbo].[Tbl_E_TodoTask]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_TodoTask', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_TodoTask](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[EventInstanceID] [bigint] NULL,
	[EventLogID] [bigint] NULL,
	[AppCode] [varchar](50) NOT NULL,
	[EventCode] [varchar](100) NULL,
	[UserID] [int] NOT NULL,
	[ObjectType] [varchar](50) NOT NULL,
	[ObjectKey] [nvarchar](100) NOT NULL,
	[ObjectCode] [nvarchar](100) NULL,
	[ObjectTitle] [nvarchar](300) NULL,
	[ObjectUrl] [nvarchar](500) NULL,
	[Status] [tinyint] NOT NULL,
	[TodoTitle] [nvarchar](200) NOT NULL,
	[TodoContent] [nvarchar](max) NULL,
	[HandlerDeptID] [int] NULL,
	[HandlerPosID] [int] NULL,
	[DueTime] [datetime] NULL,
	[Priority] [varchar](20) NOT NULL,
	[CreateTime] [datetime] NOT NULL,
	[HandleTime] [datetime] NULL,
	[TodoGroupID] [int] NULL,
	[OriginalUserID] [int] NULL,
	[IsDelegate] [bit] NOT NULL,
	[DelegateSourceUserID] [int] NULL,
	[IsCosign] [bit] NOT NULL,
	[CosignParentTaskID] [int] NULL,
	[ClaimTime] [datetime] NULL,
 CONSTRAINT [PK_Tbl_E_TodoTask] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_TodoTask_EventInstance' AND object_id = OBJECT_ID(N'dbo.Tbl_E_TodoTask'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_TodoTask_EventInstance] ON [dbo].[Tbl_E_TodoTask] 
(
	[EventInstanceID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_TodoTask_Group' AND object_id = OBJECT_ID(N'dbo.Tbl_E_TodoTask'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_TodoTask_Group] ON [dbo].[Tbl_E_TodoTask] 
(
	[TodoGroupID] ASC
)
WHERE ([TodoGroupID] IS NOT NULL)
WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_TodoTask_Object' AND object_id = OBJECT_ID(N'dbo.Tbl_E_TodoTask'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_TodoTask_Object] ON [dbo].[Tbl_E_TodoTask] 
(
	[AppCode] ASC,
	[ObjectType] ASC,
	[ObjectKey] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_TodoTask_UserStatus' AND object_id = OBJECT_ID(N'dbo.Tbl_E_TodoTask'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_TodoTask_UserStatus] ON [dbo].[Tbl_E_TodoTask] 
(
	[UserID] ASC,
	[Status] ASC,
	[CreateTime] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'待办主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'事件实例 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'EventInstanceID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'关联事件日志 ID；类型与 Tbl_E_EventLog.DataID 一致' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'EventLogID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'应用编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'AppCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'来源事件编码' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'EventCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'当前待办处理人用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'UserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务对象类型' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'ObjectType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务对象主键字符串' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'ObjectKey'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务对象编码或单号' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'ObjectCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'业务对象标题' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'ObjectTitle'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'点击待办时跳转地址' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'ObjectUrl'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'状态；0待处理、1已处理、2已关闭、3已转交、4已撤回' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'Status'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'待办标题' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'TodoTitle'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'待办内容' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'TodoContent'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'处理人部门快照' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'HandlerDeptID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'处理人岗位快照' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'HandlerPosID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'截止处理时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'DueTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'优先级；LOW、NORMAL、HIGH、URGENT' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'Priority'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'CreateTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'处理时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask', @level2type=N'COLUMN',@level2name=N'HandleTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'待办任务表；使用 AppCode/ObjectType/ObjectKey 表示业务对象，不引用业务表。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTask'
GO
/****** Object:  Table [dbo].[Tbl_E_TodoTaskLog]    Script Date: 06/18/2026 23:13:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF OBJECT_ID(N'dbo.Tbl_E_TodoTaskLog', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Tbl_E_TodoTaskLog](
	[DataID] [bigint] IDENTITY(1,1) NOT NULL,
	[TodoTaskID] [int] NOT NULL,
	[ActionType] [varchar](50) NOT NULL,
	[FromUserID] [int] NULL,
	[ToUserID] [int] NULL,
	[ActionResult] [varchar](50) NULL,
	[Remark] [nvarchar](max) NULL,
	[CreateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Tbl_E_TodoTaskLog] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_E_TodoTaskLog_Task' AND object_id = OBJECT_ID(N'dbo.Tbl_E_TodoTaskLog'))
BEGIN
CREATE NONCLUSTERED INDEX [IX_Tbl_E_TodoTaskLog_Task] ON [dbo].[Tbl_E_TodoTaskLog] 
(
	[TodoTaskID] ASC,
	[CreateTime] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
END
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTaskLog', @level2type=N'COLUMN',@level2name=N'DataID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'待办任务 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTaskLog', @level2type=N'COLUMN',@level2name=N'TodoTaskID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'动作类型；CREATE、CLAIM、TRANSFER、RETURN、FINISH、CLOSE、REVOKE、REMIND' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTaskLog', @level2type=N'COLUMN',@level2name=N'ActionType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'原处理人用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTaskLog', @level2type=N'COLUMN',@level2name=N'FromUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'新处理人用户 ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTaskLog', @level2type=N'COLUMN',@level2name=N'ToUserID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'动作结果' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTaskLog', @level2type=N'COLUMN',@level2name=N'ActionResult'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'处理意见或备注' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTaskLog', @level2type=N'COLUMN',@level2name=N'Remark'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTaskLog', @level2type=N'COLUMN',@level2name=N'CreateTime'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'待办处理日志表；记录创建、领取、转交、退回、完成、关闭、撤回、催办等动作。' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Tbl_E_TodoTaskLog'
GO
/****** Object:  Default [DF_Tbl_E_UserHandover_HandoverTime]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_UserHandover] ADD  CONSTRAINT [DF_Tbl_E_UserHandover_HandoverTime]  DEFAULT (getdate()) FOR [HandoverTime]
GO
/****** Object:  Default [DF_Tbl_E_UserDelegate_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_UserDelegate] ADD  CONSTRAINT [DF_Tbl_E_UserDelegate_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_UserDelegate_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_UserDelegate] ADD  CONSTRAINT [DF_Tbl_E_UserDelegate_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_UserDelegate_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_UserDelegate] ADD  CONSTRAINT [DF_Tbl_E_UserDelegate_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_UserDelegate_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_UserDelegate] ADD  CONSTRAINT [DF_Tbl_E_UserDelegate_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_Users_PasswordAlgo]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Users] ADD  CONSTRAINT [DF_Tbl_E_Users_PasswordAlgo]  DEFAULT ('MD5_16') FOR [PasswordAlgo]
GO
/****** Object:  Default [DF_Tbl_E_Users_PasswordVersion]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Users] ADD  CONSTRAINT [DF_Tbl_E_Users_PasswordVersion]  DEFAULT ((1)) FOR [PasswordVersion]
GO
/****** Object:  Default [DF_Tbl_E_Users_UserType]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Users] ADD  CONSTRAINT [DF_Tbl_E_Users_UserType]  DEFAULT ('EMPLOYEE') FOR [UserType]
GO
/****** Object:  Default [DF_Tbl_E_Users_LoginCount]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Users] ADD  CONSTRAINT [DF_Tbl_E_Users_LoginCount]  DEFAULT ((0)) FOR [LoginCount]
GO
/****** Object:  Default [DF_Tbl_E_Users_MaxLoginCount]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Users] ADD  CONSTRAINT [DF_Tbl_E_Users_MaxLoginCount]  DEFAULT ((99999)) FOR [MaxLoginCount]
GO
/****** Object:  Default [DF_Tbl_E_Users_PwdErrorCount]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Users] ADD  CONSTRAINT [DF_Tbl_E_Users_PwdErrorCount]  DEFAULT ((0)) FOR [PwdErrorCount]
GO
/****** Object:  Default [DF_Tbl_E_Users_MaxPwdErrorCount]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Users] ADD  CONSTRAINT [DF_Tbl_E_Users_MaxPwdErrorCount]  DEFAULT ((5)) FOR [MaxPwdErrorCount]
GO
/****** Object:  Default [DF_Tbl_E_Users_IsLocked]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Users] ADD  CONSTRAINT [DF_Tbl_E_Users_IsLocked]  DEFAULT ((0)) FOR [IsLocked]
GO
/****** Object:  Default [DF_Tbl_E_Users_IsEnabled]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Users] ADD  CONSTRAINT [DF_Tbl_E_Users_IsEnabled]  DEFAULT ((1)) FOR [IsEnabled]
GO
/****** Object:  Default [DF_Tbl_E_Users_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Users] ADD  CONSTRAINT [DF_Tbl_E_Users_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_Users_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Users] ADD  CONSTRAINT [DF_Tbl_E_Users_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_Users_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Users] ADD  CONSTRAINT [DF_Tbl_E_Users_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_Users_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Users] ADD  CONSTRAINT [DF_Tbl_E_Users_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_EventConfig_ExecType]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventConfig] ADD  CONSTRAINT [DF_Tbl_E_EventConfig_ExecType]  DEFAULT ('ASYNC') FOR [ExecType]
GO
/****** Object:  Default [DF_Tbl_E_EventConfig_IsGenerateTodo]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventConfig] ADD  CONSTRAINT [DF_Tbl_E_EventConfig_IsGenerateTodo]  DEFAULT ((1)) FOR [IsGenerateTodo]
GO
/****** Object:  Default [DF_Tbl_E_EventConfig_HandleMode]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventConfig] ADD  CONSTRAINT [DF_Tbl_E_EventConfig_HandleMode]  DEFAULT ('SINGLE') FOR [HandleMode]
GO
/****** Object:  Default [DF_Tbl_E_EventConfig_DispSeq]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventConfig] ADD  CONSTRAINT [DF_Tbl_E_EventConfig_DispSeq]  DEFAULT ((99)) FOR [DispSeq]
GO
/****** Object:  Default [DF_Tbl_E_EventConfig_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventConfig] ADD  CONSTRAINT [DF_Tbl_E_EventConfig_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_EventConfig_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventConfig] ADD  CONSTRAINT [DF_Tbl_E_EventConfig_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_EventConfig_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventConfig] ADD  CONSTRAINT [DF_Tbl_E_EventConfig_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_EventConfig_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventConfig] ADD  CONSTRAINT [DF_Tbl_E_EventConfig_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_DutyResourceAction_IsAllowed]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DutyResourceAction] ADD  CONSTRAINT [DF_Tbl_E_DutyResourceAction_IsAllowed]  DEFAULT ((1)) FOR [IsAllowed]
GO
/****** Object:  Default [DF_Tbl_E_DutyResourceAction_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DutyResourceAction] ADD  CONSTRAINT [DF_Tbl_E_DutyResourceAction_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_DutyResourceAction_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DutyResourceAction] ADD  CONSTRAINT [DF_Tbl_E_DutyResourceAction_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_DutyResourceAction_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DutyResourceAction] ADD  CONSTRAINT [DF_Tbl_E_DutyResourceAction_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_DutyResourceAction_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DutyResourceAction] ADD  CONSTRAINT [DF_Tbl_E_DutyResourceAction_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_Duty_DutyDispSeq]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Duty] ADD  CONSTRAINT [DF_Tbl_E_Duty_DutyDispSeq]  DEFAULT ((99)) FOR [DutyDispSeq]
GO
/****** Object:  Default [DF_Tbl_E_Duty_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Duty] ADD  CONSTRAINT [DF_Tbl_E_Duty_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_Duty_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Duty] ADD  CONSTRAINT [DF_Tbl_E_Duty_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_Duty_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Duty] ADD  CONSTRAINT [DF_Tbl_E_Duty_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_Duty_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Duty] ADD  CONSTRAINT [DF_Tbl_E_Duty_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_DictType_AppCode]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DictType] ADD  CONSTRAINT [DF_Tbl_E_DictType_AppCode]  DEFAULT ('FRAME') FOR [AppCode]
GO
/****** Object:  Default [DF_Tbl_E_DictType_IsSystem]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DictType] ADD  CONSTRAINT [DF_Tbl_E_DictType_IsSystem]  DEFAULT ((0)) FOR [IsSystem]
GO
/****** Object:  Default [DF_Tbl_E_DictType_IsEditable]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DictType] ADD  CONSTRAINT [DF_Tbl_E_DictType_IsEditable]  DEFAULT ((1)) FOR [IsEditable]
GO
/****** Object:  Default [DF_Tbl_E_DictType_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DictType] ADD  CONSTRAINT [DF_Tbl_E_DictType_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_DictType_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DictType] ADD  CONSTRAINT [DF_Tbl_E_DictType_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_DictType_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DictType] ADD  CONSTRAINT [DF_Tbl_E_DictType_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_DictType_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DictType] ADD  CONSTRAINT [DF_Tbl_E_DictType_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_Department_DeptLevel]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Department] ADD  CONSTRAINT [DF_Tbl_E_Department_DeptLevel]  DEFAULT ((1)) FOR [DeptLevel]
GO
/****** Object:  Default [DF_Tbl_E_Department_DispSeq]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Department] ADD  CONSTRAINT [DF_Tbl_E_Department_DispSeq]  DEFAULT ((99)) FOR [DispSeq]
GO
/****** Object:  Default [DF_Tbl_E_Department_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Department] ADD  CONSTRAINT [DF_Tbl_E_Department_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_Department_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Department] ADD  CONSTRAINT [DF_Tbl_E_Department_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_Department_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Department] ADD  CONSTRAINT [DF_Tbl_E_Department_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_Department_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Department] ADD  CONSTRAINT [DF_Tbl_E_Department_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_DataScopeRule_Priority]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DataScopeRule] ADD  CONSTRAINT [DF_Tbl_E_DataScopeRule_Priority]  DEFAULT ((99)) FOR [Priority]
GO
/****** Object:  Default [DF_Tbl_E_DataScopeRule_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DataScopeRule] ADD  CONSTRAINT [DF_Tbl_E_DataScopeRule_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_DataScopeRule_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DataScopeRule] ADD  CONSTRAINT [DF_Tbl_E_DataScopeRule_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_DataScopeRule_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DataScopeRule] ADD  CONSTRAINT [DF_Tbl_E_DataScopeRule_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_DataScopeRule_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DataScopeRule] ADD  CONSTRAINT [DF_Tbl_E_DataScopeRule_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_AppModule_AppType]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_AppModule] ADD  CONSTRAINT [DF_Tbl_E_AppModule_AppType]  DEFAULT ('BUSINESS') FOR [AppType]
GO
/****** Object:  Default [DF_Tbl_E_AppModule_DispSeq]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_AppModule] ADD  CONSTRAINT [DF_Tbl_E_AppModule_DispSeq]  DEFAULT ((99)) FOR [DispSeq]
GO
/****** Object:  Default [DF_Tbl_E_AppModule_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_AppModule] ADD  CONSTRAINT [DF_Tbl_E_AppModule_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_AppModule_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_AppModule] ADD  CONSTRAINT [DF_Tbl_E_AppModule_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_AppModule_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_AppModule] ADD  CONSTRAINT [DF_Tbl_E_AppModule_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_AppModule_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_AppModule] ADD  CONSTRAINT [DF_Tbl_E_AppModule_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_EventInstance_EventStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventInstance] ADD  CONSTRAINT [DF_Tbl_E_EventInstance_EventStatus]  DEFAULT ('NEW') FOR [EventStatus]
GO
/****** Object:  Default [DF_Tbl_E_EventInstance_RetryCount]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventInstance] ADD  CONSTRAINT [DF_Tbl_E_EventInstance_RetryCount]  DEFAULT ((0)) FOR [RetryCount]
GO
/****** Object:  Default [DF_Tbl_E_EventInstance_CreateTime]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventInstance] ADD  CONSTRAINT [DF_Tbl_E_EventInstance_CreateTime]  DEFAULT (getdate()) FOR [CreateTime]
GO
/****** Object:  Default [DF_Tbl_E_EventFlowRule_HandleMode]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventFlowRule] ADD  CONSTRAINT [DF_Tbl_E_EventFlowRule_HandleMode]  DEFAULT ('SINGLE') FOR [HandleMode]
GO
/****** Object:  Default [DF_Tbl_E_EventFlowRule_DispSeq]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventFlowRule] ADD  CONSTRAINT [DF_Tbl_E_EventFlowRule_DispSeq]  DEFAULT ((99)) FOR [DispSeq]
GO
/****** Object:  Default [DF_Tbl_E_EventFlowRule_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventFlowRule] ADD  CONSTRAINT [DF_Tbl_E_EventFlowRule_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_EventFlowRule_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventFlowRule] ADD  CONSTRAINT [DF_Tbl_E_EventFlowRule_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_EventFlowRule_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventFlowRule] ADD  CONSTRAINT [DF_Tbl_E_EventFlowRule_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_EventFlowRule_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventFlowRule] ADD  CONSTRAINT [DF_Tbl_E_EventFlowRule_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_ManagerSubordinate_RelationType]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ManagerSubordinate] ADD  CONSTRAINT [DF_Tbl_E_ManagerSubordinate_RelationType]  DEFAULT ('DIRECT') FOR [RelationType]
GO
/****** Object:  Default [DF_Tbl_E_ManagerSubordinate_DispSeq]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ManagerSubordinate] ADD  CONSTRAINT [DF_Tbl_E_ManagerSubordinate_DispSeq]  DEFAULT ((99)) FOR [DispSeq]
GO
/****** Object:  Default [DF_Tbl_E_ManagerSubordinate_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ManagerSubordinate] ADD  CONSTRAINT [DF_Tbl_E_ManagerSubordinate_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_ManagerSubordinate_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ManagerSubordinate] ADD  CONSTRAINT [DF_Tbl_E_ManagerSubordinate_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_ManagerSubordinate_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ManagerSubordinate] ADD  CONSTRAINT [DF_Tbl_E_ManagerSubordinate_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_ManagerSubordinate_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ManagerSubordinate] ADD  CONSTRAINT [DF_Tbl_E_ManagerSubordinate_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_LoginLog_LoginTime]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_LoginLog] ADD  CONSTRAINT [DF_Tbl_E_LoginLog_LoginTime]  DEFAULT (getdate()) FOR [LoginTime]
GO
/****** Object:  Default [DF_Tbl_E_HrLeaveRequest_Status]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_HrLeaveRequest] ADD  CONSTRAINT [DF_Tbl_E_HrLeaveRequest_Status]  DEFAULT ('PENDING') FOR [Status]
GO
/****** Object:  Default [DF_Tbl_E_HrLeaveRequest_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_HrLeaveRequest] ADD  CONSTRAINT [DF_Tbl_E_HrLeaveRequest_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_HrLeaveRequest_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_HrLeaveRequest] ADD  CONSTRAINT [DF_Tbl_E_HrLeaveRequest_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_HrLeaveRequest_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_HrLeaveRequest] ADD  CONSTRAINT [DF_Tbl_E_HrLeaveRequest_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_HrLeaveRequest_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_HrLeaveRequest] ADD  CONSTRAINT [DF_Tbl_E_HrLeaveRequest_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_Position_DataScope]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Position] ADD  CONSTRAINT [DF_Tbl_E_Position_DataScope]  DEFAULT ('SELF') FOR [DataScope]
GO
/****** Object:  Default [DF_Tbl_E_Position_DispSeq]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Position] ADD  CONSTRAINT [DF_Tbl_E_Position_DispSeq]  DEFAULT ((99)) FOR [DispSeq]
GO
/****** Object:  Default [DF_Tbl_E_Position_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Position] ADD  CONSTRAINT [DF_Tbl_E_Position_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_Position_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Position] ADD  CONSTRAINT [DF_Tbl_E_Position_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_Position_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Position] ADD  CONSTRAINT [DF_Tbl_E_Position_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_Position_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Position] ADD  CONSTRAINT [DF_Tbl_E_Position_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_OperationLog_CreateTime]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_OperationLog] ADD  CONSTRAINT [DF_Tbl_E_OperationLog_CreateTime]  DEFAULT (getdate()) FOR [CreateTime]
GO
/****** Object:  Default [DF_Tbl_E_MenuGroup_AppCode]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_MenuGroup] ADD  CONSTRAINT [DF_Tbl_E_MenuGroup_AppCode]  DEFAULT ('FRAME') FOR [AppCode]
GO
/****** Object:  Default [DF_Tbl_E_MenuGroup_DispSeq]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_MenuGroup] ADD  CONSTRAINT [DF_Tbl_E_MenuGroup_DispSeq]  DEFAULT ((99)) FOR [DispSeq]
GO
/****** Object:  Default [DF_Tbl_E_MenuGroup_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_MenuGroup] ADD  CONSTRAINT [DF_Tbl_E_MenuGroup_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_MenuGroup_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_MenuGroup] ADD  CONSTRAINT [DF_Tbl_E_MenuGroup_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_MenuGroup_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_MenuGroup] ADD  CONSTRAINT [DF_Tbl_E_MenuGroup_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_MenuGroup_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_MenuGroup] ADD  CONSTRAINT [DF_Tbl_E_MenuGroup_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_ResourcePermission_CanCreate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourcePermission] ADD  CONSTRAINT [DF_Tbl_E_ResourcePermission_CanCreate]  DEFAULT ((0)) FOR [CanCreate]
GO
/****** Object:  Default [DF_Tbl_E_ResourcePermission_CanUpdate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourcePermission] ADD  CONSTRAINT [DF_Tbl_E_ResourcePermission_CanUpdate]  DEFAULT ((0)) FOR [CanUpdate]
GO
/****** Object:  Default [DF_Tbl_E_ResourcePermission_CanDelete]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourcePermission] ADD  CONSTRAINT [DF_Tbl_E_ResourcePermission_CanDelete]  DEFAULT ((0)) FOR [CanDelete]
GO
/****** Object:  Default [DF_Tbl_E_ResourcePermission_CanQuery]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourcePermission] ADD  CONSTRAINT [DF_Tbl_E_ResourcePermission_CanQuery]  DEFAULT ((1)) FOR [CanQuery]
GO
/****** Object:  Default [DF_Tbl_E_ResourcePermission_CanExport]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourcePermission] ADD  CONSTRAINT [DF_Tbl_E_ResourcePermission_CanExport]  DEFAULT ((0)) FOR [CanExport]
GO
/****** Object:  Default [DF_Tbl_E_ResourcePermission_CanImport]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourcePermission] ADD  CONSTRAINT [DF_Tbl_E_ResourcePermission_CanImport]  DEFAULT ((0)) FOR [CanImport]
GO
/****** Object:  Default [DF_Tbl_E_ResourcePermission_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourcePermission] ADD  CONSTRAINT [DF_Tbl_E_ResourcePermission_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_ResourcePermission_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourcePermission] ADD  CONSTRAINT [DF_Tbl_E_ResourcePermission_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_ResourcePermission_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourcePermission] ADD  CONSTRAINT [DF_Tbl_E_ResourcePermission_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_ResourcePermission_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourcePermission] ADD  CONSTRAINT [DF_Tbl_E_ResourcePermission_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_ResourceAction_DispSeq]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourceAction] ADD  CONSTRAINT [DF_Tbl_E_ResourceAction_DispSeq]  DEFAULT ((99)) FOR [DispSeq]
GO
/****** Object:  Default [DF_Tbl_E_ResourceAction_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourceAction] ADD  CONSTRAINT [DF_Tbl_E_ResourceAction_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_ResourceAction_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourceAction] ADD  CONSTRAINT [DF_Tbl_E_ResourceAction_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_ResourceAction_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourceAction] ADD  CONSTRAINT [DF_Tbl_E_ResourceAction_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_ResourceAction_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_ResourceAction] ADD  CONSTRAINT [DF_Tbl_E_ResourceAction_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_Resource_DispSeq]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Resource] ADD  CONSTRAINT [DF_Tbl_E_Resource_DispSeq]  DEFAULT ((99)) FOR [DispSeq]
GO
/****** Object:  Default [DF_Tbl_E_Resource_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Resource] ADD  CONSTRAINT [DF_Tbl_E_Resource_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_Resource_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Resource] ADD  CONSTRAINT [DF_Tbl_E_Resource_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_Resource_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Resource] ADD  CONSTRAINT [DF_Tbl_E_Resource_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_Resource_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Resource] ADD  CONSTRAINT [DF_Tbl_E_Resource_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_TodoGroup_TenantID]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoGroup] ADD  CONSTRAINT [DF_Tbl_E_TodoGroup_TenantID]  DEFAULT ((0)) FOR [TenantID]
GO
/****** Object:  Default [DF_Tbl_E_TodoGroup_HandleMode]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoGroup] ADD  CONSTRAINT [DF_Tbl_E_TodoGroup_HandleMode]  DEFAULT ('SINGLE') FOR [HandleMode]
GO
/****** Object:  Default [DF_Tbl_E_TodoGroup_TotalCount]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoGroup] ADD  CONSTRAINT [DF_Tbl_E_TodoGroup_TotalCount]  DEFAULT ((0)) FOR [TotalCount]
GO
/****** Object:  Default [DF_Tbl_E_TodoGroup_DoneCount]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoGroup] ADD  CONSTRAINT [DF_Tbl_E_TodoGroup_DoneCount]  DEFAULT ((0)) FOR [DoneCount]
GO
/****** Object:  Default [DF_Tbl_E_TodoGroup_GroupStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoGroup] ADD  CONSTRAINT [DF_Tbl_E_TodoGroup_GroupStatus]  DEFAULT ('PENDING') FOR [GroupStatus]
GO
/****** Object:  Default [DF_Tbl_E_TodoGroup_CreateTime]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoGroup] ADD  CONSTRAINT [DF_Tbl_E_TodoGroup_CreateTime]  DEFAULT (getdate()) FOR [CreateTime]
GO
/****** Object:  Default [DF_Tbl_E_TodoGroup_UpdateTime]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoGroup] ADD  CONSTRAINT [DF_Tbl_E_TodoGroup_UpdateTime]  DEFAULT (getdate()) FOR [UpdateTime]
GO
/****** Object:  Default [DF_Tbl_E_TodoCandidate_TenantID]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoCandidate] ADD  CONSTRAINT [DF_Tbl_E_TodoCandidate_TenantID]  DEFAULT ((0)) FOR [TenantID]
GO
/****** Object:  Default [DF_Tbl_E_TodoCandidate_Status]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoCandidate] ADD  CONSTRAINT [DF_Tbl_E_TodoCandidate_Status]  DEFAULT ('WAITING') FOR [CandidateStatus]
GO
/****** Object:  Default [DF_Tbl_E_TodoCandidate_CreateTime]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoCandidate] ADD  CONSTRAINT [DF_Tbl_E_TodoCandidate_CreateTime]  DEFAULT (getdate()) FOR [CreateTime]
GO
/****** Object:  Default [DF_Tbl_E_Subscription_IsPrimary]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Subscription] ADD  CONSTRAINT [DF_Tbl_E_Subscription_IsPrimary]  DEFAULT ((1)) FOR [IsPrimary]
GO
/****** Object:  Default [DF_Tbl_E_Subscription_DispSeq]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Subscription] ADD  CONSTRAINT [DF_Tbl_E_Subscription_DispSeq]  DEFAULT ((99)) FOR [DispSeq]
GO
/****** Object:  Default [DF_Tbl_E_Subscription_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Subscription] ADD  CONSTRAINT [DF_Tbl_E_Subscription_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_Subscription_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Subscription] ADD  CONSTRAINT [DF_Tbl_E_Subscription_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_Subscription_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Subscription] ADD  CONSTRAINT [DF_Tbl_E_Subscription_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_Subscription_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Subscription] ADD  CONSTRAINT [DF_Tbl_E_Subscription_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_PositionDuty_DispSeq]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_PositionDuty] ADD  CONSTRAINT [DF_Tbl_E_PositionDuty_DispSeq]  DEFAULT ((99)) FOR [DispSeq]
GO
/****** Object:  Default [DF_Tbl_E_PositionDuty_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_PositionDuty] ADD  CONSTRAINT [DF_Tbl_E_PositionDuty_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_PositionDuty_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_PositionDuty] ADD  CONSTRAINT [DF_Tbl_E_PositionDuty_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_PositionDuty_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_PositionDuty] ADD  CONSTRAINT [DF_Tbl_E_PositionDuty_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_PositionDuty_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_PositionDuty] ADD  CONSTRAINT [DF_Tbl_E_PositionDuty_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_Member_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Member] ADD  CONSTRAINT [DF_Tbl_E_Member_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_Member_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Member] ADD  CONSTRAINT [DF_Tbl_E_Member_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_Member_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Member] ADD  CONSTRAINT [DF_Tbl_E_Member_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_Member_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_Member] ADD  CONSTRAINT [DF_Tbl_E_Member_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_EventReceiver_CreateTime]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventReceiver] ADD  CONSTRAINT [DF_Tbl_E_EventReceiver_CreateTime]  DEFAULT (getdate()) FOR [CreateTime]
GO
/****** Object:  Default [DF_Tbl_E_EventLog_CreateTime]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventLog] ADD  CONSTRAINT [DF_Tbl_E_EventLog_CreateTime]  DEFAULT (getdate()) FOR [CreateTime]
GO
/****** Object:  Default [DF_Tbl_E_EventDelivery_DeliveryStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventDelivery] ADD  CONSTRAINT [DF_Tbl_E_EventDelivery_DeliveryStatus]  DEFAULT ('PENDING') FOR [DeliveryStatus]
GO
/****** Object:  Default [DF_Tbl_E_EventDelivery_RetryCount]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventDelivery] ADD  CONSTRAINT [DF_Tbl_E_EventDelivery_RetryCount]  DEFAULT ((0)) FOR [RetryCount]
GO
/****** Object:  Default [DF_Tbl_E_EventDelivery_CreateTime]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_EventDelivery] ADD  CONSTRAINT [DF_Tbl_E_EventDelivery_CreateTime]  DEFAULT (getdate()) FOR [CreateTime]
GO
/****** Object:  Default [DF_Tbl_E_DictItem_DispSeq]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DictItem] ADD  CONSTRAINT [DF_Tbl_E_DictItem_DispSeq]  DEFAULT ((99)) FOR [DispSeq]
GO
/****** Object:  Default [DF_Tbl_E_DictItem_IsSystem]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DictItem] ADD  CONSTRAINT [DF_Tbl_E_DictItem_IsSystem]  DEFAULT ((0)) FOR [IsSystem]
GO
/****** Object:  Default [DF_Tbl_E_DictItem_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DictItem] ADD  CONSTRAINT [DF_Tbl_E_DictItem_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_DictItem_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DictItem] ADD  CONSTRAINT [DF_Tbl_E_DictItem_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_DictItem_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DictItem] ADD  CONSTRAINT [DF_Tbl_E_DictItem_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_DictItem_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_DictItem] ADD  CONSTRAINT [DF_Tbl_E_DictItem_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_UserPosition_IsPrimary]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_UserPosition] ADD  CONSTRAINT [DF_Tbl_E_UserPosition_IsPrimary]  DEFAULT ((0)) FOR [IsPrimary]
GO
/****** Object:  Default [DF_Tbl_E_UserPosition_BStatus]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_UserPosition] ADD  CONSTRAINT [DF_Tbl_E_UserPosition_BStatus]  DEFAULT ('1') FOR [BStatus]
GO
/****** Object:  Default [DF_Tbl_E_UserPosition_IsDeleted]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_UserPosition] ADD  CONSTRAINT [DF_Tbl_E_UserPosition_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
/****** Object:  Default [DF_Tbl_E_UserPosition_CreateDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_UserPosition] ADD  CONSTRAINT [DF_Tbl_E_UserPosition_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO
/****** Object:  Default [DF_Tbl_E_UserPosition_AmendDate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_UserPosition] ADD  CONSTRAINT [DF_Tbl_E_UserPosition_AmendDate]  DEFAULT (getdate()) FOR [AmendDate]
GO
/****** Object:  Default [DF_Tbl_E_TodoTask_Status]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoTask] ADD  CONSTRAINT [DF_Tbl_E_TodoTask_Status]  DEFAULT ((0)) FOR [Status]
GO
/****** Object:  Default [DF_Tbl_E_TodoTask_Priority]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoTask] ADD  CONSTRAINT [DF_Tbl_E_TodoTask_Priority]  DEFAULT ('NORMAL') FOR [Priority]
GO
/****** Object:  Default [DF_Tbl_E_TodoTask_CreateTime]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoTask] ADD  CONSTRAINT [DF_Tbl_E_TodoTask_CreateTime]  DEFAULT (getdate()) FOR [CreateTime]
GO
/****** Object:  Default [DF_Tbl_E_TodoTask_IsDelegate]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoTask] ADD  CONSTRAINT [DF_Tbl_E_TodoTask_IsDelegate]  DEFAULT ((0)) FOR [IsDelegate]
GO
/****** Object:  Default [DF_Tbl_E_TodoTask_IsCosign]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoTask] ADD  CONSTRAINT [DF_Tbl_E_TodoTask_IsCosign]  DEFAULT ((0)) FOR [IsCosign]
GO
/****** Object:  Default [DF_Tbl_E_TodoTaskLog_CreateTime]    Script Date: 06/18/2026 23:13:11 ******/
ALTER TABLE [dbo].[Tbl_E_TodoTaskLog] ADD  CONSTRAINT [DF_Tbl_E_TodoTaskLog_CreateTime]  DEFAULT (getdate()) FOR [CreateTime]
GO
/****** Object:  ForeignKey [FK_Tbl_E_Department_Parent]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_Department_Parent' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_Department'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_Department]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_Department_Parent] FOREIGN KEY([ParentDeptID])
REFERENCES [dbo].[Tbl_E_Department] ([DataID])
END
GO
ALTER TABLE [dbo].[Tbl_E_Department] CHECK CONSTRAINT [FK_Tbl_E_Department_Parent]
GO
/****** Object:  ForeignKey [FK_Tbl_E_TodoCandidate_Group]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_TodoCandidate_Group' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_TodoCandidate'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_TodoCandidate]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_TodoCandidate_Group] FOREIGN KEY([TodoGroupID])
REFERENCES [dbo].[Tbl_E_TodoGroup] ([DataID])
END
GO
ALTER TABLE [dbo].[Tbl_E_TodoCandidate] CHECK CONSTRAINT [FK_Tbl_E_TodoCandidate_Group]
GO
/****** Object:  ForeignKey [FK_Tbl_E_Subscription_Duty]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_Subscription_Duty' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_Subscription'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_Subscription]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_Subscription_Duty] FOREIGN KEY([DutyID])
REFERENCES [dbo].[Tbl_E_Duty] ([DataID])
END
GO
ALTER TABLE [dbo].[Tbl_E_Subscription] CHECK CONSTRAINT [FK_Tbl_E_Subscription_Duty]
GO
/****** Object:  ForeignKey [FK_Tbl_E_PositionDuty_Duty]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_PositionDuty_Duty' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_PositionDuty'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_PositionDuty]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_PositionDuty_Duty] FOREIGN KEY([DutyID])
REFERENCES [dbo].[Tbl_E_Duty] ([DataID])
END
GO
ALTER TABLE [dbo].[Tbl_E_PositionDuty] CHECK CONSTRAINT [FK_Tbl_E_PositionDuty_Duty]
GO
/****** Object:  ForeignKey [FK_Tbl_E_PositionDuty_Pos]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_PositionDuty_Pos' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_PositionDuty'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_PositionDuty]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_PositionDuty_Pos] FOREIGN KEY([PosID])
REFERENCES [dbo].[Tbl_E_Position] ([DataID])
END
GO
ALTER TABLE [dbo].[Tbl_E_PositionDuty] CHECK CONSTRAINT [FK_Tbl_E_PositionDuty_Pos]
GO
/****** Object:  ForeignKey [FK_Tbl_E_Member_User]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_Member_User' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_Member'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_Member]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_Member_User] FOREIGN KEY([UserID])
REFERENCES [dbo].[Tbl_E_Users] ([DataID])
END
GO
ALTER TABLE [dbo].[Tbl_E_Member] CHECK CONSTRAINT [FK_Tbl_E_Member_User]
GO
/****** Object:  ForeignKey [FK_Tbl_E_EventReceiver_Instance]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_EventReceiver_Instance' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_EventReceiver'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_EventReceiver]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_EventReceiver_Instance] FOREIGN KEY([EventInstanceID])
REFERENCES [dbo].[Tbl_E_EventInstance] ([DataID])
END
GO
ALTER TABLE [dbo].[Tbl_E_EventReceiver] CHECK CONSTRAINT [FK_Tbl_E_EventReceiver_Instance]
GO
/****** Object:  ForeignKey [FK_Tbl_E_EventLog_Instance]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_EventLog_Instance' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_EventLog'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_EventLog]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_EventLog_Instance] FOREIGN KEY([EventInstanceID])
REFERENCES [dbo].[Tbl_E_EventInstance] ([DataID])
END
GO
ALTER TABLE [dbo].[Tbl_E_EventLog] CHECK CONSTRAINT [FK_Tbl_E_EventLog_Instance]
GO
/****** Object:  ForeignKey [FK_Tbl_E_EventDelivery_Instance]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_EventDelivery_Instance' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_EventDelivery'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_EventDelivery]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_EventDelivery_Instance] FOREIGN KEY([EventInstanceID])
REFERENCES [dbo].[Tbl_E_EventInstance] ([DataID])
END
GO
ALTER TABLE [dbo].[Tbl_E_EventDelivery] CHECK CONSTRAINT [FK_Tbl_E_EventDelivery_Instance]
GO
/****** Object:  ForeignKey [FK_Tbl_E_DictItem_Type]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_DictItem_Type' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_DictItem'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_DictItem]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_DictItem_Type] FOREIGN KEY([DictTypeCode])
REFERENCES [dbo].[Tbl_E_DictType] ([DictTypeCode])
END
GO
ALTER TABLE [dbo].[Tbl_E_DictItem] CHECK CONSTRAINT [FK_Tbl_E_DictItem_Type]
GO
/****** Object:  ForeignKey [FK_Tbl_E_UserPosition_Dept]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_UserPosition_Dept' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_UserPosition'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_UserPosition]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_UserPosition_Dept] FOREIGN KEY([DeptID])
REFERENCES [dbo].[Tbl_E_Department] ([DataID])
END
GO
ALTER TABLE [dbo].[Tbl_E_UserPosition] CHECK CONSTRAINT [FK_Tbl_E_UserPosition_Dept]
GO
/****** Object:  ForeignKey [FK_Tbl_E_UserPosition_Pos]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_UserPosition_Pos' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_UserPosition'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_UserPosition]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_UserPosition_Pos] FOREIGN KEY([PosID])
REFERENCES [dbo].[Tbl_E_Position] ([DataID])
END
GO
ALTER TABLE [dbo].[Tbl_E_UserPosition] CHECK CONSTRAINT [FK_Tbl_E_UserPosition_Pos]
GO
/****** Object:  ForeignKey [FK_Tbl_E_UserPosition_User]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_UserPosition_User' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_UserPosition'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_UserPosition]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_UserPosition_User] FOREIGN KEY([UserID])
REFERENCES [dbo].[Tbl_E_Users] ([DataID])
END
GO
ALTER TABLE [dbo].[Tbl_E_UserPosition] CHECK CONSTRAINT [FK_Tbl_E_UserPosition_User]
GO
/****** Object:  ForeignKey [FK_Tbl_E_TodoTask_Instance]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_TodoTask_Instance' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_TodoTask'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_TodoTask]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_TodoTask_Instance] FOREIGN KEY([EventInstanceID])
REFERENCES [dbo].[Tbl_E_EventInstance] ([DataID])
END
GO
ALTER TABLE [dbo].[Tbl_E_TodoTask] CHECK CONSTRAINT [FK_Tbl_E_TodoTask_Instance]
GO
/****** Object:  ForeignKey [FK_Tbl_E_TodoTaskLog_Task]    Script Date: 06/18/2026 23:13:11 ******/
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'FK_Tbl_E_TodoTaskLog_Task' AND parent_object_id = OBJECT_ID(N'dbo.Tbl_E_TodoTaskLog'))
BEGIN
ALTER TABLE [dbo].[Tbl_E_TodoTaskLog]  WITH CHECK ADD  CONSTRAINT [FK_Tbl_E_TodoTaskLog_Task] FOREIGN KEY([TodoTaskID])
REFERENCES [dbo].[Tbl_E_TodoTask] ([DataID])
END
GO
ALTER TABLE [dbo].[Tbl_E_TodoTaskLog] CHECK CONSTRAINT [FK_Tbl_E_TodoTaskLog_Task]
GO

USE [FamilyTree];
GO

/* ---------- 脚本执行台账 ----------
   仓库原先没有任何迁移机制：文件名是唯一的顺序依据，而编号已经在碰撞
   （20-Seed_Foundation / 20-Seed_README 同号），也没有办法问一个数据库「你跑过哪些脚本」。
   这段自建表 + 记录，幂等，可在任意脚本单独执行。 */
IF OBJECT_ID(N'dbo.SchemaScriptLog', N'U') IS NULL
    CREATE TABLE dbo.SchemaScriptLog (
        ScriptName   NVARCHAR(200) NOT NULL,
        AppliedAt    DATETIME      NOT NULL CONSTRAINT DF_SchemaScriptLog_AppliedAt DEFAULT (GETDATE()),
        AppliedBy    NVARCHAR(128) NOT NULL CONSTRAINT DF_SchemaScriptLog_AppliedBy DEFAULT (SUSER_SNAME()),
        RunCount     INT           NOT NULL CONSTRAINT DF_SchemaScriptLog_RunCount DEFAULT (1),
        CONSTRAINT PK_SchemaScriptLog PRIMARY KEY CLUSTERED (ScriptName)
    );
GO
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'10-EFrame.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'10-EFrame.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'10-EFrame.sql');
GO
