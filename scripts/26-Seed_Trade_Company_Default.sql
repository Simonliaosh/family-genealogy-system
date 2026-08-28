/*
==============================================================================
  EFrame 种子 07 - 贸易公司默认组织/职责/事件/订阅模板
==============================================================================
  命名规则：
    部门 TRADE001~008 | 岗位 POST_* | 职责 TRADE_* | 事件 TRADE.{SO|PO|PAY|OUT|IN}.*
    资源 RES.TRADE.* | 菜单组 TRD | 应用 TRADE
  前置：20~24 种子（或至少 20 Foundation + 23 框架菜单）
  顺序：第 7 步（在 25 Dashboard 之后亦可）
  编码：ANSI (GBK)
  说明：幂等可重复执行；演示账号密码均为 123456
==============================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-TRADE';
DECLARE @Pwd NVARCHAR(200) = N'49ba59abbe56e057';

/* ----- 应用模块 TRADE ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_AppModule WHERE AppCode='TRADE')
    INSERT INTO dbo.Tbl_E_AppModule (AppCode, AppName, AppType, BaseUrl, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', N'贸易管理', 'BUSINESS', N'/TradeHome/Index', 5, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_AppModule SET AppName=N'贸易管理', AppType='BUSINESS', BaseUrl=N'/TradeHome/Index', DispSeq=5, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='TRD')
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRD', 'TRADE', N'贸易管理', 22, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_MenuGroup SET AppCode='TRADE', MenuGroupName=N'贸易管理', DispSeq=22, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE MenuGroupCode='TRD';

/* ----- 部门（先根后子） ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode=N'TRADE001')
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'TRADE001', N'某某贸易有限公司', 1, N'/1/', 'COMPANY', 1, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Department SET DeptCName=N'某某贸易有限公司', DeptLevel=1, DeptPath=N'/1/', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DeptCode=N'TRADE001';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode=N'TRADE002')
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, ParentDeptID, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT N'TRADE002', N'总经理办公室', p.DataID, 2, N'/1/1/', 'DEPT', 2, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Department p WHERE p.DeptCode=N'TRADE001';
ELSE
    UPDATE dbo.Tbl_E_Department SET DeptCName=N'总经理办公室', DeptLevel=2, DeptPath=N'/1/1/', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DeptCode=N'TRADE002';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode=N'TRADE003')
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, ParentDeptID, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT N'TRADE003', N'销售部', p.DataID, 2, N'/1/2/', 'DEPT', 3, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Department p WHERE p.DeptCode=N'TRADE001';
ELSE
    UPDATE dbo.Tbl_E_Department SET DeptCName=N'销售部', DeptLevel=2, DeptPath=N'/1/2/', DispSeq=3, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DeptCode=N'TRADE003';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode=N'TRADE004')
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, ParentDeptID, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT N'TRADE004', N'采购部', p.DataID, 2, N'/1/3/', 'DEPT', 4, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Department p WHERE p.DeptCode=N'TRADE001';
ELSE
    UPDATE dbo.Tbl_E_Department SET DeptCName=N'采购部', DeptLevel=2, DeptPath=N'/1/3/', DispSeq=4, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DeptCode=N'TRADE004';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode=N'TRADE005')
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, ParentDeptID, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT N'TRADE005', N'仓储物流部', p.DataID, 2, N'/1/4/', 'DEPT', 5, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Department p WHERE p.DeptCode=N'TRADE001';
ELSE
    UPDATE dbo.Tbl_E_Department SET DeptCName=N'仓储物流部', DeptLevel=2, DeptPath=N'/1/4/', DispSeq=5, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DeptCode=N'TRADE005';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode=N'TRADE006')
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, ParentDeptID, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT N'TRADE006', N'财务部', p.DataID, 2, N'/1/5/', 'DEPT', 6, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Department p WHERE p.DeptCode=N'TRADE001';
ELSE
    UPDATE dbo.Tbl_E_Department SET DeptCName=N'财务部', DeptLevel=2, DeptPath=N'/1/5/', DispSeq=6, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DeptCode=N'TRADE006';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode=N'TRADE007')
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, ParentDeptID, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT N'TRADE007', N'人事行政部', p.DataID, 2, N'/1/6/', 'DEPT', 7, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Department p WHERE p.DeptCode=N'TRADE001';
ELSE
    UPDATE dbo.Tbl_E_Department SET DeptCName=N'人事行政部', DeptLevel=2, DeptPath=N'/1/6/', DispSeq=7, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DeptCode=N'TRADE007';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode=N'TRADE008')
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, ParentDeptID, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT N'TRADE008', N'信息技术部', p.DataID, 2, N'/1/7/', 'DEPT', 8, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Department p WHERE p.DeptCode=N'TRADE001';
ELSE
    UPDATE dbo.Tbl_E_Department SET DeptCName=N'信息技术部', DeptLevel=2, DeptPath=N'/1/7/', DispSeq=8, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DeptCode=N'TRADE008';

/* ----- 岗位 ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'POST_CEO')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PositionType, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'POST_CEO', N'总经理', N'MANAGER', N'ALL', 1, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'总经理', PositionType=N'MANAGER', DataScope=N'ALL', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'POST_CEO';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'POST_SALES_MGR')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PositionType, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'POST_SALES_MGR', N'销售经理', N'MANAGER', N'DEPT', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'销售经理', PositionType=N'MANAGER', DataScope=N'DEPT', DispSeq=10, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'POST_SALES_MGR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'POST_SALES')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PositionType, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'POST_SALES', N'销售员', N'SALES', N'DEPT', 11, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'销售员', PositionType=N'SALES', DataScope=N'DEPT', DispSeq=11, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'POST_SALES';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'POST_PUR_MGR')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PositionType, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'POST_PUR_MGR', N'采购经理', N'MANAGER', N'DEPT', 20, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'采购经理', PositionType=N'MANAGER', DataScope=N'DEPT', DispSeq=20, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'POST_PUR_MGR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'POST_PUR')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PositionType, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'POST_PUR', N'采购员', N'SERVICE', N'DEPT', 21, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'采购员', PositionType=N'SERVICE', DataScope=N'DEPT', DispSeq=21, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'POST_PUR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'POST_WH_MGR')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PositionType, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'POST_WH_MGR', N'仓储主管', N'MANAGER', N'DEPT', 30, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'仓储主管', PositionType=N'MANAGER', DataScope=N'DEPT', DispSeq=30, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'POST_WH_MGR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'POST_WH')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PositionType, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'POST_WH', N'仓管员', N'SERVICE', N'DEPT', 31, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'仓管员', PositionType=N'SERVICE', DataScope=N'DEPT', DispSeq=31, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'POST_WH';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'POST_FIN_MGR')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PositionType, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'POST_FIN_MGR', N'财务经理', N'FINANCE', N'DEPT', 40, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'财务经理', PositionType=N'FINANCE', DataScope=N'DEPT', DispSeq=40, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'POST_FIN_MGR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'POST_FIN')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PositionType, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'POST_FIN', N'会计', N'FINANCE', N'DEPT', 41, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'会计', PositionType=N'FINANCE', DataScope=N'DEPT', DispSeq=41, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'POST_FIN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'POST_HR_MGR')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PositionType, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'POST_HR_MGR', N'人事主管', N'MANAGER', N'DEPT', 50, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'人事主管', PositionType=N'MANAGER', DataScope=N'DEPT', DispSeq=50, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'POST_HR_MGR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'POST_HR')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PositionType, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'POST_HR', N'人事专员', N'SERVICE', N'DEPT', 51, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'人事专员', PositionType=N'SERVICE', DataScope=N'DEPT', DispSeq=51, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'POST_HR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'POST_ADMIN')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PositionType, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'POST_ADMIN', N'系统管理员', N'ADMIN', N'ALL', 90, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'系统管理员', PositionType=N'ADMIN', DataScope=N'ALL', DispSeq=90, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'POST_ADMIN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'POST_STAFF')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PositionType, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'POST_STAFF', N'普通职员', N'SERVICE', N'DEPT', 99, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'普通职员', PositionType=N'SERVICE', DataScope=N'DEPT', DispSeq=99, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'POST_STAFF';

/* ----- 职责 ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_ADMIN')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'TRADE_ADMIN', N'贸易系统管理员', N'ADMIN', 1, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'贸易系统管理员', DutyCategory=N'ADMIN', DutyDispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_VIEWER')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'TRADE_VIEWER', N'贸易只读查询', N'VIEW', 2, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'贸易只读查询', DutyCategory=N'VIEW', DutyDispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'TRADE_VIEWER';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_CEO')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'TRADE_CEO', N'总经理审批', N'APPROVAL', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'总经理审批', DutyCategory=N'APPROVAL', DutyDispSeq=10, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'TRADE_CEO';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_SALES_MGR')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'TRADE_SALES_MGR', N'销售审批', N'APPROVAL', 11, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'销售审批', DutyCategory=N'APPROVAL', DutyDispSeq=11, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'TRADE_SALES_MGR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_SALES')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'TRADE_SALES', N'销售业务', N'SERVICE', 12, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'销售业务', DutyCategory=N'SERVICE', DutyDispSeq=12, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'TRADE_SALES';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_PUR_MGR')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'TRADE_PUR_MGR', N'采购审批', N'APPROVAL', 21, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'采购审批', DutyCategory=N'APPROVAL', DutyDispSeq=21, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'TRADE_PUR_MGR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_PUR')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'TRADE_PUR', N'采购业务', N'SERVICE', 22, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'采购业务', DutyCategory=N'SERVICE', DutyDispSeq=22, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'TRADE_PUR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_WH_MGR')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'TRADE_WH_MGR', N'仓储审批', N'APPROVAL', 31, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'仓储审批', DutyCategory=N'APPROVAL', DutyDispSeq=31, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'TRADE_WH_MGR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_WH')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'TRADE_WH', N'仓储业务', N'SERVICE', 32, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'仓储业务', DutyCategory=N'SERVICE', DutyDispSeq=32, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'TRADE_WH';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_FIN_MGR')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'TRADE_FIN_MGR', N'财务审批', N'APPROVAL', 41, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'财务审批', DutyCategory=N'APPROVAL', DutyDispSeq=41, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'TRADE_FIN_MGR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_FIN')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'TRADE_FIN', N'财务业务', N'SERVICE', 42, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'财务业务', DutyCategory=N'SERVICE', DutyDispSeq=42, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'TRADE_FIN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_HR_MGR')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'TRADE_HR_MGR', N'人事审批', N'APPROVAL', 51, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'人事审批', DutyCategory=N'APPROVAL', DutyDispSeq=51, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'TRADE_HR_MGR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_HR')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'TRADE_HR', N'人事业务', N'SERVICE', 52, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'人事业务', DutyCategory=N'SERVICE', DutyDispSeq=52, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'TRADE_HR';

/* ----- 岗位职责 ----- */

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_CEO' AND d.DutyCode=N'TRADE_CEO' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_CEO' AND d.DutyCode=N'TRADE_CEO';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_CEO' AND d.DutyCode=N'TRADE_VIEWER' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'110000', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_CEO' AND d.DutyCode=N'TRADE_VIEWER';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_SALES_MGR' AND d.DutyCode=N'TRADE_SALES_MGR' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_SALES_MGR' AND d.DutyCode=N'TRADE_SALES_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_SALES_MGR' AND d.DutyCode=N'TRADE_SALES' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_SALES_MGR' AND d.DutyCode=N'TRADE_SALES';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_SALES' AND d.DutyCode=N'TRADE_SALES' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_SALES' AND d.DutyCode=N'TRADE_SALES';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_PUR_MGR' AND d.DutyCode=N'TRADE_PUR_MGR' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_PUR_MGR' AND d.DutyCode=N'TRADE_PUR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_PUR_MGR' AND d.DutyCode=N'TRADE_PUR' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_PUR_MGR' AND d.DutyCode=N'TRADE_PUR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_PUR' AND d.DutyCode=N'TRADE_PUR' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_PUR' AND d.DutyCode=N'TRADE_PUR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_WH_MGR' AND d.DutyCode=N'TRADE_WH_MGR' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_WH_MGR' AND d.DutyCode=N'TRADE_WH_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_WH_MGR' AND d.DutyCode=N'TRADE_WH' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_WH_MGR' AND d.DutyCode=N'TRADE_WH';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_WH' AND d.DutyCode=N'TRADE_WH' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_WH' AND d.DutyCode=N'TRADE_WH';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_FIN_MGR' AND d.DutyCode=N'TRADE_FIN_MGR' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_FIN_MGR' AND d.DutyCode=N'TRADE_FIN_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_FIN_MGR' AND d.DutyCode=N'TRADE_FIN' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_FIN_MGR' AND d.DutyCode=N'TRADE_FIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_FIN' AND d.DutyCode=N'TRADE_FIN' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_FIN' AND d.DutyCode=N'TRADE_FIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_HR_MGR' AND d.DutyCode=N'TRADE_HR_MGR' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_HR_MGR' AND d.DutyCode=N'TRADE_HR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_HR_MGR' AND d.DutyCode=N'TRADE_HR' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_HR_MGR' AND d.DutyCode=N'TRADE_HR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_HR' AND d.DutyCode=N'TRADE_HR' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111100', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_HR' AND d.DutyCode=N'TRADE_HR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_ADMIN' AND d.DutyCode=N'TRADE_ADMIN' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111111', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_ADMIN' AND d.DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_ADMIN' AND d.DutyCode=N'TRADE_VIEWER' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'110000', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_ADMIN' AND d.DutyCode=N'TRADE_VIEWER';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'POST_STAFF' AND d.DutyCode=N'TRADE_VIEWER' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'110000', 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'POST_STAFF' AND d.DutyCode=N'TRADE_VIEWER';

/* ----- 演示用户（密码 123456） ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Users WHERE LoginId=N'tradeadmin')
    INSERT INTO dbo.Tbl_E_Users (LoginId, RealName, PwdHash, PasswordAlgo, PasswordVersion, UserType, LoginCount, MaxLoginCount, PwdErrorCount, MaxPwdErrorCount, IsLocked, IsEnabled, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'tradeadmin', N'系统管理员', @Pwd, 'MD5_16', 1, 'EMPLOYEE', 0, 99999, 0, 5, 0, 1, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Users SET RealName=N'系统管理员', PwdHash=@Pwd, PasswordAlgo='MD5_16', IsEnabled=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE LoginId=N'tradeadmin';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Users WHERE LoginId=N'tradeceo')
    INSERT INTO dbo.Tbl_E_Users (LoginId, RealName, PwdHash, PasswordAlgo, PasswordVersion, UserType, LoginCount, MaxLoginCount, PwdErrorCount, MaxPwdErrorCount, IsLocked, IsEnabled, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'tradeceo', N'张总', @Pwd, 'MD5_16', 1, 'EMPLOYEE', 0, 99999, 0, 5, 0, 1, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Users SET RealName=N'张总', PwdHash=@Pwd, PasswordAlgo='MD5_16', IsEnabled=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE LoginId=N'tradeceo';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Users WHERE LoginId=N'salesmgr')
    INSERT INTO dbo.Tbl_E_Users (LoginId, RealName, PwdHash, PasswordAlgo, PasswordVersion, UserType, LoginCount, MaxLoginCount, PwdErrorCount, MaxPwdErrorCount, IsLocked, IsEnabled, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'salesmgr', N'李销售经理', @Pwd, 'MD5_16', 1, 'EMPLOYEE', 0, 99999, 0, 5, 0, 1, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Users SET RealName=N'李销售经理', PwdHash=@Pwd, PasswordAlgo='MD5_16', IsEnabled=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE LoginId=N'salesmgr';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Users WHERE LoginId=N'sales01')
    INSERT INTO dbo.Tbl_E_Users (LoginId, RealName, PwdHash, PasswordAlgo, PasswordVersion, UserType, LoginCount, MaxLoginCount, PwdErrorCount, MaxPwdErrorCount, IsLocked, IsEnabled, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'sales01', N'王销售', @Pwd, 'MD5_16', 1, 'EMPLOYEE', 0, 99999, 0, 5, 0, 1, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Users SET RealName=N'王销售', PwdHash=@Pwd, PasswordAlgo='MD5_16', IsEnabled=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE LoginId=N'sales01';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Users WHERE LoginId=N'purmgr')
    INSERT INTO dbo.Tbl_E_Users (LoginId, RealName, PwdHash, PasswordAlgo, PasswordVersion, UserType, LoginCount, MaxLoginCount, PwdErrorCount, MaxPwdErrorCount, IsLocked, IsEnabled, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'purmgr', N'赵采购经理', @Pwd, 'MD5_16', 1, 'EMPLOYEE', 0, 99999, 0, 5, 0, 1, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Users SET RealName=N'赵采购经理', PwdHash=@Pwd, PasswordAlgo='MD5_16', IsEnabled=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE LoginId=N'purmgr';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Users WHERE LoginId=N'pur01')
    INSERT INTO dbo.Tbl_E_Users (LoginId, RealName, PwdHash, PasswordAlgo, PasswordVersion, UserType, LoginCount, MaxLoginCount, PwdErrorCount, MaxPwdErrorCount, IsLocked, IsEnabled, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'pur01', N'钱采购', @Pwd, 'MD5_16', 1, 'EMPLOYEE', 0, 99999, 0, 5, 0, 1, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Users SET RealName=N'钱采购', PwdHash=@Pwd, PasswordAlgo='MD5_16', IsEnabled=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE LoginId=N'pur01';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Users WHERE LoginId=N'whmgr')
    INSERT INTO dbo.Tbl_E_Users (LoginId, RealName, PwdHash, PasswordAlgo, PasswordVersion, UserType, LoginCount, MaxLoginCount, PwdErrorCount, MaxPwdErrorCount, IsLocked, IsEnabled, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'whmgr', N'孙仓储主管', @Pwd, 'MD5_16', 1, 'EMPLOYEE', 0, 99999, 0, 5, 0, 1, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Users SET RealName=N'孙仓储主管', PwdHash=@Pwd, PasswordAlgo='MD5_16', IsEnabled=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE LoginId=N'whmgr';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Users WHERE LoginId=N'finmgr')
    INSERT INTO dbo.Tbl_E_Users (LoginId, RealName, PwdHash, PasswordAlgo, PasswordVersion, UserType, LoginCount, MaxLoginCount, PwdErrorCount, MaxPwdErrorCount, IsLocked, IsEnabled, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'finmgr', N'周财务经理', @Pwd, 'MD5_16', 1, 'EMPLOYEE', 0, 99999, 0, 5, 0, 1, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Users SET RealName=N'周财务经理', PwdHash=@Pwd, PasswordAlgo='MD5_16', IsEnabled=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE LoginId=N'finmgr';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Users WHERE LoginId=N'hrmgr')
    INSERT INTO dbo.Tbl_E_Users (LoginId, RealName, PwdHash, PasswordAlgo, PasswordVersion, UserType, LoginCount, MaxLoginCount, PwdErrorCount, MaxPwdErrorCount, IsLocked, IsEnabled, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'hrmgr', N'吴人事主管', @Pwd, 'MD5_16', 1, 'EMPLOYEE', 0, 99999, 0, 5, 0, 1, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Users SET RealName=N'吴人事主管', PwdHash=@Pwd, PasswordAlgo='MD5_16', IsEnabled=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE LoginId=N'hrmgr';

/* ----- 用户任岗 ----- */

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'tradeadmin' AND d.DeptCode=N'TRADE008' AND p.PostCode=N'POST_ADMIN' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'TRADE008'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'POST_ADMIN'
    WHERE u.LoginId=N'tradeadmin';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'tradeceo' AND d.DeptCode=N'TRADE002' AND p.PostCode=N'POST_CEO' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'TRADE002'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'POST_CEO'
    WHERE u.LoginId=N'tradeceo';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'salesmgr' AND d.DeptCode=N'TRADE003' AND p.PostCode=N'POST_SALES_MGR' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'TRADE003'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'POST_SALES_MGR'
    WHERE u.LoginId=N'salesmgr';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'sales01' AND d.DeptCode=N'TRADE003' AND p.PostCode=N'POST_SALES' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'TRADE003'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'POST_SALES'
    WHERE u.LoginId=N'sales01';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'purmgr' AND d.DeptCode=N'TRADE004' AND p.PostCode=N'POST_PUR_MGR' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'TRADE004'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'POST_PUR_MGR'
    WHERE u.LoginId=N'purmgr';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'pur01' AND d.DeptCode=N'TRADE004' AND p.PostCode=N'POST_PUR' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'TRADE004'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'POST_PUR'
    WHERE u.LoginId=N'pur01';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'whmgr' AND d.DeptCode=N'TRADE005' AND p.PostCode=N'POST_WH_MGR' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'TRADE005'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'POST_WH_MGR'
    WHERE u.LoginId=N'whmgr';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'finmgr' AND d.DeptCode=N'TRADE006' AND p.PostCode=N'POST_FIN_MGR' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'TRADE006'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'POST_FIN_MGR'
    WHERE u.LoginId=N'finmgr';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'hrmgr' AND d.DeptCode=N'TRADE007' AND p.PostCode=N'POST_HR_MGR' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'TRADE007'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'POST_HR_MGR'
    WHERE u.LoginId=N'hrmgr';

/* ----- 贸易菜单资源 ----- */
IF OBJECT_ID('tempdb..#TradeRes') IS NOT NULL DROP TABLE #TradeRes;
CREATE TABLE #TradeRes (ResourceID VARCHAR(50) NOT NULL PRIMARY KEY, ResourceName NVARCHAR(100) NOT NULL, MenuPath NVARCHAR(300) NOT NULL, DispSeq INT NOT NULL);
INSERT INTO #TradeRes VALUES
(N'RES.TRADE.Home', N'贸易工作台', N'/TradeHome/Index', 10),
(N'RES.TRADE.SO.List', N'销售订单', N'/TradeSO/Index', 20),
(N'RES.TRADE.SO.Create', N'新建销售订单', N'/TradeSO/Create', 21),
(N'RES.TRADE.SO.Approval', N'销售订单审批', N'/TradeSO/Approval', 22),
(N'RES.TRADE.PO.List', N'采购申请', N'/TradePO/Index', 30),
(N'RES.TRADE.PO.Create', N'新建采购申请', N'/TradePO/Create', 31),
(N'RES.TRADE.PO.Approval', N'采购申请审批', N'/TradePO/Approval', 32),
(N'RES.TRADE.PAY.List', N'付款申请', N'/TradePay/Index', 40),
(N'RES.TRADE.PAY.Approval', N'付款审批', N'/TradePay/Approval', 41),
(N'RES.TRADE.WH.Out', N'出库管理', N'/TradeWH/Out', 50),
(N'RES.TRADE.WH.In', N'入库管理', N'/TradeWH/In', 51),
(N'RES.TRADE.MyTodo', N'我的待办', N'/ETodoTask/Index', 60);

MERGE dbo.Tbl_E_Resource AS t
USING (SELECT 'TRADE' AS AppCode, 'TRD' AS MenuGroupCode, s.* FROM #TradeRes s) AS s
ON t.ResourceID = s.ResourceID
WHEN NOT MATCHED THEN
    INSERT (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (s.AppCode, s.ResourceID, s.ResourceName, 'MENU', s.MenuPath, s.MenuGroupCode, s.DispSeq, '1', 0, @Now, @Now, @Op)
WHEN MATCHED THEN
    UPDATE SET ResourceName=s.ResourceName, MenuPath=s.MenuPath, MenuGroupCode=s.MenuGroupCode, DispSeq=s.DispSeq, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op;
DROP TABLE #TradeRes;


/* ----- 资源订阅 ----- */

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.Home', 1, N'111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.EUsers' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.EUsers', 1, N'111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.EDept' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.EDept', 1, N'111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.EPosition' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.EPosition', 1, N'111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.EDuty' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.EDuty', 1, N'111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.EUserPosition' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.EUserPosition', 1, N'111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.EPositionDuty' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.EPositionDuty', 1, N'111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.ESubscription' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.ESubscription', 1, N'111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.EEventConfig' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.EEventConfig', 1, N'111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.EEventFlowRule' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.EEventFlowRule', 1, N'111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.ETodoTask' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.ETodoTask', 1, N'111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.DASH.Board' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.DASH.Board', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.DASH.PosTemplate' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.DASH.PosTemplate', 1, N'111111', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_CEO' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_CEO';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_CEO' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.DASH.Board' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.DASH.Board', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_CEO';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_CEO' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_CEO';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_CEO' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.SO.Approval' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.SO.Approval', 1, N'111100', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_CEO';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_CEO' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.PO.Approval' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.PO.Approval', 1, N'111100', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_CEO';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_CEO' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.PAY.Approval' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.PAY.Approval', 1, N'111100', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_CEO';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_CEO' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.MyTodo' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.MyTodo', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_CEO';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_CEO' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.ETodoTask' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.ETodoTask', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_CEO';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.DASH.Board' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.DASH.Board', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.SO.List' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.SO.List', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.SO.Approval' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.SO.Approval', 1, N'111100', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.MyTodo' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.MyTodo', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.ETodoTask' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.ETodoTask', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.DASH.Board' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.DASH.Board', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.SO.List' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.SO.List', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.SO.Create' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.SO.Create', 1, N'111100', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.MyTodo' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.MyTodo', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_PUR_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_PUR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_PUR_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.DASH.Board' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.DASH.Board', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_PUR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_PUR_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.PO.List' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.PO.List', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_PUR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_PUR_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.PO.Approval' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.PO.Approval', 1, N'111100', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_PUR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_PUR_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.MyTodo' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.MyTodo', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_PUR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_PUR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_PUR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_PUR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.PO.List' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.PO.List', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_PUR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_PUR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.PO.Create' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.PO.Create', 1, N'111100', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_PUR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_PUR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.MyTodo' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.MyTodo', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_PUR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_WH_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_WH_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_WH_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.WH.Out' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.WH.Out', 1, N'111100', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_WH_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_WH_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.WH.In' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.WH.In', 1, N'111100', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_WH_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_WH_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.MyTodo' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.MyTodo', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_WH_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_WH' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_WH';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_WH' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.WH.Out' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.WH.Out', 1, N'111100', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_WH';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_WH' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.WH.In' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.WH.In', 1, N'111100', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_WH';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_FIN_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_FIN_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_FIN_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.PAY.List' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.PAY.List', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_FIN_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_FIN_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.PAY.Approval' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.PAY.Approval', 1, N'111100', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_FIN_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_FIN_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.MyTodo' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.MyTodo', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_FIN_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_FIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_FIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_FIN' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.TRADE.PAY.List' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.TRADE.PAY.List', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_FIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_HR_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_HR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_HR_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.HR.LeaveApproval' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.HR.LeaveApproval', 1, N'111100', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_HR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_HR_MGR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.HR.MyTodo' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.HR.MyTodo', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_HR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_HR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_HR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_HR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.HR.LeaveApply' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.HR.LeaveApply', 1, N'111100', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_HR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_HR' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.HR.MyLeave' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.HR.MyLeave', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_HR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_VIEWER' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.CF.Home' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.CF.Home', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_VIEWER';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_VIEWER' AND s.SubType='RESOURCE' AND s.ResourceID=N'RES.DASH.Board' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', N'RES.DASH.Board', 1, N'110000', 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_VIEWER';

/* ----- 事件配置 ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='TRADE' AND EventCode=N'TRADE.SO.SUBMIT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', N'TRADE.SO.SUBMIT', N'销售订单提交', N'TASK', N'/TradeSO/Approval', 'TRD', 'ASYNC', 1, N'【待办】销售订单待审', 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'销售订单提交', EventType=N'TASK', PageUrl=N'/TradeSO/Approval', MenuGroupCode='TRD', IsGenerateTodo=1, TodoTitle=N'【待办】销售订单待审', BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE' AND EventCode=N'TRADE.SO.SUBMIT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='TRADE' AND EventCode=N'TRADE.SO.APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', N'TRADE.SO.APPROVED', N'销售订单通过', N'NOTICE', NULL, 'TRD', 'ASYNC', 0, NULL, 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'销售订单通过', EventType=N'NOTICE', PageUrl=NULL, MenuGroupCode='TRD', IsGenerateTodo=0, TodoTitle=NULL, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE' AND EventCode=N'TRADE.SO.APPROVED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='TRADE' AND EventCode=N'TRADE.SO.REJECTED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', N'TRADE.SO.REJECTED', N'销售订单驳回', N'NOTICE', NULL, 'TRD', 'ASYNC', 0, NULL, 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'销售订单驳回', EventType=N'NOTICE', PageUrl=NULL, MenuGroupCode='TRD', IsGenerateTodo=0, TodoTitle=NULL, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE' AND EventCode=N'TRADE.SO.REJECTED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='TRADE' AND EventCode=N'TRADE.PO.SUBMIT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', N'TRADE.PO.SUBMIT', N'采购申请提交', N'TASK', N'/TradePO/Approval', 'TRD', 'ASYNC', 1, N'【待办】采购申请待审', 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'采购申请提交', EventType=N'TASK', PageUrl=N'/TradePO/Approval', MenuGroupCode='TRD', IsGenerateTodo=1, TodoTitle=N'【待办】采购申请待审', BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE' AND EventCode=N'TRADE.PO.SUBMIT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='TRADE' AND EventCode=N'TRADE.PO.APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', N'TRADE.PO.APPROVED', N'采购申请通过', N'NOTICE', NULL, 'TRD', 'ASYNC', 0, NULL, 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'采购申请通过', EventType=N'NOTICE', PageUrl=NULL, MenuGroupCode='TRD', IsGenerateTodo=0, TodoTitle=NULL, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE' AND EventCode=N'TRADE.PO.APPROVED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='TRADE' AND EventCode=N'TRADE.PO.REJECTED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', N'TRADE.PO.REJECTED', N'采购申请驳回', N'NOTICE', NULL, 'TRD', 'ASYNC', 0, NULL, 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'采购申请驳回', EventType=N'NOTICE', PageUrl=NULL, MenuGroupCode='TRD', IsGenerateTodo=0, TodoTitle=NULL, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE' AND EventCode=N'TRADE.PO.REJECTED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='TRADE' AND EventCode=N'TRADE.PAY.SUBMIT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', N'TRADE.PAY.SUBMIT', N'付款申请提交', N'TASK', N'/TradePay/Approval', 'TRD', 'ASYNC', 1, N'【待办】付款待审', 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'付款申请提交', EventType=N'TASK', PageUrl=N'/TradePay/Approval', MenuGroupCode='TRD', IsGenerateTodo=1, TodoTitle=N'【待办】付款待审', BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE' AND EventCode=N'TRADE.PAY.SUBMIT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='TRADE' AND EventCode=N'TRADE.PAY.APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', N'TRADE.PAY.APPROVED', N'付款已通过', N'NOTICE', NULL, 'TRD', 'ASYNC', 0, NULL, 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'付款已通过', EventType=N'NOTICE', PageUrl=NULL, MenuGroupCode='TRD', IsGenerateTodo=0, TodoTitle=NULL, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE' AND EventCode=N'TRADE.PAY.APPROVED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='TRADE' AND EventCode=N'TRADE.PAY.REJECTED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', N'TRADE.PAY.REJECTED', N'付款已驳回', N'NOTICE', NULL, 'TRD', 'ASYNC', 0, NULL, 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'付款已驳回', EventType=N'NOTICE', PageUrl=NULL, MenuGroupCode='TRD', IsGenerateTodo=0, TodoTitle=NULL, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE' AND EventCode=N'TRADE.PAY.REJECTED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='TRADE' AND EventCode=N'TRADE.OUT.SUBMIT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', N'TRADE.OUT.SUBMIT', N'出库单提交', N'TASK', N'/TradeWH/Out', 'TRD', 'ASYNC', 1, N'【待办】出库待确认', 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'出库单提交', EventType=N'TASK', PageUrl=N'/TradeWH/Out', MenuGroupCode='TRD', IsGenerateTodo=1, TodoTitle=N'【待办】出库待确认', BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE' AND EventCode=N'TRADE.OUT.SUBMIT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='TRADE' AND EventCode=N'TRADE.OUT.APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', N'TRADE.OUT.APPROVED', N'出库已完成', N'NOTICE', NULL, 'TRD', 'ASYNC', 0, NULL, 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'出库已完成', EventType=N'NOTICE', PageUrl=NULL, MenuGroupCode='TRD', IsGenerateTodo=0, TodoTitle=NULL, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE' AND EventCode=N'TRADE.OUT.APPROVED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='TRADE' AND EventCode=N'TRADE.IN.SUBMIT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', N'TRADE.IN.SUBMIT', N'入库单提交', N'TASK', N'/TradeWH/In', 'TRD', 'ASYNC', 1, N'【待办】入库待确认', 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'入库单提交', EventType=N'TASK', PageUrl=N'/TradeWH/In', MenuGroupCode='TRD', IsGenerateTodo=1, TodoTitle=N'【待办】入库待确认', BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE' AND EventCode=N'TRADE.IN.SUBMIT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='TRADE' AND EventCode=N'TRADE.IN.APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', N'TRADE.IN.APPROVED', N'入库已完成', N'NOTICE', NULL, 'TRD', 'ASYNC', 0, NULL, 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'入库已完成', EventType=N'NOTICE', PageUrl=NULL, MenuGroupCode='TRD', IsGenerateTodo=0, TodoTitle=NULL, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE' AND EventCode=N'TRADE.IN.APPROVED';

/* ----- 流转规则 ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode=N'FLOW_TRADE_SO_SUBMIT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FLOW_TRADE_SO_SUBMIT', N'销售提交-经理待办', 'TRADE', N'TRADE.SO.SUBMIT', N'CREATE_TODO', N'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_SALES_MGR'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'销售提交-经理待办', CurrentEvent=N'TRADE.SO.SUBMIT', ActionType=N'CREATE_TODO', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_SALES_MGR'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode=N'FLOW_TRADE_SO_SUBMIT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode=N'FLOW_TRADE_SO_APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FLOW_TRADE_SO_APPROVED', N'销售通过-通知销售', 'TRADE', N'TRADE.SO.APPROVED', N'SEND_NOTICE', N'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_SALES'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'销售通过-通知销售', CurrentEvent=N'TRADE.SO.APPROVED', ActionType=N'SEND_NOTICE', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_SALES'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode=N'FLOW_TRADE_SO_APPROVED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode=N'FLOW_TRADE_SO_REJECTED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FLOW_TRADE_SO_REJECTED', N'销售驳回-通知销售', 'TRADE', N'TRADE.SO.REJECTED', N'SEND_NOTICE', N'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_SALES'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'销售驳回-通知销售', CurrentEvent=N'TRADE.SO.REJECTED', ActionType=N'SEND_NOTICE', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_SALES'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode=N'FLOW_TRADE_SO_REJECTED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode=N'FLOW_TRADE_PO_SUBMIT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FLOW_TRADE_PO_SUBMIT', N'采购提交-经理待办', 'TRADE', N'TRADE.PO.SUBMIT', N'CREATE_TODO', N'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_PUR_MGR'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'采购提交-经理待办', CurrentEvent=N'TRADE.PO.SUBMIT', ActionType=N'CREATE_TODO', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_PUR_MGR'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode=N'FLOW_TRADE_PO_SUBMIT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode=N'FLOW_TRADE_PO_APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FLOW_TRADE_PO_APPROVED', N'采购通过-通知采购', 'TRADE', N'TRADE.PO.APPROVED', N'SEND_NOTICE', N'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_PUR'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'采购通过-通知采购', CurrentEvent=N'TRADE.PO.APPROVED', ActionType=N'SEND_NOTICE', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_PUR'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode=N'FLOW_TRADE_PO_APPROVED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode=N'FLOW_TRADE_PO_REJECTED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FLOW_TRADE_PO_REJECTED', N'采购驳回-通知采购', 'TRADE', N'TRADE.PO.REJECTED', N'SEND_NOTICE', N'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_PUR'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'采购驳回-通知采购', CurrentEvent=N'TRADE.PO.REJECTED', ActionType=N'SEND_NOTICE', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_PUR'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode=N'FLOW_TRADE_PO_REJECTED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode=N'FLOW_TRADE_PAY_SUBMIT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FLOW_TRADE_PAY_SUBMIT', N'付款提交-财务待办', 'TRADE', N'TRADE.PAY.SUBMIT', N'CREATE_TODO', N'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_FIN_MGR'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'付款提交-财务待办', CurrentEvent=N'TRADE.PAY.SUBMIT', ActionType=N'CREATE_TODO', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_FIN_MGR'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode=N'FLOW_TRADE_PAY_SUBMIT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode=N'FLOW_TRADE_PAY_APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FLOW_TRADE_PAY_APPROVED', N'付款通过-通知财务', 'TRADE', N'TRADE.PAY.APPROVED', N'SEND_NOTICE', N'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_FIN'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'付款通过-通知财务', CurrentEvent=N'TRADE.PAY.APPROVED', ActionType=N'SEND_NOTICE', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_FIN'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode=N'FLOW_TRADE_PAY_APPROVED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode=N'FLOW_TRADE_PAY_REJECTED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FLOW_TRADE_PAY_REJECTED', N'付款驳回-通知财务', 'TRADE', N'TRADE.PAY.REJECTED', N'SEND_NOTICE', N'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_FIN'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'付款驳回-通知财务', CurrentEvent=N'TRADE.PAY.REJECTED', ActionType=N'SEND_NOTICE', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_FIN'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode=N'FLOW_TRADE_PAY_REJECTED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode=N'FLOW_TRADE_OUT_SUBMIT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FLOW_TRADE_OUT_SUBMIT', N'出库提交-仓储待办', 'TRADE', N'TRADE.OUT.SUBMIT', N'CREATE_TODO', N'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_WH_MGR'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'出库提交-仓储待办', CurrentEvent=N'TRADE.OUT.SUBMIT', ActionType=N'CREATE_TODO', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_WH_MGR'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode=N'FLOW_TRADE_OUT_SUBMIT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode=N'FLOW_TRADE_OUT_APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FLOW_TRADE_OUT_APPROVED', N'出库完成-通知仓储', 'TRADE', N'TRADE.OUT.APPROVED', N'SEND_NOTICE', N'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_WH'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'出库完成-通知仓储', CurrentEvent=N'TRADE.OUT.APPROVED', ActionType=N'SEND_NOTICE', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_WH'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode=N'FLOW_TRADE_OUT_APPROVED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode=N'FLOW_TRADE_IN_SUBMIT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FLOW_TRADE_IN_SUBMIT', N'入库提交-仓储待办', 'TRADE', N'TRADE.IN.SUBMIT', N'CREATE_TODO', N'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_WH_MGR'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'入库提交-仓储待办', CurrentEvent=N'TRADE.IN.SUBMIT', ActionType=N'CREATE_TODO', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_WH_MGR'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode=N'FLOW_TRADE_IN_SUBMIT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode=N'FLOW_TRADE_IN_APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FLOW_TRADE_IN_APPROVED', N'入库完成-通知仓储', 'TRADE', N'TRADE.IN.APPROVED', N'SEND_NOTICE', N'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_WH'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'入库完成-通知仓储', CurrentEvent=N'TRADE.IN.APPROVED', ActionType=N'SEND_NOTICE', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'TRADE_WH'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode=N'FLOW_TRADE_IN_APPROVED';

/* ----- 事件订阅 ----- */

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES_MGR' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.SO.SUBMIT' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.SO.SUBMIT', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.SO.APPROVED' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.SO.APPROVED', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_SALES' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.SO.REJECTED' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.SO.REJECTED', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_SALES';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_PUR_MGR' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.PO.SUBMIT' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.PO.SUBMIT', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_PUR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_PUR' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.PO.APPROVED' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.PO.APPROVED', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_PUR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_PUR' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.PO.REJECTED' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.PO.REJECTED', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_PUR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_FIN_MGR' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.PAY.SUBMIT' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.PAY.SUBMIT', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_FIN_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_FIN' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.PAY.APPROVED' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.PAY.APPROVED', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_FIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_FIN' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.PAY.REJECTED' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.PAY.REJECTED', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_FIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_WH_MGR' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.OUT.SUBMIT' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.OUT.SUBMIT', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_WH_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_WH_MGR' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.IN.SUBMIT' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.IN.SUBMIT', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_WH_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_WH' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.OUT.APPROVED' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.OUT.APPROVED', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_WH';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_WH' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.IN.APPROVED' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.IN.APPROVED', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_WH';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_CEO' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.SO.SUBMIT' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.SO.SUBMIT', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_CEO';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_CEO' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.PO.SUBMIT' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.PO.SUBMIT', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_CEO';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode=N'TRADE_CEO' AND s.SubType='EVENT' AND s.EventCode=N'TRADE.PAY.SUBMIT' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', N'TRADE.PAY.SUBMIT', 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'TRADE_CEO';

PRINT N'26-Seed_Trade_Company_Default 完成。';
GO
