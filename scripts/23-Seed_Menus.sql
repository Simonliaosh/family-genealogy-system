/* 库名统一为 FamilyTree：本脚本原先没有 USE，会落在执行工具当时选中的库上。 */
USE [FamilyTree];
GO

/*
==============================================================================
  EFrame 种子 04 - 菜单资源/订阅/按钮权限
==============================================================================
  来源：EFrame.xls + EFrame 架构对齐（幂等 MERGE / IF NOT EXISTS）
  前置：docs/EFrame_CreateTables.sql、docs/EFrame_v2_supplement.sql
  依赖：22-Seed_Users.sql
  顺序：第 4 步
  编码：ANSI (GBK)
==============================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-EFRAME';

/* ----- 菜单资源 ----- */

IF OBJECT_ID('tempdb..#ResSeed') IS NOT NULL DROP TABLE #ResSeed;
CREATE TABLE #ResSeed (ResourceID VARCHAR(50) NOT NULL PRIMARY KEY, ResourceName NVARCHAR(100) NOT NULL, MenuPath NVARCHAR(300) NOT NULL, MenuGroupCode VARCHAR(50) NOT NULL, DispSeq INT NOT NULL, Remark NVARCHAR(200) NULL);
INSERT INTO #ResSeed (ResourceID, ResourceName, MenuPath, MenuGroupCode, DispSeq, Remark) VALUES
('RES.CF.Home', N'首页', N'/Home/Index', 'SYS', 10, NULL),
('RES.CF.EUsers', N'用户账号', N'/EUsers/Index', 'SYS', 25, NULL),
('RES.CF.EDept', N'部门管理', N'/EDepartment/Index', 'ORG', 110, NULL),
('RES.CF.EDictQuery', N'字典查询', N'/EDictQuery/Index', 'SYS', 40, NULL),
('RES.CF.EDictType', N'字典维护', N'/EDictType/Index', 'SYS', 42, NULL),
('RES.CF.EAppModule', N'应用模块查询', N'/EAppModuleQuery/Index', 'SYS', 50, NULL),
('RES.CF.EAppModuleMnt', N'应用模块维护', N'/EAppModule/Index', 'SYS', 52, NULL),
('RES.CF.EEventInst', N'事件实例', N'/EEventInstanceQuery/Index', 'EVT', 230, NULL),
('RES.CF.EventDemo', N'事件发布 Demo', N'/EventDemo/Index', 'EVT', 260, NULL),
('RES.CF.EMenuGroup', N'菜单组管理', N'/EMenuGroup/Index', 'SYS', 15, NULL),
('RES.CF.EResource', N'资源管理', N'/EResource/Index', 'SYS', 20, NULL),
('RES.CF.ESubscription', N'订阅管理', N'/ESubscription/Index', 'SYS', 30, NULL),
('RES.CF.EResourcePermission', N'资源权限', N'/EResourcePermission/Index', 'SYS', 35, NULL),
('RES.CF.EPosition', N'岗位管理', N'/EPosition/Index', 'ORG', 120, NULL),
('RES.CF.EDuty', N'职责管理', N'/EDuty/Index', 'ORG', 130, NULL),
('RES.CF.EUserPosition', N'用户岗位', N'/EUserPosition/Index', 'ORG', 140, NULL),
('RES.CF.EPositionDuty', N'岗位职责', N'/EPositionDuty/Index', 'ORG', 150, NULL),
('RES.CF.EManagerSubordinate', N'上下级关系', N'/EManagerSubordinate/Index', 'ORG', 160, NULL),
('RES.CF.EUserHandover', N'用户交接', N'/EUserHandover/Index', 'ORG', 170, NULL),
('RES.CF.EMember', N'人员档案', N'/EMember/Index', 'ORG', 180, NULL),
('RES.CF.EEventConfig', N'事件配置', N'/EEventConfig/Index', 'EVT', 210, NULL),
('RES.CF.EEventFlowRule', N'事件流转规则', N'/EEventFlowRule/Index', 'EVT', 220, NULL),
('RES.CF.EEventLog', N'事件日志', N'/EEventLog/Index', 'EVT', 240, NULL),
('RES.CF.ETodoTask', N'待办任务', N'/ETodoTask/Index', 'EVT', 250, NULL),
('RES.CF.ELoginLog', N'登录日志', N'/ELoginLog/Index', 'LOG', 310, NULL),
('RES.HR.Home', N'人事工作台', N'/HrHome/Index', 'HR', 10, NULL),
('RES.HR.LeaveApply', N'请假申请', N'/HrLeave/Create', 'HR', 20, NULL),
('RES.HR.LeaveApproval', N'请假审批', N'/HrLeave/Approval', 'HR', 40, NULL),
('RES.HR.LeaveEvents', N'请假事件查询', N'/EEventInstanceQuery/Index', 'HR', 70, NULL),
('RES.HR.Member', N'人员档案', N'/EMember/Index', 'HR', 60, NULL),
('RES.HR.MyLeave', N'我的请假', N'/HrLeave/Index', 'HR', 30, NULL),
('RES.HR.MyTodo', N'我的待办', N'/ETodoTask/Index', 'HR', 50, NULL)

;
MERGE dbo.Tbl_E_Resource AS t
USING (SELECT 'FRAME' AS AppCode, s.* FROM #ResSeed s) AS s
ON t.ResourceID = s.ResourceID
WHEN NOT MATCHED THEN
    INSERT (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (s.AppCode, s.ResourceID, s.ResourceName, 'MENU', s.MenuPath, s.MenuGroupCode, s.DispSeq, s.Remark, '1', 0, @Now, @Now, @Op)
WHEN MATCHED THEN
    UPDATE SET ResourceName=s.ResourceName, MenuPath=s.MenuPath, MenuGroupCode=s.MenuGroupCode, DispSeq=s.DispSeq, Remark=s.Remark, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op;
DROP TABLE #ResSeed;

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='DASH')
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('DASH','FRAME',N'仪表盘配置',35,'1',0,@Now,@Now,@Op);
;MERGE dbo.Tbl_E_Resource AS t USING (VALUES
    ('RES.DASH.Board',       N'我的工作台',       '/EDashBoard/Index',              'DASH', 10),
    ('RES.DASH.Indicator',   N'业务指标库',       '/EDashIndicator/Index',          'DASH', 20),
    ('RES.DASH.PosPerm',     N'岗位指标授权',     '/EDashPosIndicatorPerm/Index',   'DASH', 30),
    ('RES.DASH.PosTemplate', N'岗位仪表盘模板', '/EDashPosTemplate/Index',        'DASH', 40)
) AS s(ResourceID, ResourceName, MenuPath, MenuGroupCode, DispSeq)
ON t.ResourceID=s.ResourceID
WHEN NOT MATCHED THEN INSERT (AppCode,ResourceID,ResourceName,ResourceType,MenuPath,MenuGroupCode,DispSeq,BStatus,IsDeleted,CreateDate,AmendDate,Operator)
    VALUES ('FRAME',s.ResourceID,s.ResourceName,'MENU',s.MenuPath,s.MenuGroupCode,s.DispSeq,'1',0,@Now,@Now,@Op)
WHEN MATCHED THEN UPDATE SET ResourceName=s.ResourceName,MenuPath=s.MenuPath,MenuGroupCode=s.MenuGroupCode,DispSeq=s.DispSeq,BStatus='1',IsDeleted=0,AmendDate=@Now,Operator=@Op;

/* ----- 职责订阅 ----- */

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.Home','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.Home', 1, '111111', 10, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EUsers','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EUsers', 1, '111111', 20, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EDept','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EDept', 1, '111111', 30, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='EVENT'
      AND ISNULL(s.EventCode,'')=ISNULL('FRAME.DEMO.SUBMIT','')
      AND ISNULL(s.ResourceID,'')=ISNULL('','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'EVENT', 'FRAME.DEMO.SUBMIT', NULL, 1, NULL, 40, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='EVENT'
      AND ISNULL(s.EventCode,'')=ISNULL('FRAME.DEMO.APPROVED','')
      AND ISNULL(s.ResourceID,'')=ISNULL('','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'EVENT', 'FRAME.DEMO.APPROVED', NULL, 1, NULL, 41, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EDictQuery','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EDictQuery', 1, '111111', 45, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EDictType','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EDictType', 1, '111111', 47, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EAppModule','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EAppModule', 1, '111111', 55, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EAppModuleMnt','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EAppModuleMnt', 1, '111111', 57, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EEventInst','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EEventInst', 1, '111111', 65, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EventDemo','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EventDemo', 1, '111111', 75, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EMenuGroup','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EMenuGroup', 1, '111111', 15, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EResource','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EResource', 1, '111111', 25, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.ESubscription','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.ESubscription', 1, '111111', 35, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EResourcePermission','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EResourcePermission', 1, '111111', 45, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EPosition','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EPosition', 1, '111111', 120, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EDuty','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EDuty', 1, '111111', 130, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EUserPosition','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EUserPosition', 1, '111111', 140, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EPositionDuty','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EPositionDuty', 1, '111111', 150, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EManagerSubordinate','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EManagerSubordinate', 1, '111111', 160, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EUserHandover','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EUserHandover', 1, '111111', 170, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EMember','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EMember', 1, '111111', 180, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EEventConfig','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EEventConfig', 1, '111111', 210, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EEventFlowRule','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EEventFlowRule', 1, '111111', 220, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.EEventLog','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.EEventLog', 1, '111111', 230, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.ETodoTask','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.ETodoTask', 1, '111111', 240, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.CF.ELoginLog','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.CF.ELoginLog', 1, '111111', 310, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND s.SubType='EVENT'
      AND ISNULL(s.EventCode,'')=ISNULL('FRAME.LEAVE.SUBMIT','')
      AND ISNULL(s.ResourceID,'')=ISNULL('','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'EVENT', 'FRAME.LEAVE.SUBMIT', NULL, 1, NULL, 10, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_HR_EMP' AND s.SubType='EVENT'
      AND ISNULL(s.EventCode,'')=ISNULL('FRAME.LEAVE.APPROVED','')
      AND ISNULL(s.ResourceID,'')=ISNULL('','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'EVENT', 'FRAME.LEAVE.APPROVED', NULL, 1, NULL, 10, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_EMP';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_HR_EMP' AND s.SubType='EVENT'
      AND ISNULL(s.EventCode,'')=ISNULL('FRAME.LEAVE.REJECTED','')
      AND ISNULL(s.ResourceID,'')=ISNULL('','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'EVENT', 'FRAME.LEAVE.REJECTED', NULL, 1, NULL, 11, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_EMP';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='EVENT'
      AND ISNULL(s.EventCode,'')=ISNULL('FRAME.LEAVE.SUBMIT','')
      AND ISNULL(s.ResourceID,'')=ISNULL('','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'EVENT', 'FRAME.LEAVE.SUBMIT', NULL, 0, NULL, 15, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.Home','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.Home', 1, '111111', 10, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.LeaveApply','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.LeaveApply', 1, '111111', 20, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.LeaveApproval','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.LeaveApproval', 1, '111000', 40, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.LeaveEvents','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.LeaveEvents', 1, '111111', 70, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.Member','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.Member', 1, '111111', 60, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.MyLeave','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.MyLeave', 1, '111111', 30, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.MyTodo','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.MyTodo', 1, '111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.Home','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.Home', 1, '111111', 10, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.LeaveApply','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.LeaveApply', 1, '111111', 20, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.LeaveApproval','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.LeaveApproval', 1, '111000', 40, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.LeaveEvents','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.LeaveEvents', 1, '111111', 70, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.Member','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.Member', 1, '111111', 60, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.MyLeave','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.MyLeave', 1, '111111', 30, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.MyTodo','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.MyTodo', 1, '111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_HR_EMP' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.Home','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.Home', 1, '111111', 10, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_EMP';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_HR_EMP' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.LeaveApply','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.LeaveApply', 1, '111111', 20, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_EMP';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_HR_EMP' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.MyLeave','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.MyLeave', 1, '111111', 30, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_EMP';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'CF_HR_EMP' AND s.SubType='RESOURCE'
      AND ISNULL(s.EventCode,'')=ISNULL('','')
      AND ISNULL(s.ResourceID,'')=ISNULL('RES.HR.MyTodo','') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', NULL, 'RES.HR.MyTodo', 1, '111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_EMP';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_DASH_ADMIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'CF_DASH_ADMIN', N'仪表盘配置职责', 'SERVICE', 20, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_PositionDuty pd INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID WHERE p.PostCode=N'CF_ADMIN' AND d.DutyCode=N'CF_DASH_ADMIN' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111111', 8, '1', 0, @Now, @Now, @Op FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d WHERE p.PostCode=N'CF_ADMIN' AND d.DutyCode=N'CF_DASH_ADMIN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID='RES.DASH.Board' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', 'RES.DASH.Board', 1, '110000', 50, '1', 0, @Now, @Now, @Op FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID='RES.DASH.Indicator' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', 'RES.DASH.Indicator', 1, '111111', 50, '1', 0, @Now, @Now, @Op FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID='RES.DASH.PosPerm' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', 'RES.DASH.PosPerm', 1, '111111', 50, '1', 0, @Now, @Now, @Op FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID='RES.DASH.PosTemplate' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', 'RES.DASH.PosTemplate', 1, '111111', 50, '1', 0, @Now, @Now, @Op FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID WHERE d.DutyCode=N'CF_VIEWER' AND s.SubType='RESOURCE' AND s.ResourceID='RES.DASH.Board' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', 'RES.DASH.Board', 1, '110000', 50, '1', 0, @Now, @Now, @Op FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_VIEWER';

/* ----- 资源按钮权限 ----- */

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.Home')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.Home', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.Home';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EUsers')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EUsers', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EUsers';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EDept')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EDept', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EDept';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EDictQuery')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EDictQuery', 0, 0, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=0, CanUpdate=0, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EDictQuery';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EDictType')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EDictType', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EDictType';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EAppModule')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EAppModule', 0, 0, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=0, CanUpdate=0, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EAppModule';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EAppModuleMnt')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EAppModuleMnt', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EAppModuleMnt';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EEventInst')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EEventInst', 0, 0, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=0, CanUpdate=0, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EEventInst';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EventDemo')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EventDemo', 0, 0, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=0, CanUpdate=0, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EventDemo';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EMenuGroup')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EMenuGroup', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EMenuGroup';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EResource')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EResource', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EResource';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.ESubscription')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.ESubscription', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.ESubscription';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EResourcePermission')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EResourcePermission', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EResourcePermission';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EPosition')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EPosition', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EPosition';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EDuty')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EDuty', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EDuty';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EUserPosition')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EUserPosition', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EUserPosition';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EPositionDuty')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EPositionDuty', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EPositionDuty';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EManagerSubordinate')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EManagerSubordinate', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EManagerSubordinate';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EUserHandover')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EUserHandover', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EUserHandover';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EMember')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EMember', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EMember';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EEventConfig')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EEventConfig', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EEventConfig';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EEventFlowRule')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EEventFlowRule', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EEventFlowRule';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EEventLog')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.EEventLog', 0, 0, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=0, CanUpdate=0, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.EEventLog';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.ETodoTask')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.ETodoTask', 0, 1, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=0, CanUpdate=1, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.ETodoTask';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.ELoginLog')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.CF.ELoginLog', 0, 0, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=0, CanUpdate=0, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.CF.ELoginLog';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.HR.Home')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.Home', 1, 1, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.HR.Home';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.HR.LeaveApply')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.LeaveApply', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.HR.LeaveApply';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.HR.LeaveApproval')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.LeaveApproval', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.HR.LeaveApproval';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.HR.LeaveEvents')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.LeaveEvents', 0, 0, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=0, CanUpdate=0, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.HR.LeaveEvents';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.HR.Member')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.Member', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.HR.Member';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.HR.MyLeave')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.MyLeave', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.HR.MyLeave';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.HR.MyTodo')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.MyTodo', 0, 1, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';
ELSE
    UPDATE rp SET CanCreate=0, CanUpdate=1, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_ADMIN' AND rp.ResourceID='RES.HR.MyTodo';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND rp.ResourceID='RES.HR.Home')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.Home', 1, 1, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND rp.ResourceID='RES.HR.Home';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND rp.ResourceID='RES.HR.LeaveApply')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.LeaveApply', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND rp.ResourceID='RES.HR.LeaveApply';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND rp.ResourceID='RES.HR.LeaveApproval')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.LeaveApproval', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND rp.ResourceID='RES.HR.LeaveApproval';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND rp.ResourceID='RES.HR.LeaveEvents')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.LeaveEvents', 0, 0, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';
ELSE
    UPDATE rp SET CanCreate=0, CanUpdate=0, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND rp.ResourceID='RES.HR.LeaveEvents';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND rp.ResourceID='RES.HR.Member')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.Member', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND rp.ResourceID='RES.HR.Member';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND rp.ResourceID='RES.HR.MyLeave')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.MyLeave', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND rp.ResourceID='RES.HR.MyLeave';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND rp.ResourceID='RES.HR.MyTodo')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.MyTodo', 0, 1, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_MGR';
ELSE
    UPDATE rp SET CanCreate=0, CanUpdate=1, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_MGR' AND rp.ResourceID='RES.HR.MyTodo';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_EMP' AND rp.ResourceID='RES.HR.Home')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.Home', 1, 1, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_EMP';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_EMP' AND rp.ResourceID='RES.HR.Home';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_EMP' AND rp.ResourceID='RES.HR.LeaveApply')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.LeaveApply', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_EMP';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_EMP' AND rp.ResourceID='RES.HR.LeaveApply';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_EMP' AND rp.ResourceID='RES.HR.MyLeave')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.MyLeave', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_EMP';
ELSE
    UPDATE rp SET CanCreate=1, CanUpdate=1, CanDelete=1, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_EMP' AND rp.ResourceID='RES.HR.MyLeave';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_EMP' AND rp.ResourceID='RES.HR.MyTodo')
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'RES.HR.MyTodo', 0, 1, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_HR_EMP';
ELSE
    UPDATE rp SET CanCreate=0, CanUpdate=1, CanDelete=0, CanQuery=1, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode=N'CF_HR_EMP' AND rp.ResourceID='RES.HR.MyTodo';

PRINT N'23-Seed_Menus 完成。';
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'23-Seed_Menus.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'23-Seed_Menus.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'23-Seed_Menus.sql');
GO
