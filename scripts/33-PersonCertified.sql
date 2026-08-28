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
