-- =============================================
-- 族员认证码：扫码认证。可重复执行。
-- SQL Server 2008 R2 支持 WHERE 过滤唯一索引。
-- =============================================
USE [FamilyTree];
GO

IF COL_LENGTH(N'dbo.FamilyTree_Person', N'IsCertified') IS NULL
BEGIN
    ALTER TABLE dbo.FamilyTree_Person ADD
        IsCertified BIT NOT NULL CONSTRAINT DF_FT_Person_Certified DEFAULT (0);
END
GO

IF COL_LENGTH(N'dbo.FamilyTree_Person', N'CertCode') IS NULL
BEGIN
    ALTER TABLE dbo.FamilyTree_Person ADD
        CertCode VARCHAR(16) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_FT_Person_CertCode'
      AND object_id = OBJECT_ID(N'dbo.FamilyTree_Person')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_FT_Person_CertCode
        ON dbo.FamilyTree_Person (CertCode)
        WHERE CertCode IS NOT NULL;
END
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'34-PersonCertCode.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'34-PersonCertCode.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'34-PersonCertCode.sql');
GO
