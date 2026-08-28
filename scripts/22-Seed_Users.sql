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

DECLARE @Pwd NVARCHAR(200) = N'49ba59abbe56e057';

/* ----- 测试账号（密码均为 123456，MD5_16） ----- */
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

UPDATE dbo.Tbl_E_Users SET PwdHash=@Pwd, PasswordAlgo='MD5_16', PasswordVersion=1, PwdErrorCount=0, IsLocked=0, IsEnabled=1, BStatus='1', AmendDate=@Now, Operator=@Op
WHERE LoginId IN (N'cfadmin',N'frameop',N'testuser',N'hrstaff',N'hrmgr') AND IsDeleted=0;

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
