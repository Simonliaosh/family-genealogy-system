-- =============================================
-- 收窄族谱管理员菜单：只保留家族业务，不含框架岗位职责
-- 可重复执行。依赖 44-Seed_FtClanAdmin.sql。
-- =============================================
USE [FamilyTree];
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-CLAN-SCOPE';
DECLARE @ClanDuty INT = (SELECT TOP 1 DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'FT_CLAN_ADMIN' AND IsDeleted=0);

IF @ClanDuty IS NULL
BEGIN
    PRINT N'FT_CLAN_ADMIN duty missing; skip.';
END
ELSE
BEGIN

-- 去掉误拷贝的权限（含「填写权限岗」等岗位向菜单；支链任免改走家族成员页）
UPDATE dbo.Tbl_E_Subscription
SET IsDeleted=1, AmendDate=@Now, Operator=@Op
WHERE DutyID=@ClanDuty AND IsDeleted=0 AND SubType='RESOURCE'
  AND ResourceID NOT IN (
    N'RES.FT.ClanAdmin',
    N'RES.FT.Person',
    N'RES.FT.PersonCreate',
    N'RES.FT.PersonMarry',
    N'RES.FT.MyProfile',
    N'RES.FT.PersonDraft',
    N'RES.FT.PersonLink',
    N'RES.FT.LinkAudit',
    N'RES.FT.TreeView',
    N'RES.FT.GenerationWord',
    N'RES.FT.Export'
  );

UPDATE dbo.Tbl_E_ResourcePermission
SET IsDeleted=1, AmendDate=@Now, Operator=@Op
WHERE DutyID=@ClanDuty AND IsDeleted=0
  AND ResourceID NOT IN (
    N'RES.FT.ClanAdmin',
    N'RES.FT.Person',
    N'RES.FT.PersonCreate',
    N'RES.FT.PersonMarry',
    N'RES.FT.MyProfile',
    N'RES.FT.PersonDraft',
    N'RES.FT.PersonLink',
    N'RES.FT.LinkAudit',
    N'RES.FT.TreeView',
    N'RES.FT.GenerationWord',
    N'RES.FT.Export'
  );

-- 明确写入白名单（幂等）
;WITH Want(ResourceID, FunctionLimit, CanCreate, CanUpdate, CanDelete, CanQuery, DispSeq) AS (
    SELECT * FROM (VALUES
        (N'RES.FT.ClanAdmin',     '101000', 0,1,0,1, 5),
        (N'RES.FT.Person',        '101000', 0,1,0,1, 10),
        (N'RES.FT.PersonCreate',  '110000', 1,0,0,1, 20),
        (N'RES.FT.PersonMarry',   '101000', 0,1,0,1, 30),
        (N'RES.FT.MyProfile',     '101000', 0,1,0,1, 40),
        (N'RES.FT.PersonDraft',   '100000', 0,0,0,1, 10),
        (N'RES.FT.PersonLink',    '110000', 1,0,0,1, 10),
        (N'RES.FT.LinkAudit',     '101000', 0,1,0,1, 20),
        (N'RES.FT.TreeView',      '100000', 0,0,0,1, 10),
        (N'RES.FT.GenerationWord','100000', 0,0,0,1, 60),
        (N'RES.FT.Export',        '100000', 0,0,0,1, 20)
    ) v(ResourceID, FunctionLimit, CanCreate, CanUpdate, CanDelete, CanQuery, DispSeq)
)
INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT @ClanDuty, 'FamilyTree', 'RESOURCE', NULL, w.ResourceID, 1, w.FunctionLimit, w.DispSeq, '1', 0, @Now, @Now, @Op
FROM Want w
WHERE EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource r WHERE r.ResourceID=w.ResourceID AND r.IsDeleted=0)
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=@ClanDuty AND s.ResourceID=w.ResourceID AND s.IsDeleted=0);

;WITH Want2(ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery) AS (
    SELECT ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery FROM (VALUES
        (N'RES.FT.ClanAdmin',     0,1,0,1),
        (N'RES.FT.Person',        0,1,0,1),
        (N'RES.FT.PersonCreate',  1,0,0,1),
        (N'RES.FT.PersonMarry',   0,1,0,1),
        (N'RES.FT.MyProfile',     0,1,0,1),
        (N'RES.FT.PersonDraft',   0,0,0,1),
        (N'RES.FT.PersonLink',    1,0,0,1),
        (N'RES.FT.LinkAudit',     0,1,0,1),
        (N'RES.FT.TreeView',      0,0,0,1),
        (N'RES.FT.GenerationWord',0,0,0,1),
        (N'RES.FT.Export',        0,0,0,1)
    ) v(ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery)
)
INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT @ClanDuty, w.ResourceID, w.CanCreate, w.CanUpdate, w.CanDelete, w.CanQuery, 0, 0, '1', 0, @Now, @Now, @Op
FROM Want2 w
WHERE EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource r WHERE r.ResourceID=w.ResourceID AND r.IsDeleted=0)
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=@ClanDuty AND rp.ResourceID=w.ResourceID AND rp.IsDeleted=0);

UPDATE s SET IsDeleted=0, AmendDate=@Now, Operator=@Op
FROM dbo.Tbl_E_Subscription s
WHERE s.DutyID=@ClanDuty AND s.IsDeleted=1 AND s.ResourceID IN (
    N'RES.FT.ClanAdmin',N'RES.FT.Person',N'RES.FT.PersonCreate',N'RES.FT.PersonMarry',
    N'RES.FT.MyProfile',N'RES.FT.PersonDraft',N'RES.FT.PersonLink',N'RES.FT.LinkAudit',
    N'RES.FT.TreeView',N'RES.FT.GenerationWord',N'RES.FT.Export');

UPDATE rp SET IsDeleted=0, AmendDate=@Now, Operator=@Op
FROM dbo.Tbl_E_ResourcePermission rp
WHERE rp.DutyID=@ClanDuty AND rp.IsDeleted=1 AND rp.ResourceID IN (
    N'RES.FT.ClanAdmin',N'RES.FT.Person',N'RES.FT.PersonCreate',N'RES.FT.PersonMarry',
    N'RES.FT.MyProfile',N'RES.FT.PersonDraft',N'RES.FT.PersonLink',N'RES.FT.LinkAudit',
    N'RES.FT.TreeView',N'RES.FT.GenerationWord',N'RES.FT.Export');

UPDATE dbo.Tbl_E_Resource
SET ResourceName=N'我的家族', AmendDate=@Now, Operator=@Op
WHERE ResourceID=N'RES.FT.ClanAdmin' AND IsDeleted=0;

PRINT N'FT_CLAN_ADMIN scoped to family menus only.';
END
GO
