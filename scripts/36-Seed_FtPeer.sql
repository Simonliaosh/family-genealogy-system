-- =============================================
-- 外链对接菜单资源（分支管/超管）
-- 可重复执行。
-- =============================================
USE [FamilyTree];
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-PEER';
DECLARE @App VARCHAR(50) = 'FamilyTree';

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode='FT_LINK' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES ('FT_LINK', @App, N'链入与对接', 40, '1', 0, @Now, @Now, @Op);

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID='RES.FT.Peer' AND IsDeleted=0)
    INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
    VALUES (@App, 'RES.FT.Peer', N'外链对接', 'MENU', N'/FtPeer/Index', 'FT_LINK', 80, '1', 0, @Now, @Now, @Op);

-- 分支管
INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, 'RES.FT.Peer', 1, '111000', 80, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
WHERE d.DutyCode=N'FT_BRANCH_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID='RES.FT.Peer' AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, 'RES.FT.Peer', 1, 1, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
WHERE d.DutyCode=N'FT_BRANCH_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID='RES.FT.Peer');

-- 超管（若尚未全量订阅，补这一条）
INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, @App, 'RESOURCE', NULL, 'RES.FT.Peer', 1, '111111', 80, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription s WHERE s.DutyID=d.DataID AND s.ResourceID='RES.FT.Peer' AND s.IsDeleted=0);

INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
SELECT d.DataID, 'RES.FT.Peer', 1, 1, 1, 1, 0, 0, '1', 0, @Now, @Now, @Op
FROM dbo.Tbl_E_Duty d
WHERE d.DutyCode=N'FT_SUPER_ADMIN' AND d.IsDeleted=0
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission rp WHERE rp.DutyID=d.DataID AND rp.ResourceID='RES.FT.Peer');
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'36-Seed_FtPeer.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'36-Seed_FtPeer.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'36-Seed_FtPeer.sql');
GO
