-- =============================================
-- 家族族谱 — 完整岗位职责/菜单/事件种子（v2.0）
-- 前置：EFrame 基础表 + docs/04-数据结构.sql
-- 种子用户：仅 cfadmin + FT_SUPER_ADMIN 岗（口令由 sqlcmd -v AdminPassword 传入）
-- 可重复执行
-- =============================================
USE [FamilyTree];
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-FT-FULL';
DECLARE @App VARCHAR(50) = 'FamilyTree';
/* 初始口令不写在脚本里，用 sqlcmd 变量传入：
       sqlcmd -S <server> -d <db> -v AdminPassword="你的强口令" -i 50-Seed_FamilyTree_Full.sql
   （SSMS 需先打开 SQLCMD 模式。）存 MD5_16 兼容格式，首次登录透明升级为 PBKDF2。 */
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

/* ========== 0. 最小组织与用户（仅超管） ========== */
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode=N'FT_ROOT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, DeptLevel, DeptPath, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_ROOT', N'族谱平台', 1, N'/1/', 1, '1', 0, @Now, @Now, @Op);

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Users WHERE LoginId=N'cfadmin' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Users (LoginId, RealName, PwdHash, PasswordAlgo, PasswordVersion, UserType,
        LoginCount, MaxLoginCount, PwdErrorCount, MaxPwdErrorCount, IsLocked, IsEnabled, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'cfadmin', N'超级管理员', @Pwd, 'MD5_16', 1, 'EMPLOYEE', 0, 99999, 0, 5, 0, 1, '1', 0, @Now, @Now, @Op);
/* 原先这里是 ELSE UPDATE：重跑即把已存在的超管口令重置、解锁、重新启用。已删除，口令只在首次插入时设置。 */

/* ========== 1. 职责 Duty ========== */
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'FT_MEMBER' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_MEMBER', N'普通族人', N'族谱', 110, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'FT_BRANCH_ADMIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_BRANCH_ADMIN', N'支链管理员', N'族谱', 120, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'FT_CLAN_ADMIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_CLAN_ADMIN', N'族谱管理员', N'族谱', 125, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=N'FT_SUPER_ADMIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyCategory, DutyDispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_SUPER_ADMIN', N'超级管理员', N'族谱', 130, '1', 0, @Now, @Now, @Op);

/* ========== 2. 岗位 Position ========== */
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'FT_MEMBER' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_MEMBER', N'普通族人', N'SELF', 110, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'FT_BRANCH_ADMIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_BRANCH_ADMIN', N'支链管理员', N'ALL', 120, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'FT_CLAN_ADMIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_CLAN_ADMIN', N'族谱管理员', N'ALL', 125, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=N'FT_SUPER_ADMIN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, DataScope, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (N'FT_SUPER_ADMIN', N'超级管理员', N'ALL', 130, '1', 0, @Now, @Now, @Op);

INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, BusinessLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT p.DataID, d.DataID, '111111', 10, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Position p
INNER JOIN dbo.Tbl_E_Duty d ON d.DutyCode = p.PostCode
WHERE p.PostCode IN (N'FT_MEMBER', N'FT_BRANCH_ADMIN', N'FT_CLAN_ADMIN', N'FT_SUPER_ADMIN')
  AND p.IsDeleted=0 AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_PositionDuty pd WHERE pd.PosID=p.DataID AND pd.DutyID=d.DataID AND pd.IsDeleted=0);

/* ========== 3. 菜单组 ========== */
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='FT_ORG' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FT_ORG', @App, N'组织管理', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='FT_PERSON' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FT_PERSON', @App, N'人物档案', 20, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='FT_AUDIT' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FT_AUDIT', @App, N'我的填报', 30, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='FT_LINK' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FT_LINK', @App, N'匹配链入', 40, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='FT_TREE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FT_TREE', @App, N'族谱视图', 50, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='FT_OPS' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FT_OPS', @App, N'运维', 60, '1', 0, @Now, @Now, @Op);

/* ========== 4. 资源 Resource ========== */
;WITH R(ResourceID, ResourceName, MenuPath, MenuGroupCode, DispSeq) AS (
    SELECT * FROM (VALUES
        (N'RES.FT.ClanAdmin',     N'家族与族谱管理员', N'/FtClanAdmin/Index',     N'FT_ORG',    5),
        (N'RES.FT.BranchAdmin',   N'支链管理员任免',   N'/FtBranchAdmin/Index',   N'FT_ORG',   10),
        (N'RES.FT.BranchApply',   N'申请支链管理员',   N'/FtBranchApply/Index',   N'FT_PERSON',45),
        (N'RES.FT.Person',        N'人物档案',         N'/FtPerson/Index',        N'FT_PERSON',10),
        (N'RES.FT.PersonCreate',  N'录入人物',         N'/FtPerson/Create',       N'FT_PERSON',20),
        (N'RES.FT.PersonMarry',   N'配偶信息',         N'/FtPersonMarry/Index',   N'FT_PERSON',30),
        (N'RES.FT.MyProfile',     N'我的档案',         N'/FtMyProfile/Index',     N'FT_PERSON',40),
        (N'RES.FT.MainTree',      N'纳入主谱',         N'/FtMainTree/Index',      N'FT_PERSON',50),
        (N'RES.FT.GenerationWord',N'字辈词条',         N'/FtGenerationWord/Index',N'FT_PERSON',60),
        (N'RES.FT.PersonDraft',   N'我的填报',         N'/FtPersonDraft/Index',   N'FT_AUDIT', 10),
        (N'RES.FT.PersonLink',    N'匹配链入',         N'/FtPersonLink/Index',    N'FT_LINK',  10),
        (N'RES.FT.LinkAudit',     N'链入审批',         N'/FtLinkAudit/Index',     N'FT_LINK',  20),
        (N'RES.FT.Conflict',      N'匹配冲突',         N'/FtConflict/Index',      N'FT_LINK',  30),
        (N'RES.FT.Peer',          N'外链对接',         N'/FtPeer/Index',          N'FT_LINK',  80),
        (N'RES.FT.TreeView',      N'族谱树',           N'/FtTree/Index',          N'FT_TREE',  10),
        (N'RES.FT.Export',        N'导出印刷',         N'/FtExport/Index',        N'FT_TREE',  20),
        (N'RES.FT.OpLog',         N'操作日志',         N'/FtOpLog/Index',         N'FT_OPS',   10),
        (N'RES.FT.BatchMatch',    N'批量匹配',         N'/FtBatchMatch/Index',    N'FT_OPS',   20),
        (N'RES.FT.TreeHealth',    N'谱系体检',         N'/FtTreeHealth/Index',    N'FT_OPS',   30)
    ) v(ResourceID, ResourceName, MenuPath, MenuGroupCode, DispSeq)
)
INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT @App, r.ResourceID, r.ResourceName, 'MENU', r.MenuPath, r.MenuGroupCode, r.DispSeq, '1', 0, @Now, @Now, @Op
FROM R r
WHERE NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource x WHERE x.ResourceID=r.ResourceID AND x.IsDeleted=0);

/* ========== 5. 超管：仅 ClanAdmin（先删后保） ========== */
DECLARE @SuperDuty INT = (SELECT TOP 1 DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'FT_SUPER_ADMIN' AND IsDeleted=0);

IF @SuperDuty IS NOT NULL
BEGIN
    UPDATE dbo.Tbl_E_Subscription SET IsDeleted=1, AmendDate=@Now, Operator=@Op
    WHERE DutyID=@SuperDuty AND IsDeleted=0 AND SubType='RESOURCE' AND ResourceID <> N'RES.FT.ClanAdmin';

    UPDATE dbo.Tbl_E_ResourcePermission SET IsDeleted=1, AmendDate=@Now, Operator=@Op
    WHERE DutyID=@SuperDuty AND IsDeleted=0 AND ResourceID <> N'RES.FT.ClanAdmin';

    IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription WHERE DutyID=@SuperDuty AND ResourceID=N'RES.FT.ClanAdmin' AND IsDeleted=0)
        INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
        VALUES (@SuperDuty, @App, 'RESOURCE', NULL, N'RES.FT.ClanAdmin', 1, '111000', 5, '1', 0, @Now, @Now, @Op);
    ELSE
        UPDATE dbo.Tbl_E_Subscription SET IsDeleted=0, FunctionLimit='111000', AmendDate=@Now, Operator=@Op
        WHERE DutyID=@SuperDuty AND ResourceID=N'RES.FT.ClanAdmin';

    IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission WHERE DutyID=@SuperDuty AND ResourceID=N'RES.FT.ClanAdmin' AND IsDeleted=0)
        INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
        VALUES (@SuperDuty, N'RES.FT.ClanAdmin', 1, 1, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op);
END

/* ========== 6. 族人 / 支链 / 族谱管 资源订阅（幂等插入） ========== */
DECLARE @MemRes TABLE (Rid VARCHAR(100), Lim VARCHAR(6), C BIT, U BIT, D BIT, Q BIT);
INSERT INTO @MemRes VALUES
('RES.FT.PersonDraft','111100',1,1,1,1),('RES.FT.MyProfile','101000',0,1,0,1),
('RES.FT.TreeView','100000',0,0,0,1),('RES.FT.Person','101000',0,1,0,1),
('RES.FT.PersonLink','110000',1,0,0,1),('RES.FT.GenerationWord','100000',0,0,0,1),
('RES.FT.PersonMarry','101000',0,1,0,1),('RES.FT.BranchApply','110000',1,0,0,1);

INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, m.Rid, 1, m.Lim, 10, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d CROSS JOIN @MemRes m
WHERE d.DutyCode=N'FT_MEMBER' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID=m.Rid AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, m.Rid, m.C, m.U, m.D, m.Q, 0, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d CROSS JOIN @MemRes m
WHERE d.DutyCode=N'FT_MEMBER' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID=m.Rid AND rp.IsDeleted=0);

DECLARE @BaRes TABLE (Rid VARCHAR(100), Lim VARCHAR(6), C BIT, U BIT, D BIT, Q BIT);
INSERT INTO @BaRes VALUES
('RES.FT.PersonDraft','100000',0,0,0,1),('RES.FT.MyProfile','101000',0,1,0,1),
('RES.FT.TreeView','100000',0,0,0,1),('RES.FT.Person','111000',1,1,0,1),
('RES.FT.PersonCreate','110000',1,0,0,1),('RES.FT.PersonLink','110000',1,0,0,1),
('RES.FT.LinkAudit','101000',0,1,0,1),('RES.FT.GenerationWord','100000',0,0,0,1),
('RES.FT.PersonMarry','101000',0,1,0,1),('RES.FT.Conflict','100000',0,0,0,1),
('RES.FT.Export','100000',0,0,0,1),('RES.FT.Peer','111000',1,1,0,1),
('RES.FT.TreeHealth','100000',0,0,0,1),('RES.FT.BranchApply','100000',0,0,0,1);

INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, m.Rid, 1, m.Lim, 10, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d CROSS JOIN @BaRes m
WHERE d.DutyCode=N'FT_BRANCH_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID=m.Rid AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, m.Rid, m.C, m.U, m.D, m.Q, 0, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d CROSS JOIN @BaRes m
WHERE d.DutyCode=N'FT_BRANCH_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID=m.Rid AND rp.IsDeleted=0);

DECLARE @ClanRes TABLE (Rid VARCHAR(100), Lim VARCHAR(6), C BIT, U BIT, D BIT, Q BIT, Disp INT);
INSERT INTO @ClanRes VALUES
('RES.FT.ClanAdmin',     '101000',0,1,0,1, 5),
('RES.FT.Person',        '101000',0,1,0,1,10),
('RES.FT.PersonCreate',  '110000',1,0,0,1,20),
('RES.FT.PersonMarry',   '101000',0,1,0,1,30),
('RES.FT.MyProfile',     '101000',0,1,0,1,40),
('RES.FT.PersonDraft',   '100000',0,0,0,1,10),
('RES.FT.PersonLink',    '110000',1,0,0,1,10),
('RES.FT.LinkAudit',     '101000',0,1,0,1,20),
('RES.FT.TreeView',      '100000',0,0,0,1,10),
('RES.FT.GenerationWord','100000',0,0,0,1,60),
('RES.FT.Export',        '100000',0,0,0,1,20),
('RES.FT.Conflict',      '101000',0,1,0,1,30);

INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, m.Rid, 1, m.Lim, m.Disp, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d CROSS JOIN @ClanRes m
WHERE d.DutyCode=N'FT_CLAN_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID=m.Rid AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, m.Rid, m.C, m.U, m.D, m.Q, 0, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d CROSS JOIN @ClanRes m
WHERE d.DutyCode=N'FT_CLAN_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID=m.Rid AND rp.IsDeleted=0);

UPDATE dbo.Tbl_E_Resource SET ResourceName=N'我的家族', AmendDate=@Now, Operator=@Op
WHERE ResourceID=N'RES.FT.ClanAdmin' AND IsDeleted=0;

/* ========== 7. 事件 ========== */
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.LINK.APPLY' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.LINK.APPLY', N'族员申请链入主谱', 'TASK', N'/FtLinkAudit/Index', 'FT_LINK', 'ASYNC', 1, N'链入待审批', 'SINGLE', 1440, 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.BRANCH.APPLY' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.BRANCH.APPLY', N'申请支链管理员', 'TASK', N'/FtClanAdmin/Index', 'FT_ORG', 'ASYNC', 1, N'支链管理员申请', 'SINGLE', 1440, 20, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.CONFLICT.OPEN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.CONFLICT.OPEN', N'匹配冲突待处理', 'TASK', N'/FtConflict/Index', 'FT_LINK', 'ASYNC', 1, N'匹配冲突', 'SINGLE', 1440, 50, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.LINK.APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.LINK.APPROVED', N'链入已通过', 'NOTICE', N'/FtPersonLink/Index', 'FT_LINK', 'ASYNC', 0, NULL, 'SINGLE', NULL, 20, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.LINK.REJECTED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.LINK.REJECTED', N'链入已驳回', 'NOTICE', N'/FtPersonLink/Index', 'FT_LINK', 'ASYNC', 0, NULL, 'SINGLE', NULL, 30, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.LINK.UNLINKED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.LINK.UNLINKED', N'已解链', 'NOTICE', N'/FtPersonLink/Index', 'FT_LINK', 'ASYNC', 0, NULL, 'SINGLE', NULL, 40, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.BRANCH.APPROVED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.BRANCH.APPROVED', N'支链管理员申请通过', 'NOTICE', N'/FtBranchApply/Index', 'FT_ORG', 'ASYNC', 0, NULL, 'SINGLE', NULL, 70, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE EventCode='FT.BRANCH.REJECTED' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'FT.BRANCH.REJECTED', N'支链管理员申请驳回', 'NOTICE', N'/FtBranchApply/Index', 'FT_ORG', 'ASYNC', 0, NULL, 'SINGLE', NULL, 80, '1', 0, @Now, @Now, @Op);

-- 超管不订阅业务事件
IF @SuperDuty IS NOT NULL
    UPDATE dbo.Tbl_E_Subscription SET IsDeleted=1, AmendDate=@Now, Operator=@Op
    WHERE DutyID=@SuperDuty AND IsDeleted=0 AND SubType='EVENT';

INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'EVENT', ev.EventCode, NULL, 1, NULL, 20, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
CROSS JOIN (VALUES ('FT.LINK.APPLY',N'FT_BRANCH_ADMIN'),('FT.BRANCH.APPLY',N'FT_CLAN_ADMIN'),('FT.CONFLICT.OPEN',N'FT_CLAN_ADMIN')) ev(EventCode,DutyCode)
WHERE d.DutyCode=ev.DutyCode AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.EventCode=ev.EventCode AND s.IsDeleted=0);

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_LINK_TODO_BA' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT 'FLOW_FT_LINK_TODO_BA', N'链入申请-支链管待办', @App, 'FT.LINK.APPLY', NULL, NULL, 'CREATE_TODO', 'DUTY', d.DataID, 'SINGLE', 11, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'FT_BRANCH_ADMIN' AND d.IsDeleted=0;

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_BRANCH_TODO_CLAN' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT 'FLOW_FT_BRANCH_TODO_CLAN', N'支链申请-族谱管待办', @App, 'FT.BRANCH.APPLY', NULL, NULL, 'CREATE_TODO', 'DUTY', d.DataID, 'SINGLE', 12, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'FT_CLAN_ADMIN' AND d.IsDeleted=0;

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_CONFLICT_TODO' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    SELECT 'FLOW_FT_CONFLICT_TODO', N'冲突-族谱管待办', @App, 'FT.CONFLICT.OPEN', NULL, NULL, 'CREATE_TODO', 'DUTY', d.DataID, 'SINGLE', 10, '1', 0, @Now, @Now, @Op
    FROM dbo.Tbl_E_Duty d WHERE d.DutyCode=N'FT_CLAN_ADMIN' AND d.IsDeleted=0;

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_LINK_OK_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_FT_LINK_OK_NOTICE', N'链入通过通知申请人', @App, 'FT.LINK.APPROVED', NULL, NULL, 'SEND_NOTICE', 'TRIGGER_USER', NULL, 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_LINK_NO_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_FT_LINK_NO_NOTICE', N'链入驳回通知申请人', @App, 'FT.LINK.REJECTED', NULL, NULL, 'SEND_NOTICE', 'TRIGGER_USER', NULL, 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_UNLINK_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_FT_UNLINK_NOTICE', N'解链通知相关人', @App, 'FT.LINK.UNLINKED', NULL, NULL, 'SEND_NOTICE', 'TRIGGER_USER', NULL, 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_BRANCH_OK_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_FT_BRANCH_OK_NOTICE', N'支链申请通过通知申请人', @App, 'FT.BRANCH.APPROVED', NULL, NULL, 'SEND_NOTICE', 'TRIGGER_USER', NULL, 'SINGLE', 10, '1', 0, @Now, @Now, @Op);
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode='FLOW_FT_BRANCH_NO_NOTICE' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType, TargetDutyID, HandleMode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FLOW_FT_BRANCH_NO_NOTICE', N'支链申请驳回通知申请人', @App, 'FT.BRANCH.REJECTED', NULL, NULL, 'SEND_NOTICE', 'TRIGGER_USER', NULL, 'SINGLE', 10, '1', 0, @Now, @Now, @Op);

/* ========== 8. cfadmin 仅挂超管岗 ========== */
DECLARE @Uid INT = (SELECT TOP 1 DataID FROM dbo.Tbl_E_Users WHERE LoginId=N'cfadmin' AND IsDeleted=0);
DECLARE @DeptId INT = (SELECT TOP 1 DataID FROM dbo.Tbl_E_Department WHERE DeptCode=N'FT_ROOT' AND IsDeleted=0);
DECLARE @SuperPos INT = (SELECT TOP 1 DataID FROM dbo.Tbl_E_Position WHERE PostCode=N'FT_SUPER_ADMIN' AND IsDeleted=0);

IF @Uid IS NOT NULL AND @DeptId IS NOT NULL AND @SuperPos IS NOT NULL
BEGIN
    -- 去掉 cfadmin 上其它族谱岗（若曾手工挂过）
    UPDATE up SET BStatus='2', AmendDate=@Now, Operator=@Op
    FROM dbo.Tbl_E_UserPosition up
    INNER JOIN dbo.Tbl_E_Position p ON p.DataID=up.PosID
    WHERE up.UserID=@Uid AND up.IsDeleted=0 AND p.PostCode <> N'FT_SUPER_ADMIN';

    IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_UserPosition WHERE UserID=@Uid AND PosID=@SuperPos AND IsDeleted=0 AND BStatus='1')
        INSERT INTO dbo.Tbl_E_UserPosition (UserID, DeptID, PosID, IsPrimary, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
        VALUES (@Uid, @DeptId, @SuperPos, 1, '1', 0, @Now, @Now, @Op);
END

PRINT N'50-Seed_FamilyTree_Full: duties, menus, events, cfadmin super-only done.';
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'50-Seed_FamilyTree_Full.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'50-Seed_FamilyTree_Full.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'50-Seed_FamilyTree_Full.sql');
GO
