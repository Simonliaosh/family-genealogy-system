-- =============================================
-- ???????? ?? ????/???/????????????
-- ????? docs/04-?????.sql
-- AppCode = FamilyTree
-- =============================================
USE [FamilyTree];
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-FT';
DECLARE @App VARCHAR(50) = 'FamilyTree';

-- ???
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'FT_MEMBER' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_MEMBER', N'???????', N'????', 110, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'FT_BRANCH_ADMIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_BRANCH_ADMIN', N'??????????', N'????', 120, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'FT_SUPER_ADMIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_SUPER_ADMIN', N'???????', N'????', 130, '1', 0, @Now, @Now, @Op);

-- ????
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'FT_MEMBER' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_MEMBER', N'???????', N'SELF', 110, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'FT_BRANCH_ADMIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_BRANCH_ADMIN', N'????????', N'ALL', 120, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'FT_SUPER_ADMIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_SUPER_ADMIN', N'???????', N'ALL', 130, '1', 0, @Now, @Now, @Op);

-- ????
INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT p.DataID, d.DataID, '111111', 10, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Position p
INNER JOIN dbo.Tbl_E_Duty d ON d.DutyCode = p.PostCode
WHERE p.PostCode IN (N'FT_MEMBER', N'FT_BRANCH_ADMIN', N'FT_SUPER_ADMIN')
  AND p.IsDeleted=0 AND d.IsDeleted=0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
      WHERE pd.PosID=p.DataID AND pd.DutyID=d.DataID AND pd.IsDeleted=0);

-- ?????
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='FT_ORG' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FT_ORG', @App, N'???????', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='FT_PERSON' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FT_PERSON', @App, N'??????', 20, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='FT_AUDIT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FT_AUDIT', @App, N'?????', 30, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='FT_LINK' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FT_LINK', @App, N'???????', 40, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='FT_TREE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FT_TREE', @App, N'???????', 50, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='FT_OPS' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FT_OPS', @App, N'???????', 60, '1', 0, @Now, @Now, @Op);

-- ???
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.BranchAdmin' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.BranchAdmin', N'????????', 'MENU', N'/FtBranchAdmin/Index', 'FT_ORG', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.Person' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.Person', N'??????', 'MENU', N'/FtPerson/Index', 'FT_PERSON', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.PersonCreate' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.PersonCreate', N'???????', 'MENU', N'/FtPerson/Create', 'FT_PERSON', 20, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.PersonMarry' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.PersonMarry', N'???????', 'MENU', N'/FtPersonMarry/Index', 'FT_PERSON', 30, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.MyProfile' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.MyProfile', N'??????', 'MENU', N'/FtMyProfile/Index', 'FT_PERSON', 40, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.MainTree' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.MainTree', N'????????', 'MENU', N'/FtMainTree/Index', 'FT_PERSON', 50, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.GenerationWord' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.GenerationWord', N'??????', 'MENU', N'/FtGenerationWord/Index', 'FT_PERSON', 60, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.PersonDraft' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.PersonDraft', N'???????', 'MENU', N'/FtPersonDraft/Index', 'FT_AUDIT', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.PersonLink' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.PersonLink', N'????????', 'MENU', N'/FtPersonLink/Index', 'FT_LINK', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.LinkAudit' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.LinkAudit', N'????????', 'MENU', N'/FtLinkAudit/Index', 'FT_LINK', 20, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.Conflict' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.Conflict', N'?????', 'MENU', N'/FtConflict/Index', 'FT_LINK', 30, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.TreeView' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.TreeView', N'??????', 'MENU', N'/FtTree/Index', 'FT_TREE', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.Export' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.Export', N'??????', 'MENU', N'/FtExport/Index', 'FT_TREE', 20, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.OpLog' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.OpLog', N'???????', 'MENU', N'/FtOpLog/Index', 'FT_OPS', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.BatchMatch' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.BatchMatch', N'???????', 'MENU', N'/FtBatchMatch/Index', 'FT_OPS', 20, '1', 0, @Now, @Now, @Op);

-- ?????????????????
INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, r.ResourceID, 1, '111111', r.DispSeq, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
CROSS JOIN dbo.Tbl_E_Resource r
WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0 AND r.IsDeleted=0 AND r.AppCode=@App
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID=r.ResourceID AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, r.ResourceID, 1, 1, 1, 1, 1, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
CROSS JOIN dbo.Tbl_E_Resource r
WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0 AND r.IsDeleted=0 AND r.AppCode=@App
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID=r.ResourceID);

-- ????
DECLARE @MemRes TABLE (Rid VARCHAR(100), Lim VARCHAR(6), C BIT, U BIT, D BIT, Q BIT);
INSERT INTO @MemRes VALUES
('RES.FT.PersonDraft','111100',1,1,1,1),
('RES.FT.MyProfile','101000',0,1,0,1),
('RES.FT.TreeView','100000',0,0,0,1),
('RES.FT.Person','101000',0,1,0,1),
('RES.FT.PersonLink','110000',1,0,0,1),
('RES.FT.GenerationWord','100000',0,0,0,1),
('RES.FT.PersonMarry','101000',0,1,0,1);

INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, m.Rid, 1, m.Lim, 10, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d CROSS JOIN @MemRes m
WHERE d.DutyCode=N'FT_MEMBER' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID=m.Rid AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, m.Rid, m.C, m.U, m.D, m.Q, 0, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d CROSS JOIN @MemRes m
WHERE d.DutyCode=N'FT_MEMBER' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID=m.Rid);

-- ????? = ???? + ????/??????
DECLARE @BaRes TABLE (Rid VARCHAR(100), Lim VARCHAR(6), C BIT, U BIT, D BIT, Q BIT);
INSERT INTO @BaRes VALUES
('RES.FT.PersonDraft','100000',0,0,0,1),
('RES.FT.MyProfile','101000',0,1,0,1),
('RES.FT.TreeView','100000',0,0,0,1),
('RES.FT.Person','111000',1,1,0,1),
('RES.FT.PersonCreate','110000',1,0,0,1),
('RES.FT.PersonLink','110000',1,0,0,1),
('RES.FT.LinkAudit','101000',0,1,0,1),
('RES.FT.GenerationWord','100000',0,0,0,1),
('RES.FT.PersonMarry','101000',0,1,0,1),
('RES.FT.Conflict','100000',0,0,0,1),
('RES.FT.Export','100000',0,0,0,1);

INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, m.Rid, 1, m.Lim, 10, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d CROSS JOIN @BaRes m
WHERE d.DutyCode=N'FT_BRANCH_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID=m.Rid AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, m.Rid, m.C, m.U, m.D, m.Q, 0, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d CROSS JOIN @BaRes m
WHERE d.DutyCode=N'FT_BRANCH_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID=m.Rid);

-- ???
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.LINK.APPLY' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.LINK.APPLY', N'????????????', 'TASK', N'/FtLinkAudit/Index', 'FT_LINK', 'ASYNC', 1, N'?????????', 'SINGLE', 1440, 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.LINK.APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.LINK.APPROVED', N'???????', 'NOTICE', N'/FtPersonLink/Index', 'FT_LINK', 'ASYNC', 0, NULL, 'SINGLE', NULL, 20, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.LINK.REJECTED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.LINK.REJECTED', N'??????', 'NOTICE', N'/FtPersonLink/Index', 'FT_LINK', 'ASYNC', 0, NULL, 'SINGLE', NULL, 30, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.LINK.UNLINKED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.LINK.UNLINKED', N'?????', 'NOTICE', N'/FtPersonLink/Index', 'FT_LINK', 'ASYNC', 0, NULL, 'SINGLE', NULL, 40, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.CONFLICT.OPEN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.CONFLICT.OPEN', N'??????????', 'TASK', N'/FtConflict/Index', 'FT_LINK', 'ASYNC', 1, N'??????????', 'SINGLE', 1440, 50, '1', 0, @Now, @Now, @Op);

-- ???
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_LINK_TODO' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT 'FLOW_FT_LINK_TODO', N'????????-???????', @App, 'FT.LINK.APPLY', NULL, NULL, 'CREATE_TODO', 'DUTY', d.DataID, 'SINGLE', 10, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0;
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_LINK_TODO_BA' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT 'FLOW_FT_LINK_TODO_BA', N'链入申请-分支管待办', @App, 'FT.LINK.APPLY', NULL, NULL, 'CREATE_TODO', 'DUTY', d.DataID, 'SINGLE', 11, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'FT_BRANCH_ADMIN' AND d.IsDeleted=0;
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_LINK_OK_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_FT_LINK_OK_NOTICE', N'???????????????', @App, 'FT.LINK.APPROVED', NULL, NULL, 'SEND_NOTICE', 'TRIGGER_USER', NULL, 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_LINK_NO_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_FT_LINK_NO_NOTICE', N'??????????????', @App, 'FT.LINK.REJECTED', NULL, NULL, 'SEND_NOTICE', 'TRIGGER_USER', NULL, 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_UNLINK_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_FT_UNLINK_NOTICE', N'????????????', @App, 'FT.LINK.UNLINKED', NULL, NULL, 'SEND_NOTICE', 'TRIGGER_USER', NULL, 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_CONFLICT_TODO' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT 'FLOW_FT_CONFLICT_TODO', N'???-???????', @App, 'FT.CONFLICT.OPEN', NULL, NULL, 'CREATE_TODO', 'DUTY', d.DataID, 'SINGLE', 10, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0;

INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'EVENT', 'FT.LINK.APPLY', NULL, 1, NULL, 20, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.EventCode='FT.LINK.APPLY' AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'EVENT', 'FT.LINK.APPLY', NULL, 1, NULL, 20, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'FT_BRANCH_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.EventCode='FT.LINK.APPLY' AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'EVENT', 'FT.CONFLICT.OPEN', NULL, 1, NULL, 21, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.EventCode='FT.CONFLICT.OPEN' AND s.IsDeleted=0);

-- ?????? cfadmin ??????????????
DECLARE @Uid INT, @DeptId INT, @PosId INT;
SELECT TOP 1 @Uid = DataID FROM dbo.Tbl_E_Users WHERE LoginId=N'cfadmin' AND IsDeleted=0;
SELECT TOP 1 @DeptId = DataID FROM dbo.Tbl_E_Department WHERE IsDeleted=0 ORDER BY DataID;
SELECT TOP 1 @PosId = DataID FROM dbo.Tbl_E_Position WHERE PostCode=N'FT_SUPER_ADMIN' AND IsDeleted=0;
IF @Uid IS NOT NULL AND @DeptId IS NOT NULL AND @PosId IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_UserPosition WHERE UserID=@Uid AND PosID=@PosId AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@Uid, @DeptId, @PosId, 0, '1', 0, @Now, @Now, @Op);

PRINT N'FamilyTree seed done.';
GO
