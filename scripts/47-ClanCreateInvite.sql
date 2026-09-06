-- =============================================
-- 创建新家族邀请码（仅超管 / 族谱管理员可发）
-- 扫码人凭码创建一条新的家族链，成为该族族谱管理员。
-- 可重复执行。
-- =============================================
USE [FamilyTree];
GO

IF OBJECT_ID(N'dbo.FamilyTree_ClanCreateInvite', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_ClanCreateInvite
    (
        DataID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InviteCode      VARCHAR(16)   NOT NULL,
        CreatedByUserId INT           NOT NULL,
        ExpireAt        DATETIME      NOT NULL,
        InviteStatus    VARCHAR(16)   NOT NULL,  -- OPEN / USED / REVOKED
        UsedByUserId    INT           NULL,
        UsedClanId      INT           NULL,
        Remark          NVARCHAR(128) NULL,
        BStatus         VARCHAR(1)    NOT NULL CONSTRAINT DF_FT_ClanCInv_BStatus DEFAULT ('1'),
        IsDeleted       BIT           NOT NULL CONSTRAINT DF_FT_ClanCInv_IsDeleted DEFAULT (0),
        CreateDate      DATETIME      NOT NULL,
        AmendDate       DATETIME      NOT NULL,
        OperatorName    VARCHAR(30)   NOT NULL
    );
    CREATE UNIQUE NONCLUSTERED INDEX UX_FT_ClanCInv_Code
        ON dbo.FamilyTree_ClanCreateInvite (InviteCode) WHERE IsDeleted = 0;
    CREATE NONCLUSTERED INDEX IX_FT_ClanCInv_Status
        ON dbo.FamilyTree_ClanCreateInvite (InviteStatus, ExpireAt) WHERE IsDeleted = 0;
END
GO

PRINT N'FamilyTree_ClanCreateInvite ready.';
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'47-ClanCreateInvite.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'47-ClanCreateInvite.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'47-ClanCreateInvite.sql');
GO
