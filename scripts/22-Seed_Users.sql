/* 库名统一为 FamilyTree：本脚本原先没有 USE，会落在执行工具当时选中的库上。 */
USE [FamilyTree];
GO

/*
==============================================================================
  EFrame 种子 03 - 用户/任岗/人员
==============================================================================
  来源：EFrame.xls + EFrame 架构对齐（幂等 MERGE / IF NOT EXISTS）
  前置：docs/EFrame_CreateTables.sql、docs/EFrame_v2_supplement.sql
  依赖：21-Seed_Organization.sql
  顺序：第 3 步
  编码：ANSI (GBK)
==============================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-EFRAME';

/* 初始口令不写在脚本里。用 sqlcmd 变量传入明文：
       sqlcmd -S <server> -d <db> -v AdminPassword="你的强口令" -i 22-Seed_Users.sql
   （SSMS 请先在「查询」菜单里打开 SQLCMD 模式。）
   未传入该变量时 sqlcmd 会直接报「scripting variable not defined」并终止，不会静默种出弱口令。
   存的是 MD5_16 兼容格式，首次登录时 PasswordHasher 会透明升级为 PBKDF2。 */
DECLARE @AdminPwdPlain VARCHAR(200) = '$(AdminPassword)';
/* 非 SQLCMD 模式（普通 SSMS 查询窗口）下 $(AdminPassword) 不会被替换，
   会原样留下字面量——必须一并拦掉，否则会静默把这串字面量当口令种进去。 */
IF LTRIM(RTRIM(@AdminPwdPlain)) = '' OR @AdminPwdPlain = '$' + '(AdminPassword)'
BEGIN
    RAISERROR(N'请用 -v AdminPassword="..." 传入初始管理员口令后再执行本脚本。', 16, 1);
    SET NOEXEC ON;
END
DECLARE @Pwd NVARCHAR(200) =
    LOWER(SUBSTRING(sys.fn_VarBinToHexStr(HASHBYTES('MD5', @AdminPwdPlain)), 11, 16));

/* ----- 初始账号（口令取自 -v AdminPassword，仅首次插入时设置） ----- */
;WITH U AS (
    SELECT * FROM (VALUES
        ('cfadmin', N'超级管理员', 'EMPLOYEE'),
        ('frameop', N'框架运维', 'EMPLOYEE'),
        ('testuser', N'测试用户', 'EMPLOYEE'),
        ('hrstaff', N'人事员工', 'EMPLOYEE'),
        ('hrmgr', N'人事主管', 'EMPLOYEE')
    ) v(LoginId, RealName, UserType)
)
INSERT INTO dbo.Tbl_E_Users (LoginId, RealName, PwdHash, PasswordAlgo, PasswordVersion, UserType,
    LoginCount, MaxLoginCount, PwdErrorCount, MaxPwdErrorCount, IsLocked, IsEnabled, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT v.LoginId, v.RealName, @Pwd, 'MD5_16', 1, v.UserType, 0, 99999, 0, 5, 0, 1, '1', 0, @Now, @Now, @Op
FROM U v WHERE NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Users u WHERE u.LoginId=v.LoginId);

/* 口令只在上面的 INSERT ... WHERE NOT EXISTS 分支里设置。
   原先这里是一条无条件 UPDATE：每次重跑都会把这 5 个账号（含超管 cfadmin）的口令重置为固定值、
   错误计数清零、解锁并重新启用——等于把已封禁的超管救活，且该脚本可从 Web 后台重跑。已删除。
   需要重置某个账号，请走后台「重置密码」，或另写一条带明确 WHERE 的运维脚本。 */

/* ----- 用户任岗 ----- */

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'cfadmin' AND d.DeptCode=N'CF001' AND p.PostCode=N'CF001' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'CF001'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'CF001'
    WHERE u.LoginId=N'cfadmin';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'cfadmin' AND d.DeptCode=N'CF001' AND p.PostCode=N'CF_ADMIN' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'CF001'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'CF_ADMIN'
    WHERE u.LoginId=N'cfadmin';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'frameop' AND d.DeptCode=N'CF001' AND p.PostCode=N'CF_ADMIN' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'CF001'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'CF_ADMIN'
    WHERE u.LoginId=N'frameop';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'testuser' AND d.DeptCode=N'CF001' AND p.PostCode=N'CF_STAFF' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'CF001'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'CF_STAFF'
    WHERE u.LoginId=N'testuser';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'hrstaff' AND d.DeptCode=N'HR001' AND p.PostCode=N'HR_EMP' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'HR001'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'HR_EMP'
    WHERE u.LoginId=N'hrstaff';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Users u ON u.DataID=up.UserID
    INNER JOIN dbo.Tbl_E_Department d ON d.DataID=up.DeptID
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE u.LoginId=N'hrmgr' AND d.DeptCode=N'HR001' AND p.PostCode=N'HR_MGR' AND up.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u.DataID, d.DataID, p.DataID, 0, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u
    INNER JOIN dbo.Tbl_E_Department d ON d.DeptCode=N'HR001'
    INNER JOIN dbo.Tbl_E_Position p ON p.PostCode=N'HR_MGR'
    WHERE u.LoginId=N'hrmgr';

/* ----- 人员档案 ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Member WHERE MemberID=N'M2026001')
    INSERT INTO dbo.Tbl_E_Member (MemberID, MemberName, Sex, PerGrade, ELevel, Health, DefaultDeptID, DefaultPosID, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT N'M2026001', N'张三', N'男', N'主办', N'本科', N'健康',
        (SELECT TOP 1 DataID FROM dbo.Tbl_E_Department WHERE DeptCode=N'CF001'),
        (SELECT TOP 1 DataID FROM dbo.Tbl_E_Position WHERE PostCode=N'CF_STAFF'),
        '1', 0, @Now, @Now, @Op;

/* ----- 上下级关系 ----- */

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_ManagerSubordinate m
    INNER JOIN dbo.Tbl_E_Users u1 ON u1.DataID=m.ManagerUserID
    INNER JOIN dbo.Tbl_E_Users u2 ON u2.DataID=m.SubUserID
    WHERE u1.LoginId=N'cfadmin' AND u2.LoginId=N'frameop')
    INSERT INTO dbo.Tbl_E_ManagerSubordinate (ManagerUserID, SubUserID, RelationType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT u1.DataID, u2.DataID, 'DIRECT', 99, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Users u1 CROSS JOIN dbo.Tbl_E_Users u2 WHERE u1.LoginId=N'cfadmin' AND u2.LoginId=N'frameop';

PRINT N'22-Seed_Users 完成。';
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'22-Seed_Users.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'22-Seed_Users.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'22-Seed_Users.sql');
GO
