/* 库名统一为 FamilyTree：本脚本原先没有 USE，会落在执行工具当时选中的库上。 */
USE [FamilyTree];
GO

/*
  补丁：族员「录入人物」开通删除（列表删除按钮）
  可重复执行。
*/
SET NOCOUNT ON;
DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'PATCH-DRAFT-DEL';

UPDATE s
SET FunctionLimit = '111100', AmendDate = @Now, Operator = @Op
FROM dbo.Tbl_E_Subscription s
INNER JOIN dbo.Tbl_E_Duty d ON d.DataID = s.DutyID
WHERE d.DutyCode = N'FT_MEMBER' AND d.IsDeleted = 0
  AND s.ResourceID = 'RES.FT.PersonDraft' AND s.IsDeleted = 0;

UPDATE rp
SET CanDelete = 1, AmendDate = @Now, Operator = @Op
FROM dbo.Tbl_E_ResourcePermission rp
INNER JOIN dbo.Tbl_E_Duty d ON d.DataID = rp.DutyID
WHERE d.DutyCode = N'FT_MEMBER' AND d.IsDeleted = 0
  AND rp.ResourceID = 'RES.FT.PersonDraft' AND rp.IsDeleted = 0;

PRINT N'已为 FT_MEMBER 开通 RES.FT.PersonDraft 删除权限。';

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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'39-PersonDraft_Member_Delete.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'39-PersonDraft_Member_Delete.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'39-PersonDraft_Member_Delete.sql');
GO
