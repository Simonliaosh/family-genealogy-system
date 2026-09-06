/* 库名统一为 FamilyTree：本脚本原先没有 USE，会落在执行工具当时选中的库上。 */
USE [FamilyTree];
GO

/*
==============================================================================
  EFrame 种子 01 - 基础数据（应用模块/菜单组/字典）
==============================================================================
  来源：EFrame.xls + EFrame 架构对齐（幂等 MERGE / IF NOT EXISTS）
  前置：docs/EFrame_CreateTables.sql、docs/EFrame_v2_supplement.sql
  顺序：第 1 步
  编码：ANSI (GBK)
==============================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-EFRAME';

/* ----- 应用模块 ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_AppModule WHERE AppCode = 'FRAME')
    INSERT INTO dbo.Tbl_E_AppModule (AppCode, AppName, AppType, BaseUrl, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FRAME', N'EFrame 管理框架', 'FRAMEWORK', NULL, 99, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_AppModule SET AppName=N'EFrame 管理框架', AppType='FRAMEWORK',
        BaseUrl=NULL, DispSeq=99, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='FRAME';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_AppModule WHERE AppCode = 'CRM')
    INSERT INTO dbo.Tbl_E_AppModule (AppCode, AppName, AppType, BaseUrl, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('CRM', N'客户关系（演示）', 'BUSINESS', N'https://crm.example.local', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_AppModule SET AppName=N'客户关系（演示）', AppType='BUSINESS',
        BaseUrl=N'https://crm.example.local', DispSeq=10, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='CRM';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_AppModule WHERE AppCode = 'OA')
    INSERT INTO dbo.Tbl_E_AppModule (AppCode, AppName, AppType, BaseUrl, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('OA', N'办公自动化（演示）', 'BUSINESS', N'https://oa.example.local', 20, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_AppModule SET AppName=N'办公自动化（演示）', AppType='BUSINESS',
        BaseUrl=N'https://oa.example.local', DispSeq=20, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='OA';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_AppModule WHERE AppCode = 'HR')
    INSERT INTO dbo.Tbl_E_AppModule (AppCode, AppName, AppType, BaseUrl, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('HR', N'人事管理', 'BUSINESS', N'/HrHome/Index', 15, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_AppModule SET AppName=N'人事管理', AppType='BUSINESS',
        BaseUrl=N'/HrHome/Index', DispSeq=15, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='HR';

/* ----- 菜单组 ----- */

;MERGE dbo.Tbl_E_MenuGroup AS t
USING (VALUES
    ('EVT', 'FRAME', N'事件管理', 30),
    ('HR', 'FRAME', N'人事管理', 25),
    ('LOG', 'FRAME', N'日志查询', 40),
    ('ORG', 'FRAME', N'组织架构', 20),
    ('SYS', 'FRAME', N'系统管理', 10)
) AS s (MenuGroupCode, AppCode, MenuGroupName, DispSeq)
ON t.MenuGroupCode = s.MenuGroupCode
WHEN NOT MATCHED THEN
    INSERT (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (s.MenuGroupCode, s.AppCode, s.MenuGroupName, s.DispSeq, '1', 0, @Now, @Now, @Op)
WHEN MATCHED THEN
    UPDATE SET AppCode=s.AppCode, MenuGroupName=s.MenuGroupName, DispSeq=s.DispSeq, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op;

/* ----- 字典类型 ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'ACTION_CODE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('ACTION_CODE', N'资源动作编码', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'资源动作编码', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='ACTION_CODE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'APP_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('APP_TYPE', N'应用类型', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'应用类型', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='APP_TYPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'BSTATUS')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('BSTATUS', N'业务状态', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'业务状态', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='BSTATUS';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'CANDIDATE_STATUS')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('CANDIDATE_STATUS', N'候选人状态', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'候选人状态', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='CANDIDATE_STATUS';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'CHANNEL')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('CHANNEL', N'通知渠道', 'FRAME', 1, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'通知渠道', AppCode='FRAME',
        IsSystem=1, IsEditable=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='CHANNEL';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'DATA_SCOPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DATA_SCOPE', N'数据范围', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'数据范围', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DATA_SCOPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'DELEGATE_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DELEGATE_TYPE', N'委托类型', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'委托类型', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DELEGATE_TYPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'DELIVERY_STATUS')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DELIVERY_STATUS', N'投递状态', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'投递状态', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DELIVERY_STATUS';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'DEPT_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DEPT_TYPE', N'部门类型', 'FRAME', 1, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'部门类型', AppCode='FRAME',
        IsSystem=1, IsEditable=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DEPT_TYPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'DUTY_CATEGORY')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DUTY_CATEGORY', N'职责分类', 'FRAME', 1, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'职责分类', AppCode='FRAME',
        IsSystem=1, IsEditable=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DUTY_CATEGORY';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'EVENT_ACTION')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_ACTION', N'事件日志动作', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'事件日志动作', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_ACTION';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'EVENT_STATUS')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_STATUS', N'事件实例状态', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'事件实例状态', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_STATUS';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'EVENT_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_TYPE', N'事件分类', 'FRAME', 1, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'事件分类', AppCode='FRAME',
        IsSystem=1, IsEditable=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_TYPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'EXEC_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EXEC_TYPE', N'事件执行类型', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'事件执行类型', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EXEC_TYPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'FLOW_ACTION')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('FLOW_ACTION', N'流转动作类型', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'流转动作类型', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='FLOW_ACTION';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'GROUP_STATUS')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('GROUP_STATUS', N'待办组状态', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'待办组状态', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='GROUP_STATUS';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'HANDLE_MODE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HANDLE_MODE', N'处理模式', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'处理模式', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HANDLE_MODE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'HANDOVER_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HANDOVER_TYPE', N'交接类型', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'交接类型', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HANDOVER_TYPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'HR_LEAVE_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HR_LEAVE_TYPE', N'请假假别', 'FRAME', 1, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'请假假别', AppCode='FRAME',
        IsSystem=1, IsEditable=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HR_LEAVE_TYPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'MEMBER_EDU_LEVEL')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_EDU_LEVEL', N'学历', 'FRAME', 1, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'学历', AppCode='FRAME',
        IsSystem=1, IsEditable=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_EDU_LEVEL';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'MEMBER_HEALTH')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_HEALTH', N'健康状况', 'FRAME', 1, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'健康状况', AppCode='FRAME',
        IsSystem=1, IsEditable=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_HEALTH';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'MEMBER_PER_GRADE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_PER_GRADE', N'人员职级', 'FRAME', 1, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'人员职级', AppCode='FRAME',
        IsSystem=1, IsEditable=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_PER_GRADE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'MEMBER_SEX')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_SEX', N'人员性别', 'FRAME', 1, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'人员性别', AppCode='FRAME',
        IsSystem=1, IsEditable=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_SEX';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'POSITION_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('POSITION_TYPE', N'岗位类型', 'FRAME', 1, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'岗位类型', AppCode='FRAME',
        IsSystem=1, IsEditable=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='POSITION_TYPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'PWD_ALGO')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('PWD_ALGO', N'密码算法', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'密码算法', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='PWD_ALGO';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'RELATION_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RELATION_TYPE', N'上下级关系类型', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'上下级关系类型', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RELATION_TYPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'RESOLVE_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RESOLVE_TYPE', N'接收人解析方式', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'接收人解析方式', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RESOLVE_TYPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'RESOURCE_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RESOURCE_TYPE', N'资源类型', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'资源类型', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RESOURCE_TYPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'SCOPE_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SCOPE_TYPE', N'数据范围类型', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'数据范围类型', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SCOPE_TYPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'SUB_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SUB_TYPE', N'订阅类型', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'订阅类型', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SUB_TYPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'SUBJECT_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SUBJECT_TYPE', N'授权主体类型', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'授权主体类型', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SUBJECT_TYPE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'TARGET_RESOLVE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TARGET_RESOLVE', N'处理人解析方式', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'处理人解析方式', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TARGET_RESOLVE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'TODO_ACTION')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_ACTION', N'待办动作类型', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'待办动作类型', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_ACTION';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'TODO_PRIORITY')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_PRIORITY', N'待办优先级', 'FRAME', 1, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'待办优先级', AppCode='FRAME',
        IsSystem=1, IsEditable=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_PRIORITY';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'TODO_STATUS')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_STATUS', N'待办状态', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'待办状态', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_STATUS';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = 'USER_TYPE')
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('USER_TYPE', N'用户类型', 'FRAME', 1, 0, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName=N'用户类型', AppCode='FRAME',
        IsSystem=1, IsEditable=0, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='USER_TYPE';

/* ----- 字典项 ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='APP_TYPE' AND ItemCode=N'FRAMEWORK')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('APP_TYPE', N'FRAMEWORK', N'框架应用', N'Framework', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'框架应用', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='APP_TYPE' AND ItemCode=N'FRAMEWORK';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='APP_TYPE' AND ItemCode=N'BUSINESS')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('APP_TYPE', N'BUSINESS', N'业务应用', N'Business', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'业务应用', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='APP_TYPE' AND ItemCode=N'BUSINESS';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='APP_TYPE' AND ItemCode=N'PLUGIN')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('APP_TYPE', N'PLUGIN', N'插件', N'Plugin', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'插件', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='APP_TYPE' AND ItemCode=N'PLUGIN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='BSTATUS' AND ItemCode=N'1')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('BSTATUS', N'1', N'启用', N'Enabled', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'启用', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='BSTATUS' AND ItemCode=N'1';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='BSTATUS' AND ItemCode=N'2')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('BSTATUS', N'2', N'停用', N'Disabled', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'停用', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='BSTATUS' AND ItemCode=N'2';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DEPT_TYPE' AND ItemCode=N'COMPANY')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DEPT_TYPE', N'COMPANY', N'公司', N'Company', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'公司', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DEPT_TYPE' AND ItemCode=N'COMPANY';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DEPT_TYPE' AND ItemCode=N'DEPT')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DEPT_TYPE', N'DEPT', N'部门', N'Department', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'部门', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DEPT_TYPE' AND ItemCode=N'DEPT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DEPT_TYPE' AND ItemCode=N'TEAM')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DEPT_TYPE', N'TEAM', N'团队', N'Team', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'团队', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DEPT_TYPE' AND ItemCode=N'TEAM';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DEPT_TYPE' AND ItemCode=N'STORE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DEPT_TYPE', N'STORE', N'门店', N'Store', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'门店', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DEPT_TYPE' AND ItemCode=N'STORE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='POSITION_TYPE' AND ItemCode=N'MANAGER')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('POSITION_TYPE', N'MANAGER', N'管理岗', N'Manager', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'管理岗', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='POSITION_TYPE' AND ItemCode=N'MANAGER';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='POSITION_TYPE' AND ItemCode=N'SALES')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('POSITION_TYPE', N'SALES', N'销售岗', N'Sales', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'销售岗', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='POSITION_TYPE' AND ItemCode=N'SALES';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='POSITION_TYPE' AND ItemCode=N'SERVICE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('POSITION_TYPE', N'SERVICE', N'服务岗', N'Service', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'服务岗', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='POSITION_TYPE' AND ItemCode=N'SERVICE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='POSITION_TYPE' AND ItemCode=N'FINANCE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('POSITION_TYPE', N'FINANCE', N'财务岗', N'Finance', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'财务岗', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='POSITION_TYPE' AND ItemCode=N'FINANCE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DATA_SCOPE' AND ItemCode=N'SELF')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DATA_SCOPE', N'SELF', N'仅本人', N'Self Only', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'仅本人', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DATA_SCOPE' AND ItemCode=N'SELF';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DATA_SCOPE' AND ItemCode=N'DEPT')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DATA_SCOPE', N'DEPT', N'本部门', N'Own Dept', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'本部门', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DATA_SCOPE' AND ItemCode=N'DEPT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DATA_SCOPE' AND ItemCode=N'DEPT_TREE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DATA_SCOPE', N'DEPT_TREE', N'本部门及下级', N'Dept & Sub', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'本部门及下级', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DATA_SCOPE' AND ItemCode=N'DEPT_TREE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DATA_SCOPE' AND ItemCode=N'ALL')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DATA_SCOPE', N'ALL', N'全部', N'All', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'全部', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DATA_SCOPE' AND ItemCode=N'ALL';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DATA_SCOPE' AND ItemCode=N'CUSTOM')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DATA_SCOPE', N'CUSTOM', N'自定义', N'Custom', NULL, 5, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'自定义', DispSeq=5, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DATA_SCOPE' AND ItemCode=N'CUSTOM';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DUTY_CATEGORY' AND ItemCode=N'APPROVAL')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DUTY_CATEGORY', N'APPROVAL', N'审批', N'Approval', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'审批', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DUTY_CATEGORY' AND ItemCode=N'APPROVAL';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DUTY_CATEGORY' AND ItemCode=N'SALES')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DUTY_CATEGORY', N'SALES', N'销售', N'Sales', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'销售', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DUTY_CATEGORY' AND ItemCode=N'SALES';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DUTY_CATEGORY' AND ItemCode=N'SERVICE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DUTY_CATEGORY', N'SERVICE', N'服务', N'Service', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'服务', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DUTY_CATEGORY' AND ItemCode=N'SERVICE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DUTY_CATEGORY' AND ItemCode=N'FINANCE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DUTY_CATEGORY', N'FINANCE', N'财务', N'Finance', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'财务', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DUTY_CATEGORY' AND ItemCode=N'FINANCE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='USER_TYPE' AND ItemCode=N'EMPLOYEE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('USER_TYPE', N'EMPLOYEE', N'员工', N'Employee', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'员工', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='USER_TYPE' AND ItemCode=N'EMPLOYEE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='USER_TYPE' AND ItemCode=N'CUSTOMER')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('USER_TYPE', N'CUSTOMER', N'客户', N'Customer', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'客户', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='USER_TYPE' AND ItemCode=N'CUSTOMER';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='USER_TYPE' AND ItemCode=N'SUPPLIER')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('USER_TYPE', N'SUPPLIER', N'供应商', N'Supplier', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'供应商', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='USER_TYPE' AND ItemCode=N'SUPPLIER';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='USER_TYPE' AND ItemCode=N'PARTNER')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('USER_TYPE', N'PARTNER', N'合作伙伴', N'Partner', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'合作伙伴', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='USER_TYPE' AND ItemCode=N'PARTNER';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='PWD_ALGO' AND ItemCode=N'MD5_16')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('PWD_ALGO', N'MD5_16', N'MD5 16位', N'MD5-16', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'MD5 16位', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='PWD_ALGO' AND ItemCode=N'MD5_16';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='PWD_ALGO' AND ItemCode=N'PBKDF2')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('PWD_ALGO', N'PBKDF2', N'PBKDF2', N'PBKDF2', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'PBKDF2', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='PWD_ALGO' AND ItemCode=N'PBKDF2';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='PWD_ALGO' AND ItemCode=N'BCrypt')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('PWD_ALGO', N'BCrypt', N'BCrypt', N'BCrypt', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'BCrypt', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='PWD_ALGO' AND ItemCode=N'BCrypt';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DELEGATE_TYPE' AND ItemCode=N'TODO')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DELEGATE_TYPE', N'TODO', N'待办委托', N'Todo', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'待办委托', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DELEGATE_TYPE' AND ItemCode=N'TODO';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DELEGATE_TYPE' AND ItemCode=N'APPROVAL')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DELEGATE_TYPE', N'APPROVAL', N'审批委托', N'Approval', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'审批委托', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DELEGATE_TYPE' AND ItemCode=N'APPROVAL';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DELEGATE_TYPE' AND ItemCode=N'NOTICE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DELEGATE_TYPE', N'NOTICE', N'通知委托', N'Notice', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'通知委托', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DELEGATE_TYPE' AND ItemCode=N'NOTICE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DELEGATE_TYPE' AND ItemCode=N'ALL')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DELEGATE_TYPE', N'ALL', N'全部委托', N'All', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'全部委托', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DELEGATE_TYPE' AND ItemCode=N'ALL';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HANDOVER_TYPE' AND ItemCode=N'LEAVE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HANDOVER_TYPE', N'LEAVE', N'请假', N'Leave', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'请假', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HANDOVER_TYPE' AND ItemCode=N'LEAVE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HANDOVER_TYPE' AND ItemCode=N'TRANSFER')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HANDOVER_TYPE', N'TRANSFER', N'调岗', N'Transfer', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'调岗', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HANDOVER_TYPE' AND ItemCode=N'TRANSFER';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HANDOVER_TYPE' AND ItemCode=N'RESIGN')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HANDOVER_TYPE', N'RESIGN', N'离职', N'Resign', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'离职', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HANDOVER_TYPE' AND ItemCode=N'RESIGN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HANDOVER_TYPE' AND ItemCode=N'TEMP')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HANDOVER_TYPE', N'TEMP', N'临时', N'Temp', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'临时', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HANDOVER_TYPE' AND ItemCode=N'TEMP';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='RELATION_TYPE' AND ItemCode=N'DIRECT')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RELATION_TYPE', N'DIRECT', N'直接汇报', N'Direct', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'直接汇报', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RELATION_TYPE' AND ItemCode=N'DIRECT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='RELATION_TYPE' AND ItemCode=N'MATRIX')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RELATION_TYPE', N'MATRIX', N'矩阵汇报', N'Matrix', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'矩阵汇报', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RELATION_TYPE' AND ItemCode=N'MATRIX';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='RELATION_TYPE' AND ItemCode=N'TEMP')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RELATION_TYPE', N'TEMP', N'临时汇报', N'Temp', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'临时汇报', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RELATION_TYPE' AND ItemCode=N'TEMP';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='RESOURCE_TYPE' AND ItemCode=N'PAGE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RESOURCE_TYPE', N'PAGE', N'页面', N'Page', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'页面', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RESOURCE_TYPE' AND ItemCode=N'PAGE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='RESOURCE_TYPE' AND ItemCode=N'GROUP')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RESOURCE_TYPE', N'GROUP', N'分组', N'Group', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'分组', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RESOURCE_TYPE' AND ItemCode=N'GROUP';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='RESOURCE_TYPE' AND ItemCode=N'MENU')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RESOURCE_TYPE', N'MENU', N'菜单', N'Menu', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'菜单', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RESOURCE_TYPE' AND ItemCode=N'MENU';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='RESOURCE_TYPE' AND ItemCode=N'BUTTON')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RESOURCE_TYPE', N'BUTTON', N'按钮', N'Button', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'按钮', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RESOURCE_TYPE' AND ItemCode=N'BUTTON';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='RESOURCE_TYPE' AND ItemCode=N'API')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RESOURCE_TYPE', N'API', N'API', N'API', NULL, 5, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'API', DispSeq=5, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RESOURCE_TYPE' AND ItemCode=N'API';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='ACTION_CODE' AND ItemCode=N'QUERY')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('ACTION_CODE', N'QUERY', N'查询', N'Query', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'查询', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='ACTION_CODE' AND ItemCode=N'QUERY';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='ACTION_CODE' AND ItemCode=N'CREATE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('ACTION_CODE', N'CREATE', N'新增', N'Create', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'新增', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='ACTION_CODE' AND ItemCode=N'CREATE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='ACTION_CODE' AND ItemCode=N'UPDATE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('ACTION_CODE', N'UPDATE', N'修改', N'Update', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'修改', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='ACTION_CODE' AND ItemCode=N'UPDATE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='ACTION_CODE' AND ItemCode=N'DELETE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('ACTION_CODE', N'DELETE', N'删除', N'Delete', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'删除', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='ACTION_CODE' AND ItemCode=N'DELETE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='ACTION_CODE' AND ItemCode=N'APPROVE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('ACTION_CODE', N'APPROVE', N'审批', N'Approve', NULL, 5, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'审批', DispSeq=5, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='ACTION_CODE' AND ItemCode=N'APPROVE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='ACTION_CODE' AND ItemCode=N'EXPORT')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('ACTION_CODE', N'EXPORT', N'导出', N'Export', NULL, 6, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'导出', DispSeq=6, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='ACTION_CODE' AND ItemCode=N'EXPORT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='ACTION_CODE' AND ItemCode=N'IMPORT')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('ACTION_CODE', N'IMPORT', N'导入', N'Import', NULL, 7, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'导入', DispSeq=7, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='ACTION_CODE' AND ItemCode=N'IMPORT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='SCOPE_TYPE' AND ItemCode=N'SELF')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SCOPE_TYPE', N'SELF', N'仅本人', N'Self', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'仅本人', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SCOPE_TYPE' AND ItemCode=N'SELF';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='SCOPE_TYPE' AND ItemCode=N'DEPT')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SCOPE_TYPE', N'DEPT', N'本部门', N'Dept', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'本部门', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SCOPE_TYPE' AND ItemCode=N'DEPT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='SCOPE_TYPE' AND ItemCode=N'DEPT_TREE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SCOPE_TYPE', N'DEPT_TREE', N'本部门及下级', N'Dept Tree', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'本部门及下级', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SCOPE_TYPE' AND ItemCode=N'DEPT_TREE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='SCOPE_TYPE' AND ItemCode=N'TEAM')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SCOPE_TYPE', N'TEAM', N'团队', N'Team', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'团队', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SCOPE_TYPE' AND ItemCode=N'TEAM';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='SCOPE_TYPE' AND ItemCode=N'ALL')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SCOPE_TYPE', N'ALL', N'全部', N'All', NULL, 5, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'全部', DispSeq=5, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SCOPE_TYPE' AND ItemCode=N'ALL';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='SCOPE_TYPE' AND ItemCode=N'CUSTOM')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SCOPE_TYPE', N'CUSTOM', N'自定义', N'Custom', NULL, 6, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'自定义', DispSeq=6, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SCOPE_TYPE' AND ItemCode=N'CUSTOM';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='SUBJECT_TYPE' AND ItemCode=N'USER')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SUBJECT_TYPE', N'USER', N'用户', N'User', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'用户', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SUBJECT_TYPE' AND ItemCode=N'USER';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='SUBJECT_TYPE' AND ItemCode=N'POSITION')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SUBJECT_TYPE', N'POSITION', N'岗位', N'Position', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'岗位', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SUBJECT_TYPE' AND ItemCode=N'POSITION';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='SUBJECT_TYPE' AND ItemCode=N'DUTY')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SUBJECT_TYPE', N'DUTY', N'职责', N'Duty', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'职责', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SUBJECT_TYPE' AND ItemCode=N'DUTY';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='SUBJECT_TYPE' AND ItemCode=N'DEPT')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SUBJECT_TYPE', N'DEPT', N'部门', N'Dept', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'部门', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SUBJECT_TYPE' AND ItemCode=N'DEPT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EXEC_TYPE' AND ItemCode=N'SYNC')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EXEC_TYPE', N'SYNC', N'同步执行', N'Sync', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'同步执行', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EXEC_TYPE' AND ItemCode=N'SYNC';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EXEC_TYPE' AND ItemCode=N'ASYNC')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EXEC_TYPE', N'ASYNC', N'异步执行', N'Async', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'异步执行', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EXEC_TYPE' AND ItemCode=N'ASYNC';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EXEC_TYPE' AND ItemCode=N'MANUAL')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EXEC_TYPE', N'MANUAL', N'手动触发', N'Manual', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'手动触发', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EXEC_TYPE' AND ItemCode=N'MANUAL';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HANDLE_MODE' AND ItemCode=N'SINGLE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HANDLE_MODE', N'SINGLE', N'单人处理', N'Single', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'单人处理', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HANDLE_MODE' AND ItemCode=N'SINGLE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HANDLE_MODE' AND ItemCode=N'ALL')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HANDLE_MODE', N'ALL', N'全部会签', N'All Sign', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'全部会签', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HANDLE_MODE' AND ItemCode=N'ALL';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HANDLE_MODE' AND ItemCode=N'ANY')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HANDLE_MODE', N'ANY', N'任一处理', N'Any One', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'任一处理', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HANDLE_MODE' AND ItemCode=N'ANY';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HANDLE_MODE' AND ItemCode=N'CLAIM')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HANDLE_MODE', N'CLAIM', N'抢单', N'Claim', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'抢单', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HANDLE_MODE' AND ItemCode=N'CLAIM';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EVENT_TYPE' AND ItemCode=N'APPROVAL')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_TYPE', N'APPROVAL', N'审批事件', N'Approval', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'审批事件', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_TYPE' AND ItemCode=N'APPROVAL';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EVENT_TYPE' AND ItemCode=N'NOTICE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_TYPE', N'NOTICE', N'通知事件', N'Notice', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'通知事件', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_TYPE' AND ItemCode=N'NOTICE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EVENT_TYPE' AND ItemCode=N'TASK')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_TYPE', N'TASK', N'任务事件', N'Task', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'任务事件', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_TYPE' AND ItemCode=N'TASK';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EVENT_TYPE' AND ItemCode=N'SYSTEM')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_TYPE', N'SYSTEM', N'系统事件', N'System', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'系统事件', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_TYPE' AND ItemCode=N'SYSTEM';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EVENT_STATUS' AND ItemCode=N'NEW')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_STATUS', N'NEW', N'待处理', N'New', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'待处理', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_STATUS' AND ItemCode=N'NEW';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EVENT_STATUS' AND ItemCode=N'PROCESSING')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_STATUS', N'PROCESSING', N'处理中', N'Processing', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'处理中', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_STATUS' AND ItemCode=N'PROCESSING';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EVENT_STATUS' AND ItemCode=N'DONE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_STATUS', N'DONE', N'已完成', N'Done', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'已完成', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_STATUS' AND ItemCode=N'DONE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EVENT_STATUS' AND ItemCode=N'FAILED')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_STATUS', N'FAILED', N'失败', N'Failed', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'失败', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_STATUS' AND ItemCode=N'FAILED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EVENT_STATUS' AND ItemCode=N'CANCELLED')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_STATUS', N'CANCELLED', N'已取消', N'Cancelled', NULL, 5, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'已取消', DispSeq=5, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_STATUS' AND ItemCode=N'CANCELLED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DELIVERY_STATUS' AND ItemCode=N'PENDING')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DELIVERY_STATUS', N'PENDING', N'待投递', N'Pending', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'待投递', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DELIVERY_STATUS' AND ItemCode=N'PENDING';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DELIVERY_STATUS' AND ItemCode=N'SENT')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DELIVERY_STATUS', N'SENT', N'已发送', N'Sent', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'已发送', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DELIVERY_STATUS' AND ItemCode=N'SENT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DELIVERY_STATUS' AND ItemCode=N'FAILED')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DELIVERY_STATUS', N'FAILED', N'失败', N'Failed', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'失败', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DELIVERY_STATUS' AND ItemCode=N'FAILED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='DELIVERY_STATUS' AND ItemCode=N'READ')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('DELIVERY_STATUS', N'READ', N'已读', N'Read', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'已读', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='DELIVERY_STATUS' AND ItemCode=N'READ';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='CHANNEL' AND ItemCode=N'TODO')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('CHANNEL', N'TODO', N'待办', N'Todo', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'待办', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='CHANNEL' AND ItemCode=N'TODO';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='CHANNEL' AND ItemCode=N'MESSAGE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('CHANNEL', N'MESSAGE', N'站内信', N'Message', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'站内信', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='CHANNEL' AND ItemCode=N'MESSAGE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='CHANNEL' AND ItemCode=N'EMAIL')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('CHANNEL', N'EMAIL', N'邮件', N'Email', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'邮件', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='CHANNEL' AND ItemCode=N'EMAIL';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='CHANNEL' AND ItemCode=N'WECHAT')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('CHANNEL', N'WECHAT', N'企业微信', N'WeChat', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'企业微信', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='CHANNEL' AND ItemCode=N'WECHAT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='CHANNEL' AND ItemCode=N'SMS')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('CHANNEL', N'SMS', N'短信', N'SMS', NULL, 5, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'短信', DispSeq=5, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='CHANNEL' AND ItemCode=N'SMS';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_STATUS' AND ItemCode=N'0')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_STATUS', N'0', N'待处理', N'Pending', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'待处理', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_STATUS' AND ItemCode=N'0';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_STATUS' AND ItemCode=N'1')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_STATUS', N'1', N'已处理', N'Done', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'已处理', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_STATUS' AND ItemCode=N'1';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_STATUS' AND ItemCode=N'2')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_STATUS', N'2', N'已关闭', N'Closed', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'已关闭', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_STATUS' AND ItemCode=N'2';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_STATUS' AND ItemCode=N'3')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_STATUS', N'3', N'已转交', N'Transferred', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'已转交', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_STATUS' AND ItemCode=N'3';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_STATUS' AND ItemCode=N'4')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_STATUS', N'4', N'已撤回', N'Revoked', NULL, 5, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'已撤回', DispSeq=5, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_STATUS' AND ItemCode=N'4';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_PRIORITY' AND ItemCode=N'LOW')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_PRIORITY', N'LOW', N'低', N'Low', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'低', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_PRIORITY' AND ItemCode=N'LOW';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_PRIORITY' AND ItemCode=N'NORMAL')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_PRIORITY', N'NORMAL', N'普通', N'Normal', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'普通', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_PRIORITY' AND ItemCode=N'NORMAL';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_PRIORITY' AND ItemCode=N'HIGH')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_PRIORITY', N'HIGH', N'高', N'High', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'高', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_PRIORITY' AND ItemCode=N'HIGH';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_PRIORITY' AND ItemCode=N'URGENT')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_PRIORITY', N'URGENT', N'紧急', N'Urgent', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'紧急', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_PRIORITY' AND ItemCode=N'URGENT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'CREATE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_ACTION', N'CREATE', N'创建', N'Create', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'创建', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'CREATE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'CLAIM')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_ACTION', N'CLAIM', N'领取', N'Claim', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'领取', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'CLAIM';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'TRANSFER')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_ACTION', N'TRANSFER', N'转交', N'Transfer', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'转交', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'TRANSFER';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'RETURN')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_ACTION', N'RETURN', N'退回', N'Return', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'退回', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'RETURN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'FINISH')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_ACTION', N'FINISH', N'完成', N'Finish', NULL, 5, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'完成', DispSeq=5, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'FINISH';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'CLOSE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_ACTION', N'CLOSE', N'关闭', N'Close', NULL, 6, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'关闭', DispSeq=6, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'CLOSE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'REVOKE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_ACTION', N'REVOKE', N'撤回', N'Revoke', NULL, 7, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'撤回', DispSeq=7, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'REVOKE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'REMIND')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_ACTION', N'REMIND', N'催办', N'Remind', NULL, 8, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'催办', DispSeq=8, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'REMIND';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'COSIGN')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TODO_ACTION', N'COSIGN', N'加签', N'Cosign', NULL, 9, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'加签', DispSeq=9, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TODO_ACTION' AND ItemCode=N'COSIGN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='GROUP_STATUS' AND ItemCode=N'PENDING')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('GROUP_STATUS', N'PENDING', N'待处理', N'Pending', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'待处理', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='GROUP_STATUS' AND ItemCode=N'PENDING';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='GROUP_STATUS' AND ItemCode=N'PARTIAL')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('GROUP_STATUS', N'PARTIAL', N'部分完成', N'Partial', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'部分完成', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='GROUP_STATUS' AND ItemCode=N'PARTIAL';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='GROUP_STATUS' AND ItemCode=N'DONE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('GROUP_STATUS', N'DONE', N'全部完成', N'Done', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'全部完成', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='GROUP_STATUS' AND ItemCode=N'DONE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='GROUP_STATUS' AND ItemCode=N'CANCELLED')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('GROUP_STATUS', N'CANCELLED', N'已取消', N'Cancelled', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'已取消', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='GROUP_STATUS' AND ItemCode=N'CANCELLED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='CANDIDATE_STATUS' AND ItemCode=N'WAITING')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('CANDIDATE_STATUS', N'WAITING', N'待领取', N'Waiting', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'待领取', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='CANDIDATE_STATUS' AND ItemCode=N'WAITING';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='CANDIDATE_STATUS' AND ItemCode=N'CLAIMED')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('CANDIDATE_STATUS', N'CLAIMED', N'已领取', N'Claimed', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'已领取', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='CANDIDATE_STATUS' AND ItemCode=N'CLAIMED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='CANDIDATE_STATUS' AND ItemCode=N'EXPIRED')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('CANDIDATE_STATUS', N'EXPIRED', N'已过期', N'Expired', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'已过期', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='CANDIDATE_STATUS' AND ItemCode=N'EXPIRED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EVENT_ACTION' AND ItemCode=N'RAISE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_ACTION', N'RAISE', N'发布事件', N'Raise', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'发布事件', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_ACTION' AND ItemCode=N'RAISE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EVENT_ACTION' AND ItemCode=N'RESOLVE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_ACTION', N'RESOLVE', N'解析接收', N'Resolve', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'解析接收', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_ACTION' AND ItemCode=N'RESOLVE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EVENT_ACTION' AND ItemCode=N'DELIVER')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_ACTION', N'DELIVER', N'投递', N'Deliver', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'投递', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_ACTION' AND ItemCode=N'DELIVER';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='EVENT_ACTION' AND ItemCode=N'HANDLE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('EVENT_ACTION', N'HANDLE', N'处理', N'Handle', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'处理', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='EVENT_ACTION' AND ItemCode=N'HANDLE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='RESOLVE_TYPE' AND ItemCode=N'DUTY')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RESOLVE_TYPE', N'DUTY', N'按职责', N'Duty', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'按职责', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RESOLVE_TYPE' AND ItemCode=N'DUTY';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='RESOLVE_TYPE' AND ItemCode=N'POSITION')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RESOLVE_TYPE', N'POSITION', N'按岗位', N'Position', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'按岗位', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RESOLVE_TYPE' AND ItemCode=N'POSITION';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='RESOLVE_TYPE' AND ItemCode=N'MANAGER')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RESOLVE_TYPE', N'MANAGER', N'按上级', N'Manager', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'按上级', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RESOLVE_TYPE' AND ItemCode=N'MANAGER';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='RESOLVE_TYPE' AND ItemCode=N'DELEGATE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('RESOLVE_TYPE', N'DELEGATE', N'按委托', N'Delegate', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'按委托', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='RESOLVE_TYPE' AND ItemCode=N'DELEGATE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='SUB_TYPE' AND ItemCode=N'RESOURCE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SUB_TYPE', N'RESOURCE', N'资源订阅', N'Resource', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'资源订阅', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SUB_TYPE' AND ItemCode=N'RESOURCE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='SUB_TYPE' AND ItemCode=N'EVENT')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SUB_TYPE', N'EVENT', N'事件订阅', N'Event', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'事件订阅', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SUB_TYPE' AND ItemCode=N'EVENT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='SUB_TYPE' AND ItemCode=N'MIXED')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('SUB_TYPE', N'MIXED', N'混合订阅', N'Mixed', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'混合订阅', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='SUB_TYPE' AND ItemCode=N'MIXED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TARGET_RESOLVE' AND ItemCode=N'DUTY')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TARGET_RESOLVE', N'DUTY', N'按职责', N'By Duty', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'按职责', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TARGET_RESOLVE' AND ItemCode=N'DUTY';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TARGET_RESOLVE' AND ItemCode=N'POSITION')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TARGET_RESOLVE', N'POSITION', N'按岗位', N'By Position', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'按岗位', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TARGET_RESOLVE' AND ItemCode=N'POSITION';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TARGET_RESOLVE' AND ItemCode=N'DEPT_MANAGER')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TARGET_RESOLVE', N'DEPT_MANAGER', N'部门负责人', N'Dept Leader', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'部门负责人', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TARGET_RESOLVE' AND ItemCode=N'DEPT_MANAGER';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TARGET_RESOLVE' AND ItemCode=N'OWNER_MANAGER')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TARGET_RESOLVE', N'OWNER_MANAGER', N'对象所有人上级', N'Owner Mgr', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'对象所有人上级', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TARGET_RESOLVE' AND ItemCode=N'OWNER_MANAGER';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='TARGET_RESOLVE' AND ItemCode=N'FIXED_USER')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('TARGET_RESOLVE', N'FIXED_USER', N'指定用户', N'Fixed User', NULL, 5, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'指定用户', DispSeq=5, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='TARGET_RESOLVE' AND ItemCode=N'FIXED_USER';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_SEX' AND ItemCode=N'男')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_SEX', N'男', N'男', NULL, NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'男', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_SEX' AND ItemCode=N'男';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_SEX' AND ItemCode=N'女')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_SEX', N'女', N'女', NULL, NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'女', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_SEX' AND ItemCode=N'女';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_PER_GRADE' AND ItemCode=N'员级')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_PER_GRADE', N'员级', N'员级', NULL, NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'员级', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_PER_GRADE' AND ItemCode=N'员级';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_PER_GRADE' AND ItemCode=N'主办')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_PER_GRADE', N'主办', N'主办', NULL, NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'主办', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_PER_GRADE' AND ItemCode=N'主办';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_PER_GRADE' AND ItemCode=N'主管')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_PER_GRADE', N'主管', N'主管', NULL, NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'主管', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_PER_GRADE' AND ItemCode=N'主管';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_PER_GRADE' AND ItemCode=N'高级主管')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_PER_GRADE', N'高级主管', N'高级主管', NULL, NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'高级主管', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_PER_GRADE' AND ItemCode=N'高级主管';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_PER_GRADE' AND ItemCode=N'经理')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_PER_GRADE', N'经理', N'经理', NULL, NULL, 5, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'经理', DispSeq=5, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_PER_GRADE' AND ItemCode=N'经理';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_PER_GRADE' AND ItemCode=N'其它')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_PER_GRADE', N'其它', N'其它', NULL, NULL, 99, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'其它', DispSeq=99, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_PER_GRADE' AND ItemCode=N'其它';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_HEALTH' AND ItemCode=N'健康')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_HEALTH', N'健康', N'健康', NULL, NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'健康', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_HEALTH' AND ItemCode=N'健康';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_HEALTH' AND ItemCode=N'良好')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_HEALTH', N'良好', N'良好', NULL, NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'良好', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_HEALTH' AND ItemCode=N'良好';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_HEALTH' AND ItemCode=N'一般')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_HEALTH', N'一般', N'一般', NULL, NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'一般', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_HEALTH' AND ItemCode=N'一般';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_HEALTH' AND ItemCode=N'慢性病')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_HEALTH', N'慢性病', N'慢性病', NULL, NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'慢性病', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_HEALTH' AND ItemCode=N'慢性病';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_HEALTH' AND ItemCode=N'其它')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_HEALTH', N'其它', N'其它', NULL, NULL, 99, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'其它', DispSeq=99, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_HEALTH' AND ItemCode=N'其它';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'小学')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_EDU_LEVEL', N'小学', N'小学', NULL, NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'小学', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'小学';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'初中')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_EDU_LEVEL', N'初中', N'初中', NULL, NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'初中', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'初中';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'高中')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_EDU_LEVEL', N'高中', N'高中', NULL, NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'高中', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'高中';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'中专')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_EDU_LEVEL', N'中专', N'中专', NULL, NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'中专', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'中专';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'大专')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_EDU_LEVEL', N'大专', N'大专', NULL, NULL, 5, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'大专', DispSeq=5, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'大专';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'本科')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_EDU_LEVEL', N'本科', N'本科', NULL, NULL, 6, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'本科', DispSeq=6, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'本科';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'硕士')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_EDU_LEVEL', N'硕士', N'硕士', NULL, NULL, 7, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'硕士', DispSeq=7, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'硕士';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'博士')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_EDU_LEVEL', N'博士', N'博士', NULL, NULL, 8, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'博士', DispSeq=8, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'博士';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'其它')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('MEMBER_EDU_LEVEL', N'其它', N'其它', NULL, NULL, 99, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'其它', DispSeq=99, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='MEMBER_EDU_LEVEL' AND ItemCode=N'其它';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='FLOW_ACTION' AND ItemCode=N'CREATE_TODO')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('FLOW_ACTION', N'CREATE_TODO', N'生成待办', N'Create Todo', NULL, 1, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'生成待办', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='FLOW_ACTION' AND ItemCode=N'CREATE_TODO';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='FLOW_ACTION' AND ItemCode=N'SEND_NOTICE')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('FLOW_ACTION', N'SEND_NOTICE', N'发送通知', N'Send Notice', NULL, 2, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'发送通知', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='FLOW_ACTION' AND ItemCode=N'SEND_NOTICE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='FLOW_ACTION' AND ItemCode=N'TRIGGER_EVENT')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('FLOW_ACTION', N'TRIGGER_EVENT', N'触发下一事件', N'Trigger Event', NULL, 3, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'触发下一事件', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='FLOW_ACTION' AND ItemCode=N'TRIGGER_EVENT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='FLOW_ACTION' AND ItemCode=N'CALL_API')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('FLOW_ACTION', N'CALL_API', N'调用接口', N'Call API', NULL, 4, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'调用接口', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='FLOW_ACTION' AND ItemCode=N'CALL_API';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HR_LEAVE_TYPE' AND ItemCode=N'年假')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HR_LEAVE_TYPE', N'年假', N'年假', NULL, NULL, 10, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'年假', DispSeq=10, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HR_LEAVE_TYPE' AND ItemCode=N'年假';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HR_LEAVE_TYPE' AND ItemCode=N'事假')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HR_LEAVE_TYPE', N'事假', N'事假', NULL, NULL, 20, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'事假', DispSeq=20, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HR_LEAVE_TYPE' AND ItemCode=N'事假';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HR_LEAVE_TYPE' AND ItemCode=N'病假')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HR_LEAVE_TYPE', N'病假', N'病假', NULL, NULL, 30, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'病假', DispSeq=30, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HR_LEAVE_TYPE' AND ItemCode=N'病假';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HR_LEAVE_TYPE' AND ItemCode=N'调休')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HR_LEAVE_TYPE', N'调休', N'调休', NULL, NULL, 40, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'调休', DispSeq=40, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HR_LEAVE_TYPE' AND ItemCode=N'调休';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HR_LEAVE_TYPE' AND ItemCode=N'婚假')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HR_LEAVE_TYPE', N'婚假', N'婚假', NULL, NULL, 50, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'婚假', DispSeq=50, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HR_LEAVE_TYPE' AND ItemCode=N'婚假';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HR_LEAVE_TYPE' AND ItemCode=N'产假')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HR_LEAVE_TYPE', N'产假', N'产假', NULL, NULL, 60, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'产假', DispSeq=60, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HR_LEAVE_TYPE' AND ItemCode=N'产假';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode='HR_LEAVE_TYPE' AND ItemCode=N'其他')
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ('HR_LEAVE_TYPE', N'其他', N'其他', NULL, NULL, 99, NULL, 1, NULL, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName=N'其他', DispSeq=99, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode='HR_LEAVE_TYPE' AND ItemCode=N'其他';

PRINT N'20-Seed_Foundation 完成。';
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'20-Seed_Foundation.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'20-Seed_Foundation.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'20-Seed_Foundation.sql');
GO
