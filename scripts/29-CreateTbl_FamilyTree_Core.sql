/*
================================================================================
  29-CreateTbl_FamilyTree_Core.sql
  族谱核心业务表建表脚本（幂等，可重复执行）

  背景：这 10 张表原先只有 docs/04-数据结构.sql 一份定义，而 docs/ 被 .gitignore
        整目录忽略——任何人 clone 之后都建不出库，19 处运行时提示指向的那个文件
        根本不在仓库里。本脚本按 EF 实体（source/Models/Ft*.cs）重建定义并纳入编号序列。

  执行顺序：10 → 11 → 20~27 → 29（本脚本） → 30~50
  执行方式：sqlcmd -S <server> -d <database> -i 29-CreateTbl_FamilyTree_Core.sql

  含：
    FamilyTree_Person / _PersonDraft / _PersonLink / _PersonMarry
    FamilyTree_ApiToken / _AccountBind / _OpLog
    FamilyTree_BranchAdminApply / _MatchConflict / _GenerationWord
  不含（另有脚本）：
    _Clan / _UserClan（43）、_ClanCreateInvite（47）、_PeerInvite / _PeerBridge（35）、_MatchExclude（40）

  说明：FamilyTree_Person.BranchId 是历史遗留的裸列，没有对应实体也没有 FamilyTree_Branch 表，
        保留为可空 INT 以兼容既有数据。
================================================================================
*/
USE [FamilyTree];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- FamilyTree_AccountBind
IF OBJECT_ID(N'dbo.FamilyTree_AccountBind', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_AccountBind (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL CONSTRAINT [DF_FamilyTree_AccountBind_UserId] DEFAULT (0),
        [WechatOpenId] NVARCHAR(64) NULL,
        [IdCardHash] NVARCHAR(64) NULL,
        [Mobile] NVARCHAR(32) NULL,
        [BStatus] NVARCHAR(1) NOT NULL CONSTRAINT [DF_FamilyTree_AccountBind_BStatus] DEFAULT (N'1'),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_FamilyTree_AccountBind_IsDeleted] DEFAULT (0),
        [Remark] NVARCHAR(512) NULL,
        [CreateDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_AccountBind_CreateDate] DEFAULT (GETDATE()),
        [AmendDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_AccountBind_AmendDate] DEFAULT (GETDATE()),
        [Operator] NVARCHAR(30) NOT NULL CONSTRAINT [DF_FamilyTree_AccountBind_Operator] DEFAULT (N''),
        CONSTRAINT [PK_FamilyTree_AccountBind] PRIMARY KEY CLUSTERED ([DataID] ASC)
    );
END
GO

-- FamilyTree_ApiToken
IF OBJECT_ID(N'dbo.FamilyTree_ApiToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_ApiToken (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL CONSTRAINT [DF_FamilyTree_ApiToken_UserId] DEFAULT (0),
        [TokenHash] NVARCHAR(64) NOT NULL CONSTRAINT [DF_FamilyTree_ApiToken_TokenHash] DEFAULT (N''),
        [ExpireDate] DATETIME NOT NULL,
        [BStatus] NVARCHAR(1) NOT NULL CONSTRAINT [DF_FamilyTree_ApiToken_BStatus] DEFAULT (N'1'),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_FamilyTree_ApiToken_IsDeleted] DEFAULT (0),
        [Remark] NVARCHAR(512) NULL,
        [CreateDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_ApiToken_CreateDate] DEFAULT (GETDATE()),
        [AmendDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_ApiToken_AmendDate] DEFAULT (GETDATE()),
        [Operator] NVARCHAR(30) NOT NULL CONSTRAINT [DF_FamilyTree_ApiToken_Operator] DEFAULT (N''),
        CONSTRAINT [PK_FamilyTree_ApiToken] PRIMARY KEY CLUSTERED ([DataID] ASC)
    );
END
GO

-- FamilyTree_BranchAdminApply
IF OBJECT_ID(N'dbo.FamilyTree_BranchAdminApply', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_BranchAdminApply (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [ApplyUserId] INT NOT NULL CONSTRAINT [DF_FamilyTree_BranchAdminApply_ApplyUserId] DEFAULT (0),
        [ApplyReason] NVARCHAR(512) NULL,
        [ApplyStatus] NVARCHAR(16) NOT NULL CONSTRAINT [DF_FamilyTree_BranchAdminApply_ApplyStatus] DEFAULT (N'PENDING'),
        [AuditUserId] INT NULL,
        [BStatus] NVARCHAR(1) NOT NULL CONSTRAINT [DF_FamilyTree_BranchAdminApply_BStatus] DEFAULT (N'1'),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_FamilyTree_BranchAdminApply_IsDeleted] DEFAULT (0),
        [Remark] NVARCHAR(512) NULL,
        [CreateDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_BranchAdminApply_CreateDate] DEFAULT (GETDATE()),
        [AmendDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_BranchAdminApply_AmendDate] DEFAULT (GETDATE()),
        [Operator] NVARCHAR(30) NOT NULL CONSTRAINT [DF_FamilyTree_BranchAdminApply_Operator] DEFAULT (N''),
        CONSTRAINT [PK_FamilyTree_BranchAdminApply] PRIMARY KEY CLUSTERED ([DataID] ASC)
    );
END
GO

-- FamilyTree_GenerationWord
IF OBJECT_ID(N'dbo.FamilyTree_GenerationWord', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_GenerationWord (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [SeqNo] INT NOT NULL CONSTRAINT [DF_FamilyTree_GenerationWord_SeqNo] DEFAULT (0),
        [Word] NVARCHAR(16) NOT NULL CONSTRAINT [DF_FamilyTree_GenerationWord_Word] DEFAULT (N''),
        [BStatus] NVARCHAR(1) NOT NULL CONSTRAINT [DF_FamilyTree_GenerationWord_BStatus] DEFAULT (N'1'),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_FamilyTree_GenerationWord_IsDeleted] DEFAULT (0),
        [Remark] NVARCHAR(512) NULL,
        [CreateDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_GenerationWord_CreateDate] DEFAULT (GETDATE()),
        [AmendDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_GenerationWord_AmendDate] DEFAULT (GETDATE()),
        [Operator] NVARCHAR(30) NOT NULL CONSTRAINT [DF_FamilyTree_GenerationWord_Operator] DEFAULT (N''),
        CONSTRAINT [PK_FamilyTree_GenerationWord] PRIMARY KEY CLUSTERED ([DataID] ASC)
    );
END
GO

-- FamilyTree_MatchConflict
IF OBJECT_ID(N'dbo.FamilyTree_MatchConflict', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_MatchConflict (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [SourcePersonId] INT NULL,
        [TargetPersonId] INT NULL,
        [ConflictType] NVARCHAR(32) NOT NULL CONSTRAINT [DF_FamilyTree_MatchConflict_ConflictType] DEFAULT (N''),
        [ConflictDetail] NVARCHAR(MAX) NULL,
        [ResolveStatus] NVARCHAR(16) NOT NULL CONSTRAINT [DF_FamilyTree_MatchConflict_ResolveStatus] DEFAULT (N'OPEN'),
        [ResolveUserId] INT NULL,
        [BStatus] NVARCHAR(1) NOT NULL CONSTRAINT [DF_FamilyTree_MatchConflict_BStatus] DEFAULT (N'1'),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_FamilyTree_MatchConflict_IsDeleted] DEFAULT (0),
        [Remark] NVARCHAR(512) NULL,
        [CreateDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_MatchConflict_CreateDate] DEFAULT (GETDATE()),
        [AmendDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_MatchConflict_AmendDate] DEFAULT (GETDATE()),
        [Operator] NVARCHAR(30) NOT NULL CONSTRAINT [DF_FamilyTree_MatchConflict_Operator] DEFAULT (N''),
        CONSTRAINT [PK_FamilyTree_MatchConflict] PRIMARY KEY CLUSTERED ([DataID] ASC)
    );
END
GO

-- FamilyTree_OpLog
IF OBJECT_ID(N'dbo.FamilyTree_OpLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_OpLog (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [OpType] NVARCHAR(32) NOT NULL CONSTRAINT [DF_FamilyTree_OpLog_OpType] DEFAULT (N''),
        [ObjectType] NVARCHAR(32) NOT NULL CONSTRAINT [DF_FamilyTree_OpLog_ObjectType] DEFAULT (N''),
        [ObjectKey] NVARCHAR(64) NOT NULL CONSTRAINT [DF_FamilyTree_OpLog_ObjectKey] DEFAULT (N''),
        [BeforeJson] NVARCHAR(MAX) NULL,
        [AfterJson] NVARCHAR(MAX) NULL,
        [OpUserId] INT NOT NULL CONSTRAINT [DF_FamilyTree_OpLog_OpUserId] DEFAULT (0),
        [BStatus] NVARCHAR(1) NOT NULL CONSTRAINT [DF_FamilyTree_OpLog_BStatus] DEFAULT (N'1'),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_FamilyTree_OpLog_IsDeleted] DEFAULT (0),
        [Remark] NVARCHAR(512) NULL,
        [CreateDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_OpLog_CreateDate] DEFAULT (GETDATE()),
        [AmendDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_OpLog_AmendDate] DEFAULT (GETDATE()),
        [Operator] NVARCHAR(30) NOT NULL CONSTRAINT [DF_FamilyTree_OpLog_Operator] DEFAULT (N''),
        CONSTRAINT [PK_FamilyTree_OpLog] PRIMARY KEY CLUSTERED ([DataID] ASC)
    );
END
GO

-- FamilyTree_Person
IF OBJECT_ID(N'dbo.FamilyTree_Person', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_Person (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [BranchId] INT NULL,
        [ClanId] INT NULL,
        [FullName] NVARCHAR(64) NOT NULL CONSTRAINT [DF_FamilyTree_Person_FullName] DEFAULT (N''),
        [FatherName] NVARCHAR(64) NOT NULL CONSTRAINT [DF_FamilyTree_Person_FatherName] DEFAULT (N''),
        [MotherName] NVARCHAR(64) NULL,
        [BirthDate] NVARCHAR(32) NULL,
        [BirthYear] INT NULL,
        [FatherPersonId] INT NULL,
        [MotherPersonId] INT NULL,
        [InMainGenealogy] BIT NOT NULL CONSTRAINT [DF_FamilyTree_Person_InMainGenealogy] DEFAULT (0),
        [SameAsPersonId] INT NULL,
        [DeathInfo] NVARCHAR(128) NULL,
        [NickName] NVARCHAR(128) NULL,
        [SelfIntro] NVARCHAR(MAX) NULL,
        [WechatId] NVARCHAR(128) NULL,
        [PhotoListJson] NVARCHAR(MAX) NULL,
        [Gender] TINYINT NOT NULL CONSTRAINT [DF_FamilyTree_Person_Gender] DEFAULT (1),
        [GenerationNo] INT NOT NULL CONSTRAINT [DF_FamilyTree_Person_GenerationNo] DEFAULT (0),
        [WordOfGeneration] NVARCHAR(32) NULL,
        [OwnerUserId] INT NOT NULL CONSTRAINT [DF_FamilyTree_Person_OwnerUserId] DEFAULT (0),
        [BindUserId] INT NULL,
        [EditLock] BIT NOT NULL CONSTRAINT [DF_FamilyTree_Person_EditLock] DEFAULT (0),
        [LinkLock] BIT NOT NULL CONSTRAINT [DF_FamilyTree_Person_LinkLock] DEFAULT (0),
        [PrivacyLevel] TINYINT NOT NULL CONSTRAINT [DF_FamilyTree_Person_PrivacyLevel] DEFAULT (1),
        [ShowPhoto] BIT NOT NULL CONSTRAINT [DF_FamilyTree_Person_ShowPhoto] DEFAULT (0),
        [ShowWechat] BIT NOT NULL CONSTRAINT [DF_FamilyTree_Person_ShowWechat] DEFAULT (0),
        [ShowSelfIntro] BIT NOT NULL CONSTRAINT [DF_FamilyTree_Person_ShowSelfIntro] DEFAULT (0),
        [ShowBirthDetail] BIT NOT NULL CONSTRAINT [DF_FamilyTree_Person_ShowBirthDetail] DEFAULT (0),
        [ShowResume] BIT NOT NULL CONSTRAINT [DF_FamilyTree_Person_ShowResume] DEFAULT (0),
        [PrintAllow] BIT NOT NULL CONSTRAINT [DF_FamilyTree_Person_PrintAllow] DEFAULT (1),
        [IsDead] BIT NOT NULL CONSTRAINT [DF_FamilyTree_Person_IsDead] DEFAULT (0),
        [IsCertified] BIT NOT NULL CONSTRAINT [DF_FamilyTree_Person_IsCertified] DEFAULT (0),
        [CertCode] NVARCHAR(16) NULL,
        [AuditStatus] NVARCHAR(16) NOT NULL CONSTRAINT [DF_FamilyTree_Person_AuditStatus] DEFAULT (N'PASS'),
        [KeyLocked] BIT NOT NULL CONSTRAINT [DF_FamilyTree_Person_KeyLocked] DEFAULT (0),
        [BStatus] NVARCHAR(1) NOT NULL CONSTRAINT [DF_FamilyTree_Person_BStatus] DEFAULT (N'1'),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_FamilyTree_Person_IsDeleted] DEFAULT (0),
        [Remark] NVARCHAR(512) NULL,
        [CreateDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_Person_CreateDate] DEFAULT (GETDATE()),
        [AmendDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_Person_AmendDate] DEFAULT (GETDATE()),
        [Operator] NVARCHAR(30) NOT NULL CONSTRAINT [DF_FamilyTree_Person_Operator] DEFAULT (N''),
        CONSTRAINT [PK_FamilyTree_Person] PRIMARY KEY CLUSTERED ([DataID] ASC)
    );
END
GO

-- FamilyTree_PersonDraft
IF OBJECT_ID(N'dbo.FamilyTree_PersonDraft', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_PersonDraft (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [BranchId] INT NULL,
        [SubmitUserId] INT NOT NULL CONSTRAINT [DF_FamilyTree_PersonDraft_SubmitUserId] DEFAULT (0),
        [FullName] NVARCHAR(64) NOT NULL CONSTRAINT [DF_FamilyTree_PersonDraft_FullName] DEFAULT (N''),
        [FatherName] NVARCHAR(64) NOT NULL CONSTRAINT [DF_FamilyTree_PersonDraft_FatherName] DEFAULT (N''),
        [MotherName] NVARCHAR(64) NULL,
        [BirthDate] NVARCHAR(32) NULL,
        [RelationType] NVARCHAR(16) NOT NULL CONSTRAINT [DF_FamilyTree_PersonDraft_RelationType] DEFAULT (N'SELF'),
        [DraftJson] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_FamilyTree_PersonDraft_DraftJson] DEFAULT (N'{}'),
        [AuditStatus] NVARCHAR(16) NOT NULL CONSTRAINT [DF_FamilyTree_PersonDraft_AuditStatus] DEFAULT (N'PASS'),
        [ResultPersonId] INT NULL,
        [BStatus] NVARCHAR(1) NOT NULL CONSTRAINT [DF_FamilyTree_PersonDraft_BStatus] DEFAULT (N'1'),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_FamilyTree_PersonDraft_IsDeleted] DEFAULT (0),
        [Remark] NVARCHAR(512) NULL,
        [CreateDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_PersonDraft_CreateDate] DEFAULT (GETDATE()),
        [AmendDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_PersonDraft_AmendDate] DEFAULT (GETDATE()),
        [Operator] NVARCHAR(30) NOT NULL CONSTRAINT [DF_FamilyTree_PersonDraft_Operator] DEFAULT (N''),
        CONSTRAINT [PK_FamilyTree_PersonDraft] PRIMARY KEY CLUSTERED ([DataID] ASC)
    );
END
GO

-- FamilyTree_PersonLink
IF OBJECT_ID(N'dbo.FamilyTree_PersonLink', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_PersonLink (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [SourcePersonId] INT NOT NULL CONSTRAINT [DF_FamilyTree_PersonLink_SourcePersonId] DEFAULT (0),
        [TargetMainPersonId] INT NOT NULL CONSTRAINT [DF_FamilyTree_PersonLink_TargetMainPersonId] DEFAULT (0),
        [ApplyUserId] INT NOT NULL CONSTRAINT [DF_FamilyTree_PersonLink_ApplyUserId] DEFAULT (0),
        [AuditSuperAdminId] INT NULL,
        [LinkStatus] NVARCHAR(16) NOT NULL CONSTRAINT [DF_FamilyTree_PersonLink_LinkStatus] DEFAULT (N'PENDING'),
        [MatchLevel] TINYINT NULL,
        [UnlinkTime] DATETIME NULL,
        [BStatus] NVARCHAR(1) NOT NULL CONSTRAINT [DF_FamilyTree_PersonLink_BStatus] DEFAULT (N'1'),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_FamilyTree_PersonLink_IsDeleted] DEFAULT (0),
        [Remark] NVARCHAR(512) NULL,
        [CreateDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_PersonLink_CreateDate] DEFAULT (GETDATE()),
        [AmendDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_PersonLink_AmendDate] DEFAULT (GETDATE()),
        [Operator] NVARCHAR(30) NOT NULL CONSTRAINT [DF_FamilyTree_PersonLink_Operator] DEFAULT (N''),
        CONSTRAINT [PK_FamilyTree_PersonLink] PRIMARY KEY CLUSTERED ([DataID] ASC)
    );
END
GO

-- FamilyTree_PersonMarry
IF OBJECT_ID(N'dbo.FamilyTree_PersonMarry', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_PersonMarry (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [PersonId] INT NOT NULL CONSTRAINT [DF_FamilyTree_PersonMarry_PersonId] DEFAULT (0),
        [SpouseName] NVARCHAR(64) NULL,
        [SpouseBirth] NVARCHAR(32) NULL,
        [MarryType] NVARCHAR(32) NOT NULL CONSTRAINT [DF_FamilyTree_PersonMarry_MarryType] DEFAULT (N'原配'),
        [HouseSeq] INT NOT NULL CONSTRAINT [DF_FamilyTree_PersonMarry_HouseSeq] DEFAULT (99),
        [SpousePersonId] INT NULL,
        [BStatus] NVARCHAR(1) NOT NULL CONSTRAINT [DF_FamilyTree_PersonMarry_BStatus] DEFAULT (N'1'),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_FamilyTree_PersonMarry_IsDeleted] DEFAULT (0),
        [Remark] NVARCHAR(512) NULL,
        [CreateDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_PersonMarry_CreateDate] DEFAULT (GETDATE()),
        [AmendDate] DATETIME NOT NULL CONSTRAINT [DF_FamilyTree_PersonMarry_AmendDate] DEFAULT (GETDATE()),
        [Operator] NVARCHAR(30) NOT NULL CONSTRAINT [DF_FamilyTree_PersonMarry_Operator] DEFAULT (N''),
        CONSTRAINT [PK_FamilyTree_PersonMarry] PRIMARY KEY CLUSTERED ([DataID] ASC)
    );
END
GO


/* ========== 乐观并发：rowversion ==========
   FtPerson / FtPersonLink / FtPersonDraft 原先没有并发令牌，EF 因此发出无条件
   UPDATE ... WHERE DataID=@id，后写覆盖先写且无提示。两名管理员并发审批同一张链入单时，
   双方都会看到 PENDING 并各跑一遍 AttachChildrenAsync。加上 rowversion 后，
   后提交的一方会拿到 DbUpdateConcurrencyException，被转成「该单已被他人处理」。

   对已存在的库：本段同样幂等，直接重跑本脚本即可补列。 */
IF COL_LENGTH(N'dbo.FamilyTree_Person', N'RowVersion') IS NULL
    ALTER TABLE dbo.FamilyTree_Person ADD [RowVersion] rowversion NOT NULL;
GO
IF COL_LENGTH(N'dbo.FamilyTree_PersonLink', N'RowVersion') IS NULL
    ALTER TABLE dbo.FamilyTree_PersonLink ADD [RowVersion] rowversion NOT NULL;
GO
IF COL_LENGTH(N'dbo.FamilyTree_PersonDraft', N'RowVersion') IS NULL
    ALTER TABLE dbo.FamilyTree_PersonDraft ADD [RowVersion] rowversion NOT NULL;
GO

/* ========== 索引 ==========
   族谱图的边只以裸 int 存在：FatherPersonId / MotherPersonId / SameAsPersonId 既无外键
   也无索引，而每次树查询都要按它们过滤。原先整张 Person 表只有 2 个索引（ClanId、CertCode）。 */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyTree_Person_FatherPersonId' AND object_id = OBJECT_ID(N'dbo.FamilyTree_Person'))
    CREATE INDEX [IX_FamilyTree_Person_FatherPersonId] ON dbo.FamilyTree_Person ([FatherPersonId]) WHERE [FatherPersonId] IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyTree_Person_MotherPersonId' AND object_id = OBJECT_ID(N'dbo.FamilyTree_Person'))
    CREATE INDEX [IX_FamilyTree_Person_MotherPersonId] ON dbo.FamilyTree_Person ([MotherPersonId]) WHERE [MotherPersonId] IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyTree_Person_SameAsPersonId' AND object_id = OBJECT_ID(N'dbo.FamilyTree_Person'))
    CREATE INDEX [IX_FamilyTree_Person_SameAsPersonId] ON dbo.FamilyTree_Person ([SameAsPersonId]) WHERE [SameAsPersonId] IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyTree_Person_OwnerUserId' AND object_id = OBJECT_ID(N'dbo.FamilyTree_Person'))
    CREATE INDEX [IX_FamilyTree_Person_OwnerUserId] ON dbo.FamilyTree_Person ([OwnerUserId]) INCLUDE ([IsDeleted], [InMainGenealogy]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyTree_Person_BindUserId' AND object_id = OBJECT_ID(N'dbo.FamilyTree_Person'))
    CREATE INDEX [IX_FamilyTree_Person_BindUserId] ON dbo.FamilyTree_Person ([BindUserId]) WHERE [BindUserId] IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyTree_Person_InMainGenealogy' AND object_id = OBJECT_ID(N'dbo.FamilyTree_Person'))
    CREATE INDEX [IX_FamilyTree_Person_InMainGenealogy] ON dbo.FamilyTree_Person ([InMainGenealogy], [IsDeleted]) INCLUDE ([ClanId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyTree_PersonLink_Source' AND object_id = OBJECT_ID(N'dbo.FamilyTree_PersonLink'))
    CREATE INDEX [IX_FamilyTree_PersonLink_Source] ON dbo.FamilyTree_PersonLink ([SourcePersonId], [LinkStatus]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyTree_PersonLink_Status' AND object_id = OBJECT_ID(N'dbo.FamilyTree_PersonLink'))
    CREATE INDEX [IX_FamilyTree_PersonLink_Status] ON dbo.FamilyTree_PersonLink ([LinkStatus], [IsDeleted]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyTree_OpLog_CreateDate' AND object_id = OBJECT_ID(N'dbo.FamilyTree_OpLog'))
    CREATE INDEX [IX_FamilyTree_OpLog_CreateDate] ON dbo.FamilyTree_OpLog ([CreateDate] DESC) INCLUDE ([OpUserId], [OpType]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyTree_ApiToken_TokenHash' AND object_id = OBJECT_ID(N'dbo.FamilyTree_ApiToken'))
    CREATE INDEX [IX_FamilyTree_ApiToken_TokenHash] ON dbo.FamilyTree_ApiToken ([TokenHash], [ExpireDate]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyTree_AccountBind_IdCardHash' AND object_id = OBJECT_ID(N'dbo.FamilyTree_AccountBind'))
    CREATE INDEX [IX_FamilyTree_AccountBind_IdCardHash] ON dbo.FamilyTree_AccountBind ([IdCardHash]) WHERE [IdCardHash] IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FamilyTree_AccountBind_WechatOpenId' AND object_id = OBJECT_ID(N'dbo.FamilyTree_AccountBind'))
    CREATE INDEX [IX_FamilyTree_AccountBind_WechatOpenId] ON dbo.FamilyTree_AccountBind ([WechatOpenId]) WHERE [WechatOpenId] IS NOT NULL;
GO

/* GetSelfPersonAsync 按 BindUserId 取 FirstOrDefault，而该列原先无唯一约束——
   重复的「本人」记录可以静默存在，并静默解析成 SQL Server 恰好先返回的那一行。
   过滤式唯一索引：只约束未删除且确实绑定了用户的行。 */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_FamilyTree_Person_BindUserId' AND object_id = OBJECT_ID(N'dbo.FamilyTree_Person'))
    CREATE UNIQUE INDEX [UX_FamilyTree_Person_BindUserId] ON dbo.FamilyTree_Person ([BindUserId])
        WHERE [BindUserId] IS NOT NULL AND [IsDeleted] = 0;
GO

PRINT N'29-CreateTbl_FamilyTree_Core.sql 执行完成。';
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'29-CreateTbl_FamilyTree_Core.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'29-CreateTbl_FamilyTree_Core.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'29-CreateTbl_FamilyTree_Core.sql');
GO
