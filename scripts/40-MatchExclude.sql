-- =============================================
-- 排除匹配（Not a Match）表
-- 可重复执行。
-- =============================================
USE [FamilyTree];
GO

IF OBJECT_ID(N'dbo.FamilyTree_MatchExclude', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_MatchExclude
    (
        DataID          INT IDENTITY(1,1) NOT NULL,
        PersonLoId      INT NOT NULL,
        PersonHiId      INT NOT NULL,
        MarkUserId      INT NOT NULL,
        Remark          NVARCHAR(256) NULL,
        BStatus         VARCHAR(1) NOT NULL CONSTRAINT DF_FT_MatchEx_BStatus DEFAULT ('1'),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_FT_MatchEx_Del DEFAULT (0),
        CreateDate      DATETIME NOT NULL,
        AmendDate       DATETIME NOT NULL,
        Operator        VARCHAR(30) NOT NULL,
        CONSTRAINT PK_FamilyTree_MatchExclude PRIMARY KEY CLUSTERED (DataID)
    );
    CREATE UNIQUE NONCLUSTERED INDEX UX_FT_MatchEx_Pair
        ON dbo.FamilyTree_MatchExclude (PersonLoId, PersonHiId) WHERE IsDeleted = 0;
    CREATE NONCLUSTERED INDEX IX_FT_MatchEx_Lo ON dbo.FamilyTree_MatchExclude (PersonLoId) WHERE IsDeleted = 0;
    CREATE NONCLUSTERED INDEX IX_FT_MatchEx_Hi ON dbo.FamilyTree_MatchExclude (PersonHiId) WHERE IsDeleted = 0;
END
GO

PRINT N'FamilyTree_MatchExclude ready.';
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'40-MatchExclude.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'40-MatchExclude.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'40-MatchExclude.sql');
GO
