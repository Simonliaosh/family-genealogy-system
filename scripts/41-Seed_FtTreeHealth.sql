-- =============================================
-- 谱系体检菜单（分支管/超管）
-- 可重复执行。须先有 FT_LINK 或 FT_OPS 菜单组。
-- =============================================
USE [FamilyTree];
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-HEALTH';
DECLARE @App VARCHAR(50) = 'FamilyTree';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='FT_OPS' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FT_OPS', @App, N'运维', 60, '1', 0, @Now, @Now, @Op);

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.TreeHealth' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.TreeHealth', N'谱系体检', 'MENU', N'/FtTreeHealth/Index', 'FT_OPS', 30, '1', 0, @Now, @Now, @Op);

INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, 'RES.FT.TreeHealth', 1, '100000', 30, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
WHERE d.DutyCode=N'FT_BRANCH_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID='RES.FT.TreeHealth' AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, 'RES.FT.TreeHealth', 0, 0, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
WHERE d.DutyCode=N'FT_BRANCH_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID='RES.FT.TreeHealth');

INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, 'RES.FT.TreeHealth', 1, '111000', 30, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID='RES.FT.TreeHealth' AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, 'RES.FT.TreeHealth', 1, 1, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID='RES.FT.TreeHealth');
GO

PRINT N'RES.FT.TreeHealth seeded.';
GO
