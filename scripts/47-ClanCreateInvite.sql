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
