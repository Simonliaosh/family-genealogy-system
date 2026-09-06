-- =============================================
-- 外链对接：邀请 + 接点（数据不动，授权互看）
-- 可重复执行。SQL Server 2008 R2。
-- =============================================
USE [FamilyTree];
GO

IF OBJECT_ID(N'dbo.FamilyTree_PeerInvite', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_PeerInvite
    (
        DataID          INT IDENTITY(1,1) NOT NULL,
        InviteCode      VARCHAR(32) NOT NULL,
        LocalPersonId   INT NOT NULL,
        SiteId          VARCHAR(36) NOT NULL,
        ExpireDate      DATETIME NOT NULL,
        InviteStatus    VARCHAR(16) NOT NULL, -- OPEN / USED / CANCELLED
        CreateUserId    INT NOT NULL,
        BStatus         CHAR(1) NOT NULL CONSTRAINT DF_FT_PInv_BStatus DEFAULT ('1'),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_FT_PInv_IsDeleted DEFAULT (0),
        Remark          NVARCHAR(512) NULL,
        CreateDate      DATETIME NOT NULL,
        AmendDate       DATETIME NOT NULL,
        Operator        VARCHAR(30) NOT NULL,
        CONSTRAINT PK_FamilyTree_PeerInvite PRIMARY KEY CLUSTERED (DataID)
    );
    CREATE UNIQUE NONCLUSTERED INDEX UX_FT_PInv_Code ON dbo.FamilyTree_PeerInvite (InviteCode) WHERE IsDeleted = 0;
    CREATE NONCLUSTERED INDEX IX_FT_PInv_Person ON dbo.FamilyTree_PeerInvite (LocalPersonId, InviteStatus);
END
GO

IF OBJECT_ID(N'dbo.FamilyTree_PeerBridge', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_PeerBridge
    (
        DataID          INT IDENTITY(1,1) NOT NULL,
        LocalPersonId   INT NOT NULL,
        PeerBaseUrl     NVARCHAR(256) NOT NULL,
        PeerSiteId      VARCHAR(36) NOT NULL,
        PeerPersonId    INT NOT NULL,
        PeerLabel       NVARCHAR(64) NULL,
        BridgeStatus    VARCHAR(16) NOT NULL, -- ACTIVE / REVOKED
        -- 对方调用本站时提交的令牌哈希
        OutTokenHash    CHAR(64) NOT NULL,
        -- 本站调用对方时使用的明文令牌（仅服务端）
        InToken         VARCHAR(64) NOT NULL,
        InviteCode      VARCHAR(32) NULL,
        CreateUserId    INT NOT NULL,
        BStatus         CHAR(1) NOT NULL CONSTRAINT DF_FT_PBr_BStatus DEFAULT ('1'),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_FT_PBr_IsDeleted DEFAULT (0),
        Remark          NVARCHAR(512) NULL,
        CreateDate      DATETIME NOT NULL,
        AmendDate       DATETIME NOT NULL,
        Operator        VARCHAR(30) NOT NULL,
        CONSTRAINT PK_FamilyTree_PeerBridge PRIMARY KEY CLUSTERED (DataID)
    );
    CREATE NONCLUSTERED INDEX IX_FT_PBr_Local ON dbo.FamilyTree_PeerBridge (LocalPersonId, BridgeStatus);
    CREATE NONCLUSTERED INDEX IX_FT_PBr_OutTok ON dbo.FamilyTree_PeerBridge (OutTokenHash);
    CREATE NONCLUSTERED INDEX IX_FT_PBr_Peer ON dbo.FamilyTree_PeerBridge (PeerSiteId, PeerPersonId);
END
GO

PRINT N'FamilyTree_PeerInvite / PeerBridge ready.';
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'35-PeerBridge.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'35-PeerBridge.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'35-PeerBridge.sql');
GO
