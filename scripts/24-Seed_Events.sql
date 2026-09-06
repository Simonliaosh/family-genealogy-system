/* 库名统一为 FamilyTree：本脚本原先没有 USE，会落在执行工具当时选中的库上。 */
USE [FamilyTree];
GO

/*
==============================================================================
  EFrame 种子 05 - 事件配置/流转规则
==============================================================================
  来源：EFrame.xls + EFrame 架构对齐（幂等 MERGE / IF NOT EXISTS）
  前置：docs/EFrame_CreateTables.sql、docs/EFrame_v2_supplement.sql
  依赖：23-Seed_Menus.sql
  顺序：第 5 步
  编码：ANSI (GBK)
==============================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-EFRAME';

/* ----- 事件定义 ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='FRAME' AND EventCode='FRAME.DEMO.SUBMIT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FRAME', 'FRAME.DEMO.SUBMIT', N'演示提交', 'NOTICE', NULL, NULL, 'ASYNC', 1, N'演示待办', 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'演示提交', EventType='NOTICE', PageUrl=NULL, MenuGroupCode=NULL, IsGenerateTodo=1, TodoTitle=N'演示待办', BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='FRAME' AND EventCode='FRAME.DEMO.SUBMIT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='FRAME' AND EventCode='FRAME.DEMO.APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FRAME', 'FRAME.DEMO.APPROVED', N'演示审批通过', 'NOTICE', NULL, NULL, 'ASYNC', 0, NULL, 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'演示审批通过', EventType='NOTICE', PageUrl=NULL, MenuGroupCode=NULL, IsGenerateTodo=0, TodoTitle=NULL, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='FRAME' AND EventCode='FRAME.DEMO.APPROVED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='FRAME' AND EventCode='FRAME.LEAVE.SUBMIT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FRAME', 'FRAME.LEAVE.SUBMIT', N'请假提交', 'TASK', N'/HrLeave/Approval', 'HR', 'ASYNC', 1, N'【请假待审】', 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'请假提交', EventType='TASK', PageUrl=N'/HrLeave/Approval', MenuGroupCode='HR', IsGenerateTodo=1, TodoTitle=N'【请假待审】', BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='FRAME' AND EventCode='FRAME.LEAVE.SUBMIT';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='FRAME' AND EventCode='FRAME.LEAVE.APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FRAME', 'FRAME.LEAVE.APPROVED', N'请假已通过', 'NOTICE', NULL, 'HR', 'ASYNC', 0, NULL, 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'请假已通过', EventType='NOTICE', PageUrl=NULL, MenuGroupCode='HR', IsGenerateTodo=0, TodoTitle=NULL, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='FRAME' AND EventCode='FRAME.LEAVE.APPROVED';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode='FRAME' AND EventCode='FRAME.LEAVE.REJECTED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FRAME', 'FRAME.LEAVE.REJECTED', N'请假已驳回', 'NOTICE', NULL, 'HR', 'ASYNC', 0, NULL, 'SINGLE', '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventConfig SET EventName=N'请假已驳回', EventType='NOTICE', PageUrl=NULL, MenuGroupCode='HR', IsGenerateTodo=0, TodoTitle=NULL, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE AppCode='FRAME' AND EventCode='FRAME.LEAVE.REJECTED';

/* ----- 流转规则（禁用自动链式通过请假，避免与人工审批冲突） ----- */

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_DEMO_SUBMIT_DUTY' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_DEMO_SUBMIT_DUTY', N'演示提交-职责待办', 'FRAME', 'FRAME.DEMO.SUBMIT', NULL, NULL, 'CREATE_TODO', 'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_ADMIN'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'演示提交-职责待办', CurrentEvent='FRAME.DEMO.SUBMIT', ActionType='CREATE_TODO', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_ADMIN'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode='FLOW_DEMO_SUBMIT_DUTY';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_DEMO_SUBMIT_CHAIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_DEMO_SUBMIT_CHAIN', N'演示提交-链式审批', 'FRAME', 'FRAME.DEMO.SUBMIT', 'FRAME.DEMO.APPROVED', NULL, 'TRIGGER_EVENT', NULL, NULL, 'SINGLE', 5, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'演示提交-链式审批', CurrentEvent='FRAME.DEMO.SUBMIT', ActionType='TRIGGER_EVENT', TargetDutyID=NULL, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode='FLOW_DEMO_SUBMIT_CHAIN';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_DEMO_SUBMIT_API' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_DEMO_SUBMIT_API', N'演示提交-回调接口', 'FRAME', 'FRAME.DEMO.SUBMIT', NULL, NULL, 'CALL_API', NULL, NULL, 'SINGLE', 8, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'演示提交-回调接口', CurrentEvent='FRAME.DEMO.SUBMIT', ActionType='CALL_API', TargetDutyID=NULL, BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode='FLOW_DEMO_SUBMIT_API';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_DEMO_APPROVED_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_DEMO_APPROVED_NOTICE', N'演示审批-通知职责', 'FRAME', 'FRAME.DEMO.APPROVED', NULL, NULL, 'SEND_NOTICE', 'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_ADMIN'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'演示审批-通知职责', CurrentEvent='FRAME.DEMO.APPROVED', ActionType='SEND_NOTICE', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_ADMIN'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode='FLOW_DEMO_APPROVED_NOTICE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_LEAVE_SUBMIT_MGR' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_LEAVE_SUBMIT_MGR', N'请假提交-主管待办', 'FRAME', 'FRAME.LEAVE.SUBMIT', NULL, NULL, 'CREATE_TODO', 'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_HR_MGR'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'请假提交-主管待办', CurrentEvent='FRAME.LEAVE.SUBMIT', ActionType='CREATE_TODO', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_HR_MGR'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode='FLOW_LEAVE_SUBMIT_MGR';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_LEAVE_SUBMIT_TODO' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_LEAVE_SUBMIT_TODO', N'请假提交-生成主管待办', 'FRAME', 'FRAME.LEAVE.SUBMIT', NULL, NULL, 'CREATE_TODO', 'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_HR_MGR'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'请假提交-生成主管待办', CurrentEvent='FRAME.LEAVE.SUBMIT', ActionType='CREATE_TODO', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_HR_MGR'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode='FLOW_LEAVE_SUBMIT_TODO';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_LEAVE_APPROVED_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_LEAVE_APPROVED_NOTICE', N'请假通过-通知员工职责', 'FRAME', 'FRAME.LEAVE.APPROVED', NULL, NULL, 'SEND_NOTICE', 'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_HR_EMP'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'请假通过-通知员工职责', CurrentEvent='FRAME.LEAVE.APPROVED', ActionType='SEND_NOTICE', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_HR_EMP'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode='FLOW_LEAVE_APPROVED_NOTICE';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_LEAVE_REJECTED_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_LEAVE_REJECTED_NOTICE', N'请假驳回-通知员工职责', 'FRAME', 'FRAME.LEAVE.REJECTED', NULL, NULL, 'SEND_NOTICE', 'DUTY', (SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_HR_EMP'), 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
ELSE
    UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=N'请假驳回-通知员工职责', CurrentEvent='FRAME.LEAVE.REJECTED', ActionType='SEND_NOTICE', TargetDutyID=(SELECT DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'CF_HR_EMP'), BStatus='1', IsDeleted=0, AmendDate=@Now, Operator=@Op
    WHERE RuleCode='FLOW_LEAVE_REJECTED_NOTICE';

IF EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_LEAVE_SUBMIT_CHAIN_OK' AND IsDeleted=0)
    UPDATE dbo.Tbl_E_EventFlowRule SET IsDeleted=1, AmendDate=@Now, Operator=@Op WHERE RuleCode='FLOW_LEAVE_SUBMIT_CHAIN_OK';

PRINT N'24-Seed_Events 完成。';
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'24-Seed_Events.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'24-Seed_Events.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'24-Seed_Events.sql');
GO
