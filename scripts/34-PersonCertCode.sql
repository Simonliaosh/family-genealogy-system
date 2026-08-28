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
