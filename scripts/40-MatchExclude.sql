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
