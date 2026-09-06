/* 库名统一为 FamilyTree：本脚本原先没有 USE，会落在执行工具当时选中的库上。 */
USE [FamilyTree];
GO

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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'27-Patch_EDataImport_Menu.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'27-Patch_EDataImport_Menu.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'27-Patch_EDataImport_Menu.sql');
GO
