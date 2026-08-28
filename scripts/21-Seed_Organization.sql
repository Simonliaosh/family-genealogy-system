/*
==============================================================================
  EFrame 种子 02 - 组织架构（部门/岗位/职责/岗位职责）
==============================================================================
  来源：EFrame.xls + EFrame 架构对齐（幂等 MERGE / IF NOT EXISTS）
  前置：docs/EFrame_CreateTables.sql、docs/EFrame_v2_supplement.sql
  依赖：20-Seed_Foundation.sql
  顺序：第 2 步
  编码：ANSI (GBK)
==============================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-EFRAME';

/* ----- 部门（先主后子） ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode=N'CF001')
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, DeptEName, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'CF001', N'演示部门', NULL, 1, N'/1/', NULL, 1, '1', 0, @Now, @Now, @Op);

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode=N'CF002')
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, DeptEName, ParentDeptID, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT N'CF002', N'研发组', NULL, p.DataID, 2, N'/1/1/', NULL, 2, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Department p WHERE p.DeptCode=N'CF001';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode=N'HR001')
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, DeptEName, ParentDeptID, DeptLevel, DeptPath, DeptType, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT N'HR001', N'人力资源部', NULL, p.DataID, 2, N'/1/1/2/', NULL, 5, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Department p WHERE p.DeptCode=N'CF001';

/* ----- 岗位 ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'CF001')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PostEName, PositionType, DataScope, DispSeq, DDescription, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'CF001', N'演示岗位', NULL, NULL, 'ALL', 1, NULL, NULL, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'演示岗位', DataScope='ALL', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'CF001';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'CF_ADMIN')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PostEName, PositionType, DataScope, DispSeq, DDescription, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'CF_ADMIN', N'框架管理员岗位', NULL, NULL, 'ALL', 1, NULL, NULL, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'框架管理员岗位', DataScope='ALL', DispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'CF_ADMIN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'CF_STAFF')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PostEName, PositionType, DataScope, DispSeq, DDescription, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'CF_STAFF', N'普通员工岗位', NULL, NULL, 'DEPT', 2, NULL, NULL, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'普通员工岗位', DataScope='DEPT', DispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'CF_STAFF';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'HR_MGR')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PostEName, PositionType, DataScope, DispSeq, DDescription, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'HR_MGR', N'人事主管岗位', NULL, NULL, 'DEPT', 10, NULL, NULL, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'人事主管岗位', DataScope='DEPT', DispSeq=10, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'HR_MGR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'HR_EMP')
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PostEName, PositionType, DataScope, DispSeq, DDescription, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'HR_EMP', N'人事员工岗位', NULL, NULL, 'DEPT', 11, NULL, NULL, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Position SET PostCName=N'人事员工岗位', DataScope='DEPT', DispSeq=11, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE PostCode=N'HR_EMP';

/* ----- 职责 ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_ADMIN')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyEName, DutyCategory, DutyDispSeq, DDescription, DutyFlow, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'CF_ADMIN', N'框架管理员职责', NULL, 'ADMIN', 1, NULL, NULL, NULL, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'框架管理员职责', DutyCategory='ADMIN', DutyDispSeq=1, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'CF_ADMIN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_VIEWER')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyEName, DutyCategory, DutyDispSeq, DDescription, DutyFlow, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'CF_VIEWER', N'只读查询职责', NULL, 'SERVICE', 2, NULL, NULL, NULL, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'只读查询职责', DutyCategory='SERVICE', DutyDispSeq=2, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'CF_VIEWER';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_HR_MGR')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyEName, DutyCategory, DutyDispSeq, DDescription, DutyFlow, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'CF_HR_MGR', N'人事审批职责', NULL, 'APPROVAL', 10, NULL, NULL, NULL, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'人事审批职责', DutyCategory='APPROVAL', DutyDispSeq=10, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'CF_HR_MGR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_HR_EMP')
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyEName, DutyCategory, DutyDispSeq, DDescription, DutyFlow, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'CF_HR_EMP', N'人事员工职责', NULL, 'SERVICE', 11, NULL, NULL, NULL, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_Duty SET DutyCName=N'人事员工职责', DutyCategory='SERVICE', DutyDispSeq=11, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE DutyCode=N'CF_HR_EMP';

/* ----- 岗位职责 ----- */

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'CF001' AND d.DutyCode=N'CF_ADMIN' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, DeptID, BusinessLimit, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, NULL, '000000', 1, NULL, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'CF001' AND d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'CF_ADMIN' AND d.DutyCode=N'CF_ADMIN' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, DeptID, BusinessLimit, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, NULL, '111111', 1, NULL, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'CF_ADMIN' AND d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'CF_STAFF' AND d.DutyCode=N'CF_ADMIN' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, DeptID, BusinessLimit, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, NULL, '111111', 2, NULL, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'CF_STAFF' AND d.DutyCode=N'CF_ADMIN';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'CF_ADMIN' AND d.DutyCode=N'CF_HR_MGR' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, DeptID, BusinessLimit, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, NULL, '111111', 5, NULL, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'CF_ADMIN' AND d.DutyCode=N'CF_HR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'CF_STAFF' AND d.DutyCode=N'CF_HR_EMP' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, DeptID, BusinessLimit, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, NULL, '111111', 5, NULL, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'CF_STAFF' AND d.DutyCode=N'CF_HR_EMP';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'HR_MGR' AND d.DutyCode=N'CF_HR_MGR' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, DeptID, BusinessLimit, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, NULL, '111111', 1, NULL, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'HR_MGR' AND d.DutyCode=N'CF_HR_MGR';

IF NOT EXISTS (
    SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=pd.PosID
    INNER JOIN dbo.Tbl_E_Duty d ON d.DataID=pd.DutyID
    WHERE p.PostCode=N'HR_EMP' AND d.DutyCode=N'CF_HR_EMP' AND pd.IsDeleted=0)
    INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, DeptID, BusinessLimit, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT p.DataID, d.DataID, NULL, '111111', 1, NULL, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Position p CROSS JOIN dbo.Tbl_E_Duty d
    WHERE p.PostCode=N'HR_EMP' AND d.DutyCode=N'CF_HR_EMP';

PRINT N'21-Seed_Organization 完成。';
GO
