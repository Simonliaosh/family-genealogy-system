/*
  补丁：投产数据导入菜单（Web 框架内置）
  执行：在 23-Seed_Menus.sql 之后；可重复执行。
*/
SET NOCOUNT ON;
DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'PATCH-EDATAIMPORT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID = 'RES.CF.EDataImport' AND IsDeleted = 0)
    INSERT INTO dbo.Tbl_E_Resource (ResourceID, ResourceName, ResourceType, AppCode, MenuPath, MenuGroupCode, ParentResourceID, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('RES.CF.EDataImport', N'投产数据导入', 'MENU', 'FRAME', N'/EDataImport/Index', 'SYS', NULL, 54, N'Excel+框架SQL一键导入', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Resource SET ResourceName=N'投产数据导入', MenuPath=N'/EDataImport/Index', MenuGroupCode='SYS', DispSeq=54, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE ResourceID='RES.CF.EDataImport';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s INNER JOIN dbo.Tbl_E_Duty d ON d.DataID = s.DutyID
    WHERE d.DutyCode = N'cfadmin' AND s.SubType = 'RESOURCE' AND s.ResourceID = 'RES.CF.EDataImport' AND s.IsDeleted = 0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EDataImport', 1, '111111', 58, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode = N'cfadmin';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID = rp.DutyID
    WHERE d.DutyCode = N'cfadmin' AND rp.ResourceID = 'RES.CF.EDataImport' AND rp.IsDeleted = 0)
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EDataImport', 1, 1, 1, 1, 0, 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode = N'cfadmin';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, CanImport=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID = rp.DutyID
    WHERE d.DutyCode = N'cfadmin' AND rp.ResourceID = 'RES.CF.EDataImport';

PRINT N'已注册菜单 RES.CF.EDataImport -> /EDataImport/Index';
