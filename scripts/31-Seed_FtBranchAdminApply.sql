-- =============================================
-- 支链管理员申请：资源 + 事件（可重复执行）
-- 依赖：scripts/29-CreateTbl_FamilyTree_Core.sql（含 FamilyTree_BranchAdminApply 表）
-- AppCode = FamilyTree
-- =============================================
USE [FamilyTree];
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-FT';
DECLARE @App VARCHAR(50) = 'FamilyTree';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.BranchApply' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.BranchApply', N'申请支链管理员', 'MENU', N'/FtBranchApply/Index', 'FT_PERSON', 45, '1', 0, @Now, @Now, @Op);

-- 超管：全功能资源权限
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

-- 普通族人：可查看、可提交申请
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

-- 支链管理员：只看与自己相关的记录
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
    VALUES (@App, 'FT.BRANCH.APPLY', N'申请支链管理员', 'TASK', N'/FtBranchAdmin/Index', 'FT_ORG', 'ASYNC', 1, N'申请成为支链管理员', 'SINGLE', 1440, 60, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.BRANCH.APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.BRANCH.APPROVED', N'支链管理员申请已通过', 'NOTICE', N'/FtBranchApply/Index', 'FT_ORG', 'ASYNC', 0, NULL, 'SINGLE', NULL, 70, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.BRANCH.REJECTED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.BRANCH.REJECTED', N'支链管理员申请被驳回', 'NOTICE', N'/FtBranchApply/Index', 'FT_ORG', 'ASYNC', 0, NULL, 'SINGLE', NULL, 80, '1', 0, @Now, @Now, @Op);

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_BRANCH_TODO' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT 'FLOW_FT_BRANCH_TODO', N'支链管理员申请-超管待办', @App, 'FT.BRANCH.APPLY', NULL, NULL, 'CREATE_TODO', 'DUTY', d.DataID, 'SINGLE', 10, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0;
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_BRANCH_OK_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_FT_BRANCH_OK_NOTICE', N'支链管理员通过通知申请人', @App, 'FT.BRANCH.APPROVED', NULL, NULL, 'SEND_NOTICE', 'TRIGGER_USER', NULL, 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_BRANCH_NO_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_FT_BRANCH_NO_NOTICE', N'支链管理员驳回通知申请人', @App, 'FT.BRANCH.REJECTED', NULL, NULL, 'SEND_NOTICE', 'TRIGGER_USER', NULL, 'SINGLE', 10, '1', 0, @Now, @Now, @Op);

INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'EVENT', 'FT.BRANCH.APPLY', NULL, 1, NULL, 22, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.EventCode='FT.BRANCH.APPLY' AND s.IsDeleted=0);

PRINT N'FamilyTree branch-admin apply seed done.';
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'31-Seed_FtBranchAdminApply.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'31-Seed_FtBranchAdminApply.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'31-Seed_FtBranchAdminApply.sql');
GO
