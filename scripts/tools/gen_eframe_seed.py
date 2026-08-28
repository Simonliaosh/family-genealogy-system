# -*- coding: utf-8 -*-
"""从 EFrame.xls 生成 EFrame 框架种子 SQL（GBK/ANSI）。运行：python gen_eframe_seed.py"""
import os
import xlrd
from datetime import datetime, timedelta

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
XLS = os.path.join(ROOT, "EFrame.xls")
OUT = ROOT

PWD_MD5 = "49ba59abbe56e057"
OP = "SEED-EFRAME"

SKIP_SHEETS = {
    "Tbl_E_DataScopeRule", "Tbl_E_DutyResourceAction", "Tbl_E_EventDelivery",
    "Tbl_E_EventInstance", "Tbl_E_EventLog", "Tbl_E_EventReceiver",
    "Tbl_E_HrLeaveRequest", "Tbl_E_LoginLog", "Tbl_E_OperationLog",
    "Tbl_E_ResourceAction", "Tbl_E_TodoCandidate", "Tbl_E_TodoGroup",
    "Tbl_E_TodoTask", "Tbl_E_TodoTaskLog", "Tbl_E_UserDelegate", "Tbl_E_UserHandover",
}

# Excel 中 Duty DataID -> DutyCode
DUTY_ID_MAP = {1: "CF_ADMIN", 2: "CF_VIEWER", 3: "CF_HR_MGR", 4: "CF_HR_EMP"}
POS_ID_MAP = {1: "CF001", 2: "CF_ADMIN", 3: "CF_STAFF", 4: "HR_MGR", 5: "HR_EMP"}
DEPT_ID_MAP = {1: "CF001", 2: "CF002", 3: "HR001"}
USER_ID_LOGIN = {1: "cfadmin", 4: "admin", 6: "lisi", 7: "frameop", 8: "testuser", 9: "hrstaff", 10: "hrmgr"}

SEED_USERS = [
    ("cfadmin", "超级管理员", "EMPLOYEE"),
    ("frameop", "框架运维", "EMPLOYEE"),
    ("testuser", "测试用户", "EMPLOYEE"),
    ("hrstaff", "人事员工", "EMPLOYEE"),
    ("hrmgr", "人事主管", "EMPLOYEE"),
]

USER_POSITIONS = [
    ("cfadmin", "CF001", "CF001"),
    ("cfadmin", "CF001", "CF_ADMIN"),
    ("frameop", "CF001", "CF_ADMIN"),
    ("testuser", "CF001", "CF_STAFF"),
    ("hrstaff", "HR001", "HR_EMP"),
    ("hrmgr", "HR001", "HR_MGR"),
]


def sql_str(v):
    if v is None:
        return "NULL"
    if isinstance(v, float):
        if v == int(v):
            v = int(v)
        else:
            return str(v)
    s = str(v).strip()
    if s == "" or s.lower() == "none":
        return "NULL"
    return "N'" + s.replace("'", "''") + "'"


def sql_varchar(v):
    if v is None:
        return "NULL"
    s = str(v).strip()
    if s == "":
        return "NULL"
    return "'" + s.replace("'", "''") + "'"


def sql_bit(v):
    if v is None or str(v).strip() == "":
        return "0"
    if isinstance(v, (int, float)):
        return "1" if v != 0 else "0"
    s = str(v).strip().lower()
    return "1" if s in ("1", "true", "-1", "yes") else "0"


def sql_int(v):
    if v is None or str(v).strip() == "":
        return "NULL"
    if isinstance(v, float):
        return str(int(v))
    return str(int(float(v)))


def excel_date(v):
    if v is None or str(v).strip() == "":
        return "NULL"
    if isinstance(v, (int, float)) and v > 1000:
        dt = datetime(1899, 12, 30) + timedelta(days=float(v))
        return "'" + dt.strftime("%Y-%m-%d %H:%M:%S") + "'"
    return sql_str(v)


def header(title, order, deps=""):
    lines = [
        "/*",
        "=" * 78,
        f"  {title}",
        "=" * 78,
        "  来源：EFrame.xls + EFrame 架构对齐（幂等 MERGE / IF NOT EXISTS）",
        "  前置：docs/EFrame_CreateTables.sql、docs/EFrame_v2_supplement.sql",
    ]
    if deps:
        lines.append(f"  依赖：{deps}")
    lines.extend([
        f"  顺序：第 {order} 步",
        "  编码：ANSI (GBK)",
        "=" * 78,
        "*/",
        "SET NOCOUNT ON;",
        "SET XACT_ABORT ON;",
        "GO",
        "",
        "DECLARE @Now DATETIME = GETDATE();",
        f"DECLARE @Op VARCHAR(30) = '{OP}';",
        "",
    ])
    return "\n".join(lines)


def footer(name):
    return f"\nPRINT N'{name} 完成。';\nGO\n"


def write_file(name, content):
    path = os.path.join(OUT, name)
    with open(path, "w", encoding="gbk", errors="replace") as f:
        f.write(content)
    print("Wrote", path, len(content), "bytes")


def read_sheet(wb, name):
    sh = wb.sheet_by_name(name)
    headers = [str(sh.cell_value(0, c)).strip() for c in range(sh.ncols)]
    rows = []
    for r in range(1, sh.nrows):
        row = {}
        empty = True
        for c, h in enumerate(headers):
            if not h:
                continue
            v = sh.cell_value(r, c)
            if isinstance(v, str):
                v = v.strip()
            if v not in ("", None):
                empty = False
            row[h] = v
        if not empty:
            rows.append(row)
    return headers, rows


def gen_foundation(wb):
    parts = [header("EFrame 种子 01 - 基础数据（应用模块/菜单组/字典）", 1)]

    _, apps = read_sheet(wb, "Tbl_E_AppModule")
    parts.append("/* ----- 应用模块 ----- */")
    for r in apps:
        code = r.get("AppCode", "")
        if not code:
            continue
        parts.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_AppModule WHERE AppCode = {sql_varchar(code)})
    INSERT INTO dbo.Tbl_E_AppModule (AppCode, AppName, AppType, BaseUrl, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ({sql_varchar(code)}, {sql_str(r.get('AppName'))}, {sql_varchar(r.get('AppType','BUSINESS'))}, {sql_str(r.get('BaseUrl'))}, {sql_int(r.get('DispSeq',99))}, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_AppModule SET AppName={sql_str(r.get('AppName'))}, AppType={sql_varchar(r.get('AppType','BUSINESS'))},
        BaseUrl={sql_str(r.get('BaseUrl'))}, DispSeq={sql_int(r.get('DispSeq',99))}, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode={sql_varchar(code)};""")

    _, groups = read_sheet(wb, "Tbl_E_MenuGroup")
    parts.append("\n/* ----- 菜单组 ----- */")
    parts.append("""
;MERGE dbo.Tbl_E_MenuGroup AS t
USING (VALUES""")
    gvals = []
    for r in groups:
        code = r.get("MenuGroupCode", "")
        if not code:
            continue
        gvals.append(f"    ({sql_varchar(code)}, {sql_varchar(r.get('AppCode','FRAME'))}, {sql_str(r.get('MenuGroupName'))}, {sql_int(r.get('DispSeq',99))})")
    parts.append(",\n".join(gvals))
    parts.append(""") AS s (MenuGroupCode, AppCode, MenuGroupName, DispSeq)
ON t.MenuGroupCode = s.MenuGroupCode
WHEN NOT MATCHED THEN
    INSERT (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (s.MenuGroupCode, s.AppCode, s.MenuGroupName, s.DispSeq, '1', 0, @Now, @Now, @Op)
WHEN MATCHED THEN
    UPDATE SET AppCode=s.AppCode, MenuGroupName=s.MenuGroupName, DispSeq=s.DispSeq, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op;""")

    _, dtypes = read_sheet(wb, "Tbl_E_DictType")
    parts.append("\n/* ----- 字典类型 ----- */")
    for r in dtypes:
        code = r.get("DictTypeCode", "")
        if not code:
            continue
        parts.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictType WHERE DictTypeCode = {sql_varchar(code)})
    INSERT INTO dbo.Tbl_E_DictType (DictTypeCode, DictTypeName, AppCode, IsSystem, IsEditable, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ({sql_varchar(code)}, {sql_str(r.get('DictTypeName'))}, {sql_varchar(r.get('AppCode','FRAME'))}, {sql_bit(r.get('IsSystem'))}, {sql_bit(r.get('IsEditable',1))}, {sql_str(r.get('Remark'))}, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictType SET DictTypeName={sql_str(r.get('DictTypeName'))}, AppCode={sql_varchar(r.get('AppCode','FRAME'))},
        IsSystem={sql_bit(r.get('IsSystem'))}, IsEditable={sql_bit(r.get('IsEditable',1))}, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode={sql_varchar(code)};""")

    _, ditems = read_sheet(wb, "Tbl_E_DictItem")
    parts.append("\n/* ----- 字典项 ----- */")
    for r in ditems:
        tc = r.get("DictTypeCode", "")
        ic = r.get("ItemCode", "")
        if not tc or not ic:
            continue
        parts.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_DictItem WHERE DictTypeCode={sql_varchar(tc)} AND ItemCode={sql_str(ic)})
    INSERT INTO dbo.Tbl_E_DictItem (DictTypeCode, ItemCode, ItemName, ItemNameEn, ParentItemCode, DispSeq, ExtJson, IsSystem, Remark, BStatus, IsDeleted, CreateDate, AmendDate)
    VALUES ({sql_varchar(tc)}, {sql_str(ic)}, {sql_str(r.get('ItemName'))}, {sql_str(r.get('ItemNameEn'))}, {sql_str(r.get('ParentItemCode'))}, {sql_int(r.get('DispSeq',99))}, {sql_str(r.get('ExtJson'))}, {sql_bit(r.get('IsSystem'))}, {sql_str(r.get('Remark'))}, '1', 0, @Now, @Now);
ELSE
    UPDATE dbo.Tbl_E_DictItem SET ItemName={sql_str(r.get('ItemName'))}, DispSeq={sql_int(r.get('DispSeq',99))}, BStatus='1', IsDeleted=0, AmendDate=@Now
    WHERE DictTypeCode={sql_varchar(tc)} AND ItemCode={sql_str(ic)};""")

    parts.append(footer("20-Seed_Foundation"))
    write_file("20-Seed_Foundation.sql", "\n".join(parts))


def gen_organization(wb):
    parts = [header("EFrame 种子 02 - 组织架构（部门/岗位/职责/岗位职责）", 2, "20-Seed_Foundation.sql")]

    _, depts = read_sheet(wb, "Tbl_E_Department")
    parts.append("/* ----- 部门（先主后子） ----- */")
    for r in sorted(depts, key=lambda x: float(x.get("DeptLevel", 1) or 1)):
        code = r.get("DeptCode", "")
        if not code:
            continue
        parent = r.get("ParentDeptID", "")
        if parent and str(parent).strip() not in ("", "0"):
            pid = int(float(parent))
            pcode = DEPT_ID_MAP.get(pid)
            if pcode:
                parts.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode={sql_str(code)})
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, DeptEName, ParentDeptID, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT {sql_str(code)}, {sql_str(r.get('DeptCName'))}, {sql_str(r.get('DeptEName'))}, p.DataID, {sql_int(r.get('DeptLevel',1))}, {sql_str(r.get('DeptPath'))}, {sql_str(r.get('DeptType'))}, {sql_int(r.get('DispSeq',99))}, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Department p WHERE p.DeptCode={sql_str(pcode)};""")
                continue
        parts.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode={sql_str(code)})
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, DeptEName, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ({sql_str(code)}, {sql_str(r.get('DeptCName'))}, {sql_str(r.get('DeptEName'))}, {sql_int(r.get('DeptLevel',1))}, {sql_str(r.get('DeptPath'))}, {sql_str(r.get('DeptType'))}, {sql_int(r.get('DispSeq',99))}, '1', 0, @Now, @Now, @Op);""")

    _, poses = read_sheet(wb, "Tbl_E_Position")
    parts.append("\n/* ----- 岗位 ----- */")
    for r in poses:
        code = r.get("PostCode", "")
        if not code:
            continue
        parts.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode={sql_str(code)})
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PostEName, PositionType, DataScope, DispSeq, DDescription, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ({sql_str(code)}, {sql_str(r.get('PostCName'))}, {sql_str(r.get('PostEName'))}, {sql_str(r.get('PositionType'))}, {sql_varchar(r.get('DataScope','SELF'))}, {sql_int(r.get('DispSeq',99))}, {sql_str(r.get('DDescription'))}, {sql_str(r.get('Remark'))}, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName={sql_str(r.get('PostCName'))}, DataScope={sql_varchar(r.get('DataScope','SELF'))}, DispSeq={sql_int(r.get('DispSeq',99))}, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode={sql_str(code)};""")

    _, duties = read_sheet(wb, "Tbl_E_Duty")
    parts.append("\n/* ----- 职责 ----- */")
    for r in duties:
        code = r.get("DutyCode", "")
        if not code:
            continue
        parts.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode={sql_str(code)})
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyEName, DutyCategory, DutyDispSeq, DDescription, DutyFlow, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ({sql_str(code)}, {sql_str(r.get('DutyCName'))}, {sql_str(r.get('DutyEName'))}, {sql_varchar(r.get('DutyCategory'))}, {sql_int(r.get('DutyDispSeq',99))}, {sql_str(r.get('DDescription'))}, {sql_str(r.get('DutyFlow'))}, {sql_str(r.get('Remark'))}, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName={sql_str(r.get('DutyCName'))}, DutyCategory={sql_varchar(r.get('DutyCategory'))}, DutyDispSeq={sql_int(r.get('DutyDispSeq',99))}, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode={sql_str(code)};""")

    _, pd = read_sheet(wb, "Tbl_E_PositionDuty")
    parts.append("\n/* ----- 岗位职责 ----- */")
    for r in pd:
        pos_id = int(float(r.get("PosID", 0)))
        duty_id = int(float(r.get("DutyID", 0)))
        pos_code = POS_ID_MAP.get(pos_id)
        duty_code = DUTY_ID_MAP.get(duty_id)
        if not pos_code or not duty_code:
            continue
        bl = r.get("BusinessLimit", "111111") or "111111"
        parts.append(f"""
IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode={sql_str(pos_code)} AND d.DutyCode={sql_str(duty_code)} AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, DeptID, BusinessLimit, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, NULL, {sql_varchar(str(bl).strip())}, {sql_int(r.get('DispSeq',99))}, {sql_str(r.get('Remark'))}, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode={sql_str(pos_code)} AND d.DutyCode={sql_str(duty_code)};""")

    parts.append(footer("21-Seed_Organization"))
    write_file("21-Seed_Organization.sql", "\n".join(parts))


def gen_users(wb):
    parts = [header("EFrame 种子 03 - 用户/任岗/人员", 3, "21-Seed_Organization.sql")]
    parts.append(f"DECLARE @Pwd NVARCHAR(200) = N'{PWD_MD5}';")
    parts.append("""
/* ----- 测试账号（密码均为 123456，MD5_16） ----- */
;WITH U AS (
    SELECT * FROM (VALUES""")
    uvals = [f"        ({sql_varchar(l)}, {sql_str(n)}, {sql_varchar(t)})" for l, n, t in SEED_USERS]
    parts.append(",\n".join(uvals))
    parts.append("""    ) v(LoginId, RealName, UserType)
)
INSERT INTO dbo.Tbl_E_Users (LoginId, RealName, PwdHash, PasswordAlgo, PasswordVersion, UserType,
    LoginCount, MaxLoginCount, PwdErrorCount, MaxPwdErrorCount, IsLocked, IsEnabled, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT v.LoginId, v.RealName, @Pwd, 'MD5_16', 1, v.UserType, 0, 99999, 0, 5, 0, 1, '1', 0, @Now, @Now, @Op
FROM U v WHERE NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Users u WHERE u.LoginId=v.LoginId);

UPDATE dbo.Tbl_E_Users SET PwdHash=@Pwd, PasswordAlgo='MD5_16', PasswordVersion=1, PwdErrorCount=0, IsLocked=0, IsEnabled=1, BStatus='1', AmendDate=@Now, Operator=@Op
WHERE LoginId IN (N'cfadmin',N'frameop',N'testuser',N'hrstaff',N'hrmgr') AND IsDeleted=0;""")

    parts.append("\n/* ----- 用户任岗 ----- */")
    for login, dept_code, pos_code in USER_POSITIONS:
        parts.append(f"""
IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId={sql_str(login)} AND d.DeptCode={sql_str(dept_code)} AND p.PostCode={sql_str(pos_code)} AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode={sql_str(dept_code)}
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode={sql_str(pos_code)}
    WHERE u.LoginId={sql_str(login)};""")

    _, members = read_sheet(wb, "Tbl_E_Member")
    if members:
        parts.append("\n/* ----- 人员档案 ----- */")
        for r in members:
            mid = r.get("MemberID", "")
            if not mid:
                continue
            parts.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Member WHERE MemberID={sql_str(mid)})
    INSERT INTO dbo.Tbl_E_Member (MemberID, MemberName, Sex, PerGrade, ELevel, Health, DefaultDeptID, DefaultPosID, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT {sql_str(mid)}, {sql_str(r.get('MemberName'))}, {sql_str(r.get('Sex'))}, {sql_str(r.get('PerGrade'))}, {sql_str(r.get('ELevel'))}, {sql_str(r.get('Health'))},
        (SELECT TOP 1 DataID FROM dbo.Tbl_E_Department WHERE DeptCode=N'CF001'),
        (SELECT TOP 1 DataID FROM dbo.Tbl_E_Position WHERE PostCode=N'CF_STAFF'),
        '1', 0, @Now, @Now, @Op;""")

    _, ms = read_sheet(wb, "Tbl_E_ManagerSubordinate")
    if len(ms) > 0:
        parts.append("\n/* ----- 上下级关系 ----- */")
        for r in ms:
            mu = int(float(r.get("ManagerUserID", 0)))
            su = int(float(r.get("SubUserID", 0)))
            ml = USER_ID_LOGIN.get(mu)
            sl = USER_ID_LOGIN.get(su)
            if not ml or not sl:
                continue
            parts.append(f"""
IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ManagerSubordinate m
    INNER JOIN dbo.Tbl_E_Users u1 ON u1.DataID=m.ManagerUserID
    INNER JOIN dbo.Tbl_E_Users u2 ON u2.DataID=m.SubUserID
    WHERE u1.LoginId={sql_str(ml)} AND u2.LoginId={sql_str(sl)})
    INSERT INTO dbo.Tbl_E_ManagerSubordinate (ManagerUserID, SubUserID, RelationType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u1.DataID, u2.DataID, {sql_varchar(r.get('RelationType','DIRECT'))}, {sql_int(r.get('DispSeq',99))}, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u1 CROSS JOIN dbo.Tbl_E_Users u2 WHERE u1.LoginId={sql_str(ml)} AND u2.LoginId={sql_str(sl)};""")

    parts.append(footer("22-Seed_Users"))
    write_file("22-Seed_Users.sql", "\n".join(parts))


def gen_menus(wb):
    parts = [header("EFrame 种子 04 - 菜单资源/订阅/按钮权限", 4, "22-Seed_Users.sql")]

    _, resources = read_sheet(wb, "Tbl_E_Resource")
    parts.append("/* ----- 菜单资源 ----- */")
    parts.append("""
IF OBJECT_ID('tempdb..#ResSeed') IS NOT NULL DROP TABLE #ResSeed;
CREATE TABLE #ResSeed (ResourceID VARCHAR(50) NOT NULL PRIMARY KEY, ResourceName NVARCHAR(100) NOT NULL, MenuPath NVARCHAR(300) NOT NULL, MenuGroupCode VARCHAR(50) NOT NULL, DispSeq INT NOT NULL, Remark NVARCHAR(200) NULL);
INSERT INTO #ResSeed (ResourceID, ResourceName, MenuPath, MenuGroupCode, DispSeq, Remark) VALUES""")
    rvals = []
    for r in resources:
        rid = r.get("ResourceID", "")
        if not rid:
            continue
        rvals.append(f"({sql_varchar(rid)}, {sql_str(r.get('ResourceName'))}, {sql_str(r.get('MenuPath'))}, {sql_varchar(r.get('MenuGroupCode'))}, {sql_int(r.get('DispSeq',99))}, {sql_str(r.get('Remark'))})")
    parts.append(",\n".join(rvals))
    parts.append("""
;
MERGE dbo.Tbl_E_Resource AS t
USING (SELECT 'FRAME' AS AppCode, s.* FROM #ResSeed s) AS s
ON t.ResourceID = s.ResourceID
WHEN NOT MATCHED THEN
    INSERT (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (s.AppCode, s.ResourceID, s.ResourceName, 'MENU', s.MenuPath, s.MenuGroupCode, s.DispSeq, s.Remark, '1', 0, @Now, @Now, @Op)
WHEN MATCHED THEN
    UPDATE SET ResourceName=s.ResourceName, MenuPath=s.MenuPath, MenuGroupCode=s.MenuGroupCode, DispSeq=s.DispSeq, Remark=s.Remark, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op;
DROP TABLE #ResSeed;""")

    # 补充仪表盘菜单（prg 已实现）
    parts.append("""
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
WHEN MATCHED THEN UPDATE SET ResourceName=s.ResourceName,MenuPath=s.MenuPath,MenuGroupCode=s.MenuGroupCode,DispSeq=s.DispSeq,BStatus='1',IsDeleted=0,AmendDate=@Now,Operator=@Op;""")

    _, subs = read_sheet(wb, "Tbl_E_Subscription")
    parts.append("\n/* ----- 职责订阅 ----- */")
    for r in subs:
        duty_id = int(float(r.get("DutyID", 0)))
        duty_code = DUTY_ID_MAP.get(duty_id)
        if not duty_code:
            continue
        st = r.get("SubType", "RESOURCE")
        ec = r.get("EventCode", "")
        rid = r.get("ResourceID", "")
        fl = r.get("FunctionLimit", "")
        fl_sql = sql_varchar(str(fl).strip()) if str(fl).strip() not in ("", "None") else "NULL"
        ec_sql = sql_varchar(str(ec).strip()) if str(ec).strip() else "NULL"
        rid_sql = sql_varchar(str(rid).strip()) if str(rid).strip() else "NULL"
        parts.append(f"""
IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID
    WHERE d.DutyCode={sql_str(duty_code)} AND s.SubType={sql_varchar(st)}
      AND ISNULL(s.EventCode,'')=ISNULL({ec_sql if ec_sql!='NULL' else "''"},'')
      AND ISNULL(s.ResourceID,'')=ISNULL({rid_sql if rid_sql!='NULL' else "''"},'') AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', {sql_varchar(st)}, {ec_sql}, {rid_sql}, {sql_bit(r.get('IsPrimary',1))}, {fl_sql}, {sql_int(r.get('DispSeq',99))}, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode={sql_str(duty_code)};""")

    # 仪表盘职责 CF_DASH_ADMIN 订阅
    parts.append("""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_DASH_ADMIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'CF_DASH_ADMIN', N'仪表盘配置职责', 'SERVICE', 20, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_PositionDuty pd INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID WHERE p.PostCode=N'CF_ADMIN' AND d.DutyCode=N'CF_DASH_ADMIN' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, N'111111', 8, '1', 0, @Now, @Now, @Op FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d WHERE p.PostCode=N'CF_ADMIN' AND d.DutyCode=N'CF_DASH_ADMIN';""")

    dash_res = ["RES.DASH.Board", "RES.DASH.Indicator", "RES.DASH.PosPerm", "RES.DASH.PosTemplate"]
    for rid in dash_res:
        fl = "'110000'" if rid == "RES.DASH.Board" else "'111111'"
        parts.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID WHERE d.DutyCode=N'CF_ADMIN' AND s.SubType='RESOURCE' AND s.ResourceID={sql_varchar(rid)} AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', {sql_varchar(rid)}, 1, {fl}, 50, '1', 0, @Now, @Now, @Op FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_ADMIN';""")
    parts.append("""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=s.DutyID WHERE d.DutyCode=N'CF_VIEWER' AND s.SubType='RESOURCE' AND s.ResourceID='RES.DASH.Board' AND s.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, 'FRAME', 'RESOURCE', 'RES.DASH.Board', 1, '110000', 50, '1', 0, @Now, @Now, @Op FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'CF_VIEWER';""")

    _, perms = read_sheet(wb, "Tbl_E_ResourcePermission")
    parts.append("\n/* ----- 资源按钮权限 ----- */")
    for r in perms:
        duty_id = int(float(r.get("DutyID", 0)))
        duty_code = DUTY_ID_MAP.get(duty_id)
        rid = r.get("ResourceID", "")
        if not duty_code or not rid:
            continue
        parts.append(f"""
IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode={sql_str(duty_code)} AND rp.ResourceID={sql_varchar(rid)})
    INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT d.DataID, {sql_varchar(rid)}, {sql_bit(r.get('CanCreate'))}, {sql_bit(r.get('CanUpdate'))}, {sql_bit(r.get('CanDelete'))}, {sql_bit(r.get('CanQuery',1))}, {sql_bit(r.get('CanExport'))}, {sql_bit(r.get('CanImport'))}, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode={sql_str(duty_code)};
ELSE
    UPDATE rp SET CanCreate={sql_bit(r.get('CanCreate'))}, CanUpdate={sql_bit(r.get('CanUpdate'))}, CanDelete={sql_bit(r.get('CanDelete'))}, CanQuery={sql_bit(r.get('CanQuery',1))}, AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_ResourcePermission rp INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=rp.DutyID
    WHERE d.DutyCode={sql_str(duty_code)} AND rp.ResourceID={sql_varchar(rid)};""")

    parts.append(footer("23-Seed_Menus"))
    write_file("23-Seed_Menus.sql", "\n".join(parts))


def gen_events(wb):
    parts = [header("EFrame 种子 05 - 事件配置/流转规则", 5, "23-Seed_Menus.sql")]

    _, evts = read_sheet(wb, "Tbl_E_EventConfig")
    parts.append("/* ----- 事件定义 ----- */")
    for r in evts:
        ec = r.get("EventCode", "")
        if not ec:
            continue
        parts.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='FRAME' AND EventCode={sql_varchar(ec)} AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FRAME', {sql_varchar(ec)}, {sql_str(r.get('EventName'))}, {sql_varchar(r.get('EventType','NOTICE'))}, {sql_str(r.get('PageUrl'))}, {sql_varchar(r.get('MenuGroupCode'))}, {sql_varchar(r.get('ExecType','ASYNC'))}, {sql_bit(r.get('IsGenerateTodo'))}, {sql_str(r.get('TodoTitle'))}, {sql_varchar(r.get('HandleMode','SINGLE'))}, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName={sql_str(r.get('EventName'))}, EventType={sql_varchar(r.get('EventType','NOTICE'))}, PageUrl={sql_str(r.get('PageUrl'))}, MenuGroupCode={sql_varchar(r.get('MenuGroupCode'))}, IsGenerateTodo={sql_bit(r.get('IsGenerateTodo'))}, TodoTitle={sql_str(r.get('TodoTitle'))}, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='FRAME' AND EventCode={sql_varchar(ec)};""")

    _, rules = read_sheet(wb, "Tbl_E_EventFlowRule")
    parts.append("\n/* ----- 流转规则（禁用自动链式通过请假，避免与人工审批冲突） ----- */")
    for r in rules:
        rc = r.get("RuleCode", "")
        if not rc or rc == "FLOW_LEAVE_SUBMIT_CHAIN_OK":
            continue
        td = r.get("TargetDutyID", "")
        td_sql = "NULL"
        if td and str(td).strip() not in ("", "0"):
            did = int(float(td))
            dcode = DUTY_ID_MAP.get(did)
            if dcode:
                td_sql = f"(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode={sql_str(dcode)})"
        parts.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode={sql_varchar(rc)} AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ({sql_varchar(rc)}, {sql_str(r.get('RuleName'))}, 'FRAME', {sql_varchar(r.get('CurrentEvent'))}, {sql_varchar(r.get('NextEvent')) if r.get('NextEvent') else 'NULL'}, {sql_str(r.get('ConditionExpr'))}, {sql_varchar(r.get('ActionType'))}, {sql_varchar(r.get('TargetResolveType'))}, {td_sql}, {sql_varchar(r.get('HandleMode','SINGLE'))}, {sql_int(r.get('DispSeq',99))}, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName={sql_str(r.get('RuleName'))}, CurrentEvent={sql_varchar(r.get('CurrentEvent'))}, ActionType={sql_varchar(r.get('ActionType'))}, TargetDutyID={td_sql}, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode={sql_varchar(rc)};""")

    if any(r.get("RuleCode") == "FLOW_LEAVE_SUBMIT_CHAIN_OK" for r in rules):
        parts.append("""
IF EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_LEAVE_SUBMIT_CHAIN_OK' AND IsDeleted=0)
    UPDATE dbo.Tbl_E_EventFlowRule SET IsDeleted=1, AmendDate=@Now, Operator=@Op WHERE RuleCode='FLOW_LEAVE_SUBMIT_CHAIN_OK';""")

    parts.append(footer("24-Seed_Events"))
    write_file("24-Seed_Events.sql", "\n".join(parts))


def gen_dashboard():
    import subprocess
    import sys
    script = os.path.join(os.path.dirname(os.path.abspath(__file__)), "write_dash_seed.py")
    subprocess.run([sys.executable, script], check=True)


def gen_readme():
    content = """/*
================================================================================
  EFrame 框架种子数据 - 执行说明
================================================================================
  前置（空库，二选一建表）：
    A) scripts\\10-EFrame.sql               （SSMS 导出的完整建表）
    B) ..\\docs\\EFrame_CreateTables.sql
       + ..\\docs\\EFrame_v2_supplement.sql
    仪表盘表：scripts\\11-CreateTbl_Dash_All.sql（可选）

  种子（按顺序在 SSMS 中执行，或 SQLCMD 模式执行本目录 20~25）：
    20-Seed_Foundation.sql      应用模块、菜单组、字典
    21-Seed_Organization.sql    部门、岗位、职责、岗位职责
    22-Seed_Users.sql           用户、任岗、人员档案
    23-Seed_Menus.sql           菜单资源、订阅、按钮权限
    24-Seed_Events.sql          事件配置、流转规则
    25-Seed_Dashboard.sql       仪表盘指标/模板（11 条指标，覆盖 ChartType 1~9）

  测试账号（密码均为 123456）：
    cfadmin   超级管理员（全菜单）
    frameop   框架运维
    testuser  普通用户
    hrstaff   人事员工
    hrmgr     人事主管

  说明：
    - 数据参考 EFrame.xls，幂等可重复执行
    - 不含历史事件实例/待办/请假单等业务流水（保持库干净）
    - 执行后请重新登录以刷新侧栏菜单
================================================================================
*/
PRINT N'请按顺序执行 20-Seed_Foundation.sql ~ 25-Seed_Dashboard.sql';
GO
"""
    write_file("20-Seed_README.sql", content)


def main():
    wb = xlrd.open_workbook(XLS, encoding_override="gbk")
    gen_readme()
    gen_foundation(wb)
    gen_organization(wb)
    gen_users(wb)
    gen_menus(wb)
    gen_events(wb)
    gen_dashboard()
    print("Done.")


if __name__ == "__main__":
    main()
