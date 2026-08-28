-- =============================================
-- ��֧����Ա���룺��Դ + �¼������ظ�ִ�У�
-- ��ִ�� docs/04-���ݽṹ.sql���� FamilyTree_BranchAdminApply��
-- AppCode = FamilyTree
-- =============================================
USE [FamilyTree];
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-FT';
DECLARE @App VARCHAR(50) = 'FamilyTree';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.BranchApply' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.BranchApply', N'�����֧����Ա', 'MENU', N'/FtBranchApply/Index', 'FT_PERSON', 45, '1', 0, @Now, @Now, @Op);

-- ���ܣ�����Դ
INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, r.ResourceID, 1, '111111', r.DispSeq, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
INNER JOIN dbo.Tbl_E_Resource r ON r.ResourceID='RES.FT.BranchApply' AND r.IsDeleted=0
WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID=r.ResourceID AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, r.ResourceID, 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
INNER JOIN dbo.Tbl_E_Resource r ON r.ResourceID='RES.FT.BranchApply' AND r.IsDeleted=0
WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID=r.ResourceID);

-- ��ͨ���ˣ��ɲ鿴�����ύ
INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, r.ResourceID, 1, '110000', 45, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
INNER JOIN dbo.Tbl_E_Resource r ON r.ResourceID='RES.FT.BranchApply' AND r.IsDeleted=0
WHERE d.DutyCode=N'FT_MEMBER' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID=r.ResourceID AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, r.ResourceID, 1, 0, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
INNER JOIN dbo.Tbl_E_Resource r ON r.ResourceID='RES.FT.BranchApply' AND r.IsDeleted=0
WHERE d.DutyCode=N'FT_MEMBER' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID=r.ResourceID);

-- ��֧�ܣ�ֻ���Լ��������¼
INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, r.ResourceID, 1, '100000', 45, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
INNER JOIN dbo.Tbl_E_Resource r ON r.ResourceID='RES.FT.BranchApply' AND r.IsDeleted=0
WHERE d.DutyCode=N'FT_BRANCH_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID=r.ResourceID AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, r.ResourceID, 0, 0, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
INNER JOIN dbo.Tbl_E_Resource r ON r.ResourceID='RES.FT.BranchApply' AND r.IsDeleted=0
WHERE d.DutyCode=N'FT_BRANCH_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID=r.ResourceID);

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.BRANCH.APPLY' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.BRANCH.APPLY', N'�����֧����Ա', 'TASK', N'/FtBranchAdmin/Index', 'FT_ORG', 'ASYNC', 1, N'������֧����Ա����', 'SINGLE', 1440, 60, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.BRANCH.APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.BRANCH.APPROVED', N'��֧����Ա����ͨ��', 'NOTICE', N'/FtBranchApply/Index', 'FT_ORG', 'ASYNC', 0, NULL, 'SINGLE', NULL, 70, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.BRANCH.REJECTED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.BRANCH.REJECTED', N'��֧����Ա���벵��', 'NOTICE', N'/FtBranchApply/Index', 'FT_ORG', 'ASYNC', 0, NULL, 'SINGLE', NULL, 80, '1', 0, @Now, @Now, @Op);

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_BRANCH_TODO' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT 'FLOW_FT_BRANCH_TODO', N'��֧����Ա����-���ܴ���', @App, 'FT.BRANCH.APPLY', NULL, NULL, 'CREATE_TODO', 'DUTY', d.DataID, 'SINGLE', 10, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0;
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_BRANCH_OK_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_FT_BRANCH_OK_NOTICE', N'��֧����Աͨ��֪ͨ������', @App, 'FT.BRANCH.APPROVED', NULL, NULL, 'SEND_NOTICE', 'TRIGGER_USER', NULL, 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_BRANCH_NO_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_FT_BRANCH_NO_NOTICE', N'��֧����Ա����֪ͨ������', @App, 'FT.BRANCH.REJECTED', NULL, NULL, 'SEND_NOTICE', 'TRIGGER_USER', NULL, 'SINGLE', 10, '1', 0, @Now, @Now, @Op);

INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'EVENT', 'FT.BRANCH.APPLY', NULL, 1, NULL, 22, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.EventCode='FT.BRANCH.APPLY' AND s.IsDeleted=0);

PRINT N'FamilyTree branch-admin apply seed done.';
GO
