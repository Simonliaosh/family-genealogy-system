-- =============================================
-- 人物认证：未认证不得加入主谱。可重复执行。
-- 已在主谱的历史人物补为已认证。
-- =============================================
USE [FamilyTree];
GO

IF COL_LENGTH(N'dbo.FamilyTree_Person', N'IsCertified') IS NULL
BEGIN
    ALTER TABLE dbo.FamilyTree_Person ADD
        IsCertified BIT NOT NULL CONSTRAINT DF_FT_Person_Certified DEFAULT (0);
END
GO

UPDATE dbo.FamilyTree_Person
SET IsCertified = 1
WHERE InMainGenealogy = 1
  AND IsCertified = 0
  AND IsDeleted = 0;
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'33-PersonCertified.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'33-PersonCertified.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'33-PersonCertified.sql');
GO
