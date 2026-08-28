# -*- coding: utf-8 -*-
"""生成 26-Seed_Trade_Company_Default.sql（GBK）— 贸易公司组织/职责/事件/订阅默认模板。"""
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "26-Seed_Trade_Company_Default.sql")

# ---------- 数据定义（命名规则：TRADE 前缀） ----------

DEPARTMENTS = [
    # code, cname, parent, level, path, disp
    ("TRADE001", "某某贸易有限公司", None, 1, "/1/", 1),
    ("TRADE002", "总经理办公室", "TRADE001", 2, "/1/1/", 2),
    ("TRADE003", "销售部", "TRADE001", 2, "/1/2/", 3),
    ("TRADE004", "采购部", "TRADE001", 2, "/1/3/", 4),
    ("TRADE005", "仓储物流部", "TRADE001", 2, "/1/4/", 5),
    ("TRADE006", "财务部", "TRADE001", 2, "/1/5/", 6),
    ("TRADE007", "人事行政部", "TRADE001", 2, "/1/6/", 7),
    ("TRADE008", "信息技术部", "TRADE001", 2, "/1/7/", 8),
]

POSITIONS = [
    # code, cname, pos_type, data_scope, disp
    ("POST_CEO", "总经理", "MANAGER", "ALL", 1),
    ("POST_SALES_MGR", "销售经理", "MANAGER", "DEPT", 10),
    ("POST_SALES", "销售员", "SALES", "DEPT", 11),
    ("POST_PUR_MGR", "采购经理", "MANAGER", "DEPT", 20),
    ("POST_PUR", "采购员", "SERVICE", "DEPT", 21),
    ("POST_WH_MGR", "仓储主管", "MANAGER", "DEPT", 30),
    ("POST_WH", "仓管员", "SERVICE", "DEPT", 31),
    ("POST_FIN_MGR", "财务经理", "FINANCE", "DEPT", 40),
    ("POST_FIN", "会计", "FINANCE", "DEPT", 41),
    ("POST_HR_MGR", "人事主管", "MANAGER", "DEPT", 50),
    ("POST_HR", "人事专员", "SERVICE", "DEPT", 51),
    ("POST_ADMIN", "系统管理员", "ADMIN", "ALL", 90),
    ("POST_STAFF", "普通职员", "SERVICE", "DEPT", 99),
]

DUTIES = [
    # code, cname, category, disp
    ("TRADE_ADMIN", "贸易系统管理员", "ADMIN", 1),
    ("TRADE_VIEWER", "贸易只读查询", "VIEW", 2),
    ("TRADE_CEO", "总经理审批", "APPROVAL", 10),
    ("TRADE_SALES_MGR", "销售审批", "APPROVAL", 11),
    ("TRADE_SALES", "销售业务", "SERVICE", 12),
    ("TRADE_PUR_MGR", "采购审批", "APPROVAL", 21),
    ("TRADE_PUR", "采购业务", "SERVICE", 22),
    ("TRADE_WH_MGR", "仓储审批", "APPROVAL", 31),
    ("TRADE_WH", "仓储业务", "SERVICE", 32),
    ("TRADE_FIN_MGR", "财务审批", "APPROVAL", 41),
    ("TRADE_FIN", "财务业务", "SERVICE", 42),
    ("TRADE_HR_MGR", "人事审批", "APPROVAL", 51),
    ("TRADE_HR", "人事业务", "SERVICE", 52),
]

# post, duty, business_limit
POSITION_DUTIES = [
    ("POST_CEO", "TRADE_CEO", "111100"),
    ("POST_CEO", "TRADE_VIEWER", "110000"),
    ("POST_SALES_MGR", "TRADE_SALES_MGR", "111100"),
    ("POST_SALES_MGR", "TRADE_SALES", "111100"),
    ("POST_SALES", "TRADE_SALES", "111100"),
    ("POST_PUR_MGR", "TRADE_PUR_MGR", "111100"),
    ("POST_PUR_MGR", "TRADE_PUR", "111100"),
    ("POST_PUR", "TRADE_PUR", "111100"),
    ("POST_WH_MGR", "TRADE_WH_MGR", "111100"),
    ("POST_WH_MGR", "TRADE_WH", "111100"),
    ("POST_WH", "TRADE_WH", "111100"),
    ("POST_FIN_MGR", "TRADE_FIN_MGR", "111100"),
    ("POST_FIN_MGR", "TRADE_FIN", "111100"),
    ("POST_FIN", "TRADE_FIN", "111100"),
    ("POST_HR_MGR", "TRADE_HR_MGR", "111100"),
    ("POST_HR_MGR", "TRADE_HR", "111100"),
    ("POST_HR", "TRADE_HR", "111100"),
    ("POST_ADMIN", "TRADE_ADMIN", "111111"),
    ("POST_ADMIN", "TRADE_VIEWER", "110000"),
    ("POST_STAFF", "TRADE_VIEWER", "110000"),
]

USERS = [
    # login, name, dept, post, primary
    ("tradeadmin", "系统管理员", "TRADE008", "POST_ADMIN", 1),
    ("tradeceo", "张总", "TRADE002", "POST_CEO", 1),
    ("salesmgr", "李销售经理", "TRADE003", "POST_SALES_MGR", 1),
    ("sales01", "王销售", "TRADE003", "POST_SALES", 1),
    ("purmgr", "赵采购经理", "TRADE004", "POST_PUR_MGR", 1),
    ("pur01", "钱采购", "TRADE004", "POST_PUR", 1),
    ("whmgr", "孙仓储主管", "TRADE005", "POST_WH_MGR", 1),
    ("finmgr", "周财务经理", "TRADE006", "POST_FIN_MGR", 1),
    ("hrmgr", "吴人事主管", "TRADE007", "POST_HR_MGR", 1),
]

TRADE_RESOURCES = [
    # rid, name, path, disp
    ("RES.TRADE.Home", "贸易工作台", "/TradeHome/Index", 10),
    ("RES.TRADE.SO.List", "销售订单", "/TradeSO/Index", 20),
    ("RES.TRADE.SO.Create", "新建销售订单", "/TradeSO/Create", 21),
    ("RES.TRADE.SO.Approval", "销售订单审批", "/TradeSO/Approval", 22),
    ("RES.TRADE.PO.List", "采购申请", "/TradePO/Index", 30),
    ("RES.TRADE.PO.Create", "新建采购申请", "/TradePO/Create", 31),
    ("RES.TRADE.PO.Approval", "采购申请审批", "/TradePO/Approval", 32),
    ("RES.TRADE.PAY.List", "付款申请", "/TradePay/Index", 40),
    ("RES.TRADE.PAY.Approval", "付款审批", "/TradePay/Approval", 41),
    ("RES.TRADE.WH.Out", "出库管理", "/TradeWH/Out", 50),
    ("RES.TRADE.WH.In", "入库管理", "/TradeWH/In", 51),
    ("RES.TRADE.MyTodo", "我的待办", "/ETodoTask/Index", 60),
]

# duty, resource, function_limit
RESOURCE_SUBS = [
    ("TRADE_ADMIN", "RES.CF.Home", "111111"),
    ("TRADE_ADMIN", "RES.CF.EUsers", "111111"),
    ("TRADE_ADMIN", "RES.CF.EDept", "111111"),
    ("TRADE_ADMIN", "RES.CF.EPosition", "111111"),
    ("TRADE_ADMIN", "RES.CF.EDuty", "111111"),
    ("TRADE_ADMIN", "RES.CF.EUserPosition", "111111"),
    ("TRADE_ADMIN", "RES.CF.EPositionDuty", "111111"),
    ("TRADE_ADMIN", "RES.CF.ESubscription", "111111"),
    ("TRADE_ADMIN", "RES.CF.EEventConfig", "111111"),
    ("TRADE_ADMIN", "RES.CF.EEventFlowRule", "111111"),
    ("TRADE_ADMIN", "RES.CF.ETodoTask", "111111"),
    ("TRADE_ADMIN", "RES.DASH.Board", "110000"),
    ("TRADE_ADMIN", "RES.DASH.PosTemplate", "111111"),
    ("TRADE_CEO", "RES.CF.Home", "110000"),
    ("TRADE_CEO", "RES.DASH.Board", "110000"),
    ("TRADE_CEO", "RES.TRADE.Home", "110000"),
    ("TRADE_CEO", "RES.TRADE.SO.Approval", "111100"),
    ("TRADE_CEO", "RES.TRADE.PO.Approval", "111100"),
    ("TRADE_CEO", "RES.TRADE.PAY.Approval", "111100"),
    ("TRADE_CEO", "RES.TRADE.MyTodo", "110000"),
    ("TRADE_CEO", "RES.CF.ETodoTask", "110000"),
    ("TRADE_SALES_MGR", "RES.CF.Home", "110000"),
    ("TRADE_SALES_MGR", "RES.DASH.Board", "110000"),
    ("TRADE_SALES_MGR", "RES.TRADE.Home", "110000"),
    ("TRADE_SALES_MGR", "RES.TRADE.SO.List", "110000"),
    ("TRADE_SALES_MGR", "RES.TRADE.SO.Approval", "111100"),
    ("TRADE_SALES_MGR", "RES.TRADE.MyTodo", "110000"),
    ("TRADE_SALES_MGR", "RES.CF.ETodoTask", "110000"),
    ("TRADE_SALES", "RES.CF.Home", "110000"),
    ("TRADE_SALES", "RES.DASH.Board", "110000"),
    ("TRADE_SALES", "RES.TRADE.Home", "110000"),
    ("TRADE_SALES", "RES.TRADE.SO.List", "110000"),
    ("TRADE_SALES", "RES.TRADE.SO.Create", "111100"),
    ("TRADE_SALES", "RES.TRADE.MyTodo", "110000"),
    ("TRADE_PUR_MGR", "RES.CF.Home", "110000"),
    ("TRADE_PUR_MGR", "RES.DASH.Board", "110000"),
    ("TRADE_PUR_MGR", "RES.TRADE.PO.List", "110000"),
    ("TRADE_PUR_MGR", "RES.TRADE.PO.Approval", "111100"),
    ("TRADE_PUR_MGR", "RES.TRADE.MyTodo", "110000"),
    ("TRADE_PUR", "RES.CF.Home", "110000"),
    ("TRADE_PUR", "RES.TRADE.PO.List", "110000"),
    ("TRADE_PUR", "RES.TRADE.PO.Create", "111100"),
    ("TRADE_PUR", "RES.TRADE.MyTodo", "110000"),
    ("TRADE_WH_MGR", "RES.CF.Home", "110000"),
    ("TRADE_WH_MGR", "RES.TRADE.WH.Out", "111100"),
    ("TRADE_WH_MGR", "RES.TRADE.WH.In", "111100"),
    ("TRADE_WH_MGR", "RES.TRADE.MyTodo", "110000"),
    ("TRADE_WH", "RES.CF.Home", "110000"),
    ("TRADE_WH", "RES.TRADE.WH.Out", "111100"),
    ("TRADE_WH", "RES.TRADE.WH.In", "111100"),
    ("TRADE_FIN_MGR", "RES.CF.Home", "110000"),
    ("TRADE_FIN_MGR", "RES.TRADE.PAY.List", "110000"),
    ("TRADE_FIN_MGR", "RES.TRADE.PAY.Approval", "111100"),
    ("TRADE_FIN_MGR", "RES.TRADE.MyTodo", "110000"),
    ("TRADE_FIN", "RES.CF.Home", "110000"),
    ("TRADE_FIN", "RES.TRADE.PAY.List", "110000"),
    ("TRADE_HR_MGR", "RES.CF.Home", "110000"),
    ("TRADE_HR_MGR", "RES.HR.LeaveApproval", "111100"),
    ("TRADE_HR_MGR", "RES.HR.MyTodo", "110000"),
    ("TRADE_HR", "RES.CF.Home", "110000"),
    ("TRADE_HR", "RES.HR.LeaveApply", "111100"),
    ("TRADE_HR", "RES.HR.MyLeave", "110000"),
    ("TRADE_VIEWER", "RES.CF.Home", "110000"),
    ("TRADE_VIEWER", "RES.DASH.Board", "110000"),
]

EVENTS = [
    # code, name, etype, page, todo_gen, todo_title
    ("TRADE.SO.SUBMIT", "销售订单提交", "TASK", "/TradeSO/Approval", 1, "【待办】销售订单待审"),
    ("TRADE.SO.APPROVED", "销售订单通过", "NOTICE", None, 0, None),
    ("TRADE.SO.REJECTED", "销售订单驳回", "NOTICE", None, 0, None),
    ("TRADE.PO.SUBMIT", "采购申请提交", "TASK", "/TradePO/Approval", 1, "【待办】采购申请待审"),
    ("TRADE.PO.APPROVED", "采购申请通过", "NOTICE", None, 0, None),
    ("TRADE.PO.REJECTED", "采购申请驳回", "NOTICE", None, 0, None),
    ("TRADE.PAY.SUBMIT", "付款申请提交", "TASK", "/TradePay/Approval", 1, "【待办】付款待审"),
    ("TRADE.PAY.APPROVED", "付款已通过", "NOTICE", None, 0, None),
    ("TRADE.PAY.REJECTED", "付款已驳回", "NOTICE", None, 0, None),
    ("TRADE.OUT.SUBMIT", "出库单提交", "TASK", "/TradeWH/Out", 1, "【待办】出库待确认"),
    ("TRADE.OUT.APPROVED", "出库已完成", "NOTICE", None, 0, None),
    ("TRADE.IN.SUBMIT", "入库单提交", "TASK", "/TradeWH/In", 1, "【待办】入库待确认"),
    ("TRADE.IN.APPROVED", "入库已完成", "NOTICE", None, 0, None),
]

# rule_code, name, current, action, target_type, target_duty, disp
FLOW_RULES = [
    ("FLOW_TRADE_SO_SUBMIT", "销售提交-经理待办", "TRADE.SO.SUBMIT", "CREATE_TODO", "DUTY", "TRADE_SALES_MGR", 10),
    ("FLOW_TRADE_SO_APPROVED", "销售通过-通知销售", "TRADE.SO.APPROVED", "SEND_NOTICE", "DUTY", "TRADE_SALES", 10),
    ("FLOW_TRADE_SO_REJECTED", "销售驳回-通知销售", "TRADE.SO.REJECTED", "SEND_NOTICE", "DUTY", "TRADE_SALES", 10),
    ("FLOW_TRADE_PO_SUBMIT", "采购提交-经理待办", "TRADE.PO.SUBMIT", "CREATE_TODO", "DUTY", "TRADE_PUR_MGR", 10),
    ("FLOW_TRADE_PO_APPROVED", "采购通过-通知采购", "TRADE.PO.APPROVED", "SEND_NOTICE", "DUTY", "TRADE_PUR", 10),
    ("FLOW_TRADE_PO_REJECTED", "采购驳回-通知采购", "TRADE.PO.REJECTED", "SEND_NOTICE", "DUTY", "TRADE_PUR", 10),
    ("FLOW_TRADE_PAY_SUBMIT", "付款提交-财务待办", "TRADE.PAY.SUBMIT", "CREATE_TODO", "DUTY", "TRADE_FIN_MGR", 10),
    ("FLOW_TRADE_PAY_APPROVED", "付款通过-通知财务", "TRADE.PAY.APPROVED", "SEND_NOTICE", "DUTY", "TRADE_FIN", 10),
    ("FLOW_TRADE_PAY_REJECTED", "付款驳回-通知财务", "TRADE.PAY.REJECTED", "SEND_NOTICE", "DUTY", "TRADE_FIN", 10),
    ("FLOW_TRADE_OUT_SUBMIT", "出库提交-仓储待办", "TRADE.OUT.SUBMIT", "CREATE_TODO", "DUTY", "TRADE_WH_MGR", 10),
    ("FLOW_TRADE_OUT_APPROVED", "出库完成-通知仓储", "TRADE.OUT.APPROVED", "SEND_NOTICE", "DUTY", "TRADE_WH", 10),
    ("FLOW_TRADE_IN_SUBMIT", "入库提交-仓储待办", "TRADE.IN.SUBMIT", "CREATE_TODO", "DUTY", "TRADE_WH_MGR", 10),
    ("FLOW_TRADE_IN_APPROVED", "入库完成-通知仓储", "TRADE.IN.APPROVED", "SEND_NOTICE", "DUTY", "TRADE_WH", 10),
]

# duty, event_code (EVENT subscription)
EVENT_SUBS = [
    ("TRADE_SALES_MGR", "TRADE.SO.SUBMIT"),
    ("TRADE_SALES", "TRADE.SO.APPROVED"),
    ("TRADE_SALES", "TRADE.SO.REJECTED"),
    ("TRADE_PUR_MGR", "TRADE.PO.SUBMIT"),
    ("TRADE_PUR", "TRADE.PO.APPROVED"),
    ("TRADE_PUR", "TRADE.PO.REJECTED"),
    ("TRADE_FIN_MGR", "TRADE.PAY.SUBMIT"),
    ("TRADE_FIN", "TRADE.PAY.APPROVED"),
    ("TRADE_FIN", "TRADE.PAY.REJECTED"),
    ("TRADE_WH_MGR", "TRADE.OUT.SUBMIT"),
    ("TRADE_WH_MGR", "TRADE.IN.SUBMIT"),
    ("TRADE_WH", "TRADE.OUT.APPROVED"),
    ("TRADE_WH", "TRADE.IN.APPROVED"),
    ("TRADE_CEO", "TRADE.SO.SUBMIT"),
    ("TRADE_CEO", "TRADE.PO.SUBMIT"),
    ("TRADE_CEO", "TRADE.PAY.SUBMIT"),
]


def n(s: str) -> str:
    return "N'" + s.replace("'", "''") + "'"


def sql_header() -> str:
    return """/*
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

"""


def gen_sql() -> str:
    lines = [sql_header()]

    # AppModule + MenuGroup
    lines.append("/* ----- 应用模块 TRADE ----- */\n")
    lines.append("""
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

""")

    # Departments
    lines.append("/* ----- 部门（先根后子） ----- */\n")
    for code, cname, parent, level, path, disp in DEPARTMENTS:
        if parent is None:
            lines.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode={n(code)})
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ({n(code)}, {n(cname)}, {level}, {n(path)}, 'COMPANY', {disp}, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Department SET DeptCName={n(cname)}, DeptLevel={level}, DeptPath={n(path)}, DispSeq={disp}, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DeptCode={n(code)};
""")
        else:
            lines.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode={n(code)})
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, ParentDeptID, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT {n(code)}, {n(cname)}, p.DataID, {level}, {n(path)}, 'DEPT', {disp}, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Department p WHERE p.DeptCode={n(parent)};
ELSE
    UPDATE dbo.Tbl_E_Department SET DeptCName={n(cname)}, DeptLevel={level}, DeptPath={n(path)}, DispSeq={disp}, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DeptCode={n(code)};
""")

    # Positions
    lines.append("\n/* ----- 岗位 ----- */\n")
    for code, cname, ptype, scope, disp in POSITIONS:
        lines.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode={n(code)})
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PositionType, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ({n(code)}, {n(cname)}, {n(ptype)}, {n(scope)}, {disp}, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName={n(cname)}, PositionType={n(ptype)}, DataScope={n(scope)}, DispSeq={disp}, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode={n(code)};
""")

    # Duties
    lines.append("\n/* ----- 职责 ----- */\n")
    for code, cname, cat, disp in DUTIES:
        lines.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode={n(code)})
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ({n(code)}, {n(cname)}, {n(cat)}, {disp}, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName={n(cname)}, DutyCategory={n(cat)}, DutyDispSeq={disp}, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode={n(code)};
""")

    # PositionDuty
    lines.append("\n/* ----- 岗位职责 ----- */\n")
    for post, duty, bl in POSITION_DUTIES:
        lines.append(f"""
IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode={n(post)} AND d.DutyCode={n(duty)} AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, {n(bl)}, 1, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode={n(post)} AND d.DutyCode={n(duty)};
""")

    # Users
    lines.append("\n/* ----- 演示用户（密码 123456） ----- */\n")
    for login, name, _, _, _ in USERS:
        lines.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Users WHERE LoginId={n(login)})
    INSERT INTO dbo.Tbl_E_Users (LoginId, RealName, PwdHash, PasswordAlgo, PasswordVersion, UserType, LoginCount, MaxLoginCount, PwdErrorCount, MaxPwdErrorCount, IsLocked, IsEnabled, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ({n(login)}, {n(name)}, @Pwd, 'MD5_16', 1, 'EMPLOYEE', 0, 99999, 0, 5, 0, 1, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Users SET RealName={n(name)}, PwdHash=@Pwd, PasswordAlgo='MD5_16', IsEnabled=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE LoginId={n(login)};
""")

    # UserPosition
    lines.append("\n/* ----- 用户任岗 ----- */\n")
    for login, _, dept, post, primary in USERS:
        ip = 1 if primary else 0
        lines.append(f"""
IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId={n(login)} AND d.DeptCode={n(dept)} AND p.PostCode={n(post)} AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, {ip}, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode={n(dept)}
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode={n(post)}
    WHERE u.LoginId={n(login)};
""")

    # Resources
    lines.append("\n/* ----- 贸易菜单资源 ----- */\n")
    lines.append("IF OBJECT_ID('tempdb..#TradeRes') IS NOT NULL DROP TABLE #TradeRes;\n")
    lines.append("CREATE TABLE #TradeRes (ResourceID VARCHAR(50) NOT NULL PRIMARY KEY, ResourceName NVARCHAR(100) NOT NULL, MenuPath NVARCHAR(300) NOT NULL, DispSeq INT NOT NULL);\n")
    lines.append("INSERT INTO #TradeRes VALUES\n")
    vals = []
    for rid, name, path, disp in TRADE_RESOURCES:
        vals.append(f"({n(rid)}, {n(name)}, {n(path)}, {disp})")
    lines.append(",\n".join(vals) + ";\n")
    lines.append("""
MERGE dbo.Tbl_E_Resource AS t
USING (SELECT 'TRADE' AS AppCode, 'TRD' AS MenuGroupCode, s.* FROM #TradeRes s) AS s
ON t.ResourceID = s.ResourceID
WHEN NOT MATCHED THEN
    INSERT (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (s.AppCode, s.ResourceID, s.ResourceName, 'MENU', s.MenuPath, s.MenuGroupCode, s.DispSeq, '1', 0, @Now, @Now, @Op)
WHEN MATCHED THEN
    UPDATE SET ResourceName=s.ResourceName, MenuPath=s.MenuPath, MenuGroupCode=s.MenuGroupCode, DispSeq=s.DispSeq, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op;
DROP TABLE #TradeRes;

""")

    # Resource subscriptions
    lines.append("\n/* ----- 资源订阅 ----- */\n")
    for duty, res, fl in RESOURCE_SUBS:
        lines.append(f"""
IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode={n(duty)} AND s.SubType='RESOURCE' AND s.ResourceID={n(res)} AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', {n(res)}, 1, {n(fl)}, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode={n(duty)};
""")

    # Event config
    lines.append("\n/* ----- 事件配置 ----- */\n")
    for code, name, etype, page, todo_gen, todo_title in EVENTS:
        page_sql = "NULL" if not page else n(page)
        title_sql = "NULL" if not todo_title else n(todo_title)
        lines.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='TRADE' AND EventCode={n(code)} AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('TRADE', {n(code)}, {n(name)}, {n(etype)}, {page_sql}, 'TRD', 'ASYNC', {todo_gen}, {title_sql}, 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName={n(name)}, EventType={n(etype)}, PageUrl={page_sql}, MenuGroupCode='TRD', IsGenerateTodo={todo_gen}, TodoTitle={title_sql}, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='TRADE' AND EventCode={n(code)};
""")

    # Flow rules
    lines.append("\n/* ----- 流转规则 ----- */\n")
    for rcode, rname, cur, action, ttype, tduty, disp in FLOW_RULES:
        lines.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode={n(rcode)} AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ({n(rcode)}, {n(rname)}, 'TRADE', {n(cur)}, {n(action)}, {n(ttype)}, (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode={n(tduty)}), 'SINGLE', {disp}, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName={n(rname)}, CurrentEvent={n(cur)}, ActionType={n(action)}, TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode={n(tduty)}), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode={n(rcode)};
""")

    # Event subscriptions
    lines.append("\n/* ----- 事件订阅 ----- */\n")
    for duty, ev in EVENT_SUBS:
        lines.append(f"""
IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode={n(duty)} AND s.SubType='EVENT' AND s.EventCode={n(ev)} AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, IsPrimary, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'TRADE', 'EVENT', {n(ev)}, 1, 50, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode={n(duty)};
""")

    lines.append("\nPRINT N'26-Seed_Trade_Company_Default 完成。';\nGO\n")
    return "".join(lines)


def main() -> None:
    content = gen_sql()
    with open(OUT, "w", encoding="gbk", errors="replace") as f:
        f.write(content)
    print("Written:", OUT, "bytes:", os.path.getsize(OUT))


if __name__ == "__main__":
    main()
