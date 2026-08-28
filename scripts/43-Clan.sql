-- =============================================
-- 家族（Clan）隔离：家族表、人物 ClanId、用户入族（一人一家）
-- 可重复执行。
-- =============================================
USE [FamilyTree];
GO

IF OBJECT_ID(N'dbo.FamilyTree_Clan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_Clan
    (
        DataID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ClanCode        NVARCHAR(16)  NOT NULL,   -- 分享用家族 ID
        ClanName        NVARCHAR(64)  NOT NULL,
        OwnerUserId     INT           NOT NULL,   -- 创建人（首任族谱管理员）
        Remark          NVARCHAR(256) NULL,
        BStatus         VARCHAR(1)    NOT NULL CONSTRAINT DF_FT_Clan_BStatus DEFAULT ('1'),
        IsDeleted       BIT           NOT NULL CONSTRAINT DF_FT_Clan_IsDeleted DEFAULT (0),
        CreateDate      DATETIME      NOT NULL,
        AmendDate       DATETIME      NOT NULL,
        OperatorName    VARCHAR(30)   NOT NULL
    );
    CREATE UNIQUE NONCLUSTERED INDEX UX_FT_Clan_Code ON dbo.FamilyTree_Clan (ClanCode) WHERE IsDeleted = 0;
END
GO

IF OBJECT_ID(N'dbo.FamilyTree_UserClan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyTree_UserClan
    (
        DataID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId          INT           NOT NULL,
        ClanId          INT           NOT NULL,
        JoinDate        DATETIME      NOT NULL,
        BStatus         VARCHAR(1)    NOT NULL CONSTRAINT DF_FT_UserClan_BStatus DEFAULT ('1'),
        IsDeleted       BIT           NOT NULL CONSTRAINT DF_FT_UserClan_IsDeleted DEFAULT (0),
        CreateDate      DATETIME      NOT NULL,
        AmendDate       DATETIME      NOT NULL,
        OperatorName    VARCHAR(30)   NOT NULL
    );
    CREATE UNIQUE NONCLUSTERED INDEX UX_FT_UserClan_User ON dbo.FamilyTree_UserClan (UserId) WHERE IsDeleted = 0;
    CREATE NONCLUSTERED INDEX IX_FT_UserClan_Clan ON dbo.FamilyTree_UserClan (ClanId) WHERE IsDeleted = 0;
END
GO

IF COL_LENGTH(N'dbo.FamilyTree_Person', N'ClanId') IS NULL
BEGIN
    ALTER TABLE dbo.FamilyTree_Person ADD ClanId INT NULL;
    CREATE NONCLUSTERED INDEX IX_FT_Person_ClanId ON dbo.FamilyTree_Person (ClanId) WHERE IsDeleted = 0;
END
GO

-- 已有数据：迁入一个默认家族，避免旧数据无 ClanId
IF NOT EXISTS (SELECT 1 FROM dbo.FamilyTree_Clan WHERE ClanCode = N'DEFAULT' AND IsDeleted = 0)
BEGIN
    DECLARE @Now0 DATETIME = GETDATE();
    DECLARE @Owner0 INT = (SELECT TOP 1 DataID FROM dbo.Tbl_E_Users WHERE IsDeleted = 0 ORDER BY DataID);
    IF @Owner0 IS NULL SET @Owner0 = 1;
    INSERT INTO dbo.FamilyTree_Clan (ClanCode, ClanName, OwnerUserId, Remark, BStatus, IsDeleted, CreateDate, AmendDate, OperatorName)
    VALUES (N'DEFAULT', N'默认家族（历史数据）', @Owner0, N'脚本迁入，可改名', '1', 0, @Now0, @Now0, 'SEED-CLAN');
END
GO

DECLARE @DefClan INT = (SELECT TOP 1 DataID FROM dbo.FamilyTree_Clan WHERE ClanCode = N'DEFAULT' AND IsDeleted = 0);
IF @DefClan IS NOT NULL
BEGIN
    UPDATE dbo.FamilyTree_Person SET ClanId = @DefClan WHERE ClanId IS NULL AND IsDeleted = 0;

    INSERT INTO dbo.FamilyTree_UserClan (UserId, ClanId, JoinDate, BStatus, IsDeleted, CreateDate, AmendDate, OperatorName)
    SELECT DISTINCT p.OwnerUserId, @DefClan, GETDATE(), '1', 0, GETDATE(), GETDATE(), 'SEED-CLAN'
    FROM dbo.FamilyTree_Person p
    WHERE p.IsDeleted = 0 AND p.OwnerUserId > 0
      AND NOT EXISTS (SELECT 1 FROM dbo.FamilyTree_UserClan uc WHERE uc.UserId = p.OwnerUserId AND uc.IsDeleted = 0);

    INSERT INTO dbo.FamilyTree_UserClan (UserId, ClanId, JoinDate, BStatus, IsDeleted, CreateDate, AmendDate, OperatorName)
    SELECT DISTINCT p.BindUserId, @DefClan, GETDATE(), '1', 0, GETDATE(), GETDATE(), 'SEED-CLAN'
    FROM dbo.FamilyTree_Person p
    WHERE p.IsDeleted = 0 AND p.BindUserId IS NOT NULL AND p.BindUserId > 0
      AND NOT EXISTS (SELECT 1 FROM dbo.FamilyTree_UserClan uc WHERE uc.UserId = p.BindUserId AND uc.IsDeleted = 0);
END
GO

PRINT N'FamilyTree_Clan / UserClan / Person.ClanId ready.';
GO
