-- =============================================
-- 族谱管理员岗位 FT_CLAN_ADMIN；支链申请改由族谱管理员审批
-- 可重复执行。依赖 43-Clan.sql、30-Seed。
-- =============================================
USE [FamilyTree];
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-FT-CLAN';
DECLARE @App VARCHAR(50) = 'FamilyTree';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'FT_CLAN_ADMIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_CLAN_ADMIN', N'族谱管理员', N'族谱', 125, '1', 0, @Now, @Now, @Op);

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'FT_CLAN_ADMIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_CLAN_ADMIN', N'族谱管理员', N'ALL', 125, '1', 0, @Now, @Now, @Op);

INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT p.DataID, d.DataID, '111111', 10, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Position p
INNER JOIN dbo.Tbl_E_Duty d ON d.DutyCode = p.PostCode
WHERE p.PostCode=N'FT_CLAN_ADMIN' AND p.IsDeleted=0 AND d.IsDeleted=0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Tbl_E_PositionDuty pd
      WHERE pd.PosID=p.DataID AND pd.DutyID=d.DataID AND pd.IsDeleted=0);

-- 菜单：家族管理（超管委任族谱管理员；族谱管理员看本族）
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.ClanAdmin' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.ClanAdmin', N'我的家族', 'MENU', N'/FtClanAdmin/Index', 'FT_ORG', 5, '1', 0, @Now, @Now, @Op);

-- 超管：家族管理
INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, 'RES.FT.ClanAdmin', 1, '111000', 5, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID='RES.FT.ClanAdmin' AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, 'RES.FT.ClanAdmin', 1, 1, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID='RES.FT.ClanAdmin');

-- 族谱管理员：仅本家族业务菜单（不拷贝分支管全部权限，不含框架岗位职责）
-- 完整白名单见 scripts/45-Scope_FtClanAdmin_Menus.sql
INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, 'RES.FT.ClanAdmin', 1, '101000', 5, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
WHERE d.DutyCode=N'FT_CLAN_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID='RES.FT.ClanAdmin' AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, 'RES.FT.ClanAdmin', 0, 1, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
WHERE d.DutyCode=N'FT_CLAN_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID='RES.FT.ClanAdmin');

-- （已移除：沿用 FT_BRANCH_ADMIN 全量菜单的拷贝逻辑）

-- 支链申请事件：族谱管理员收待办
INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'EVENT', 'FT.BRANCH.APPLY', NULL, 1, NULL, 20, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
WHERE d.DutyCode=N'FT_CLAN_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.EventCode='FT.BRANCH.APPLY' AND s.IsDeleted=0);

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_BRANCH_TODO_CLAN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT 'FLOW_FT_BRANCH_TODO_CLAN', N'支链管理员申请-族谱管理员待办', @App, 'FT.BRANCH.APPLY', NULL, NULL, 'CREATE_TODO', 'DUTY', d.DataID, 'SINGLE', 12, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'FT_CLAN_ADMIN' AND d.IsDeleted=0;

-- 默认家族 Owner 挂上族谱管理员岗（若有）
DECLARE @DefOwner INT = (SELECT TOP 1 OwnerUserId FROM dbo.FamilyTree_Clan WHERE ClanCode=N'DEFAULT' AND IsDeleted=0);
DECLARE @ClanPos INT = (SELECT TOP 1 DataID FROM dbo.Tbl_E_Position WHERE PostCode=N'FT_CLAN_ADMIN' AND IsDeleted=0);
DECLARE @Dept INT = (SELECT TOP 1 DataID FROM dbo.Tbl_E_Department WHERE IsDeleted=0);
IF @DefOwner IS NOT NULL AND @ClanPos IS NOT NULL AND @Dept IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_UserPosition WHERE UserID=@DefOwner AND PosID=@ClanPos)
BEGIN
    INSERT INTO dbo.Tbl_E_UserPosition (UserID, PosID, DeptID, IsPrimary, BStatus, CreateDate, AmendDate, Operator)
    VALUES (@DefOwner, @ClanPos, @Dept, 0, '1', @Now, @Now, @Op);
END

PRINT N'FT_CLAN_ADMIN + RES.FT.ClanAdmin ready.';
GO
