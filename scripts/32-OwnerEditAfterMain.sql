-- =============================================
-- 入谱后仍可由录入人（Owner）自行修改人物档案与配偶信息。
-- 只做 UPDATE，可重复执行；改的是已存在的订阅与资源权限，不新增行。
-- AppCode = FamilyTree
-- =============================================
USE [FamilyTree];
GO

DECLARE @Now DATETIME = GETDATE();

UPDATE s
SET s.FunctionLimit = '101000',
    s.AmendDate = @Now
FROM dbo.Tbl_E_Subscription s
INNER JOIN dbo.Tbl_E_Duty d ON d.DataID = s.DutyID AND d.IsDeleted = 0
WHERE d.DutyCode = N'FT_MEMBER'
  AND s.ResourceID IN ('RES.FT.Person', 'RES.FT.PersonMarry')
  AND s.IsDeleted = 0;

UPDATE rp
SET rp.CanUpdate = 1,
    rp.CanQuery = 1,
    rp.AmendDate = @Now
FROM dbo.Tbl_E_ResourcePermission rp
INNER JOIN dbo.Tbl_E_Duty d ON d.DataID = rp.DutyID AND d.IsDeleted = 0
WHERE d.DutyCode = N'FT_MEMBER'
  AND rp.ResourceID IN ('RES.FT.Person', 'RES.FT.PersonMarry');
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'32-OwnerEditAfterMain.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'32-OwnerEditAfterMain.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'32-OwnerEditAfterMain.sql');
GO
