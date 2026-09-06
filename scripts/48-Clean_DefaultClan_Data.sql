-- =============================================
-- 清理「默认家族」（ClanCode = DEFAULT）业务数据
-- 来源：scripts/43-Clan.sql 迁入的历史/测试数据
--
-- 保留：
--   - Tbl_E_* 框架（岗位/菜单/事件配置）
--   - cfadmin 超管账号及其 FT_SUPER_ADMIN 岗
--
-- 可选（脚本末尾 @PurgeMemberUsers = 1）：
--   - 删除默认家族下的族人测试账号（LoginId <> cfadmin）
--
-- ⚠ 生产环境慎用；建议先备份。默认物理删除业务行。
-- 执行前请确认无其它正式家族误用 ClanCode='DEFAULT'。
-- =============================================
USE [FamilyTree];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/* 开关全部走 sqlcmd 变量，不必改文件。示例：
     sqlcmd -S <server> -d FamilyTree -v ClanCode="DEFAULT" PurgeMemberUsers=0 ^
            IncludeOrphanPersons=0 PurgeGenerationWords=0 -i 48-Clean_DefaultClan_Data.sql
   （SSMS 需先打开 SQLCMD 模式。未传入时取下面 ISNULL 的默认值，全部为「最保守」。） */
DECLARE @ClanCode NVARCHAR(16) = ISNULL(NULLIF(N'$(ClanCode)', N'$' + N'(ClanCode)'), N'DEFAULT');
-- 1 = 同时软删该族测试用户（保留 cfadmin）
DECLARE @PurgeMemberUsers BIT = ISNULL(TRY_CAST(NULLIF('$(PurgeMemberUsers)', '$' + '(PurgeMemberUsers)') AS BIT), 0);
-- 1 = 一并清理 ClanId IS NULL 的历史人物。注意：43-Clan.sql:67 回填之后，
--     这批就是「历史数据」而不是「孤儿测试数据」，打开前务必先看第 28~88 行的预览计数。
DECLARE @IncludeOrphanPersons BIT = ISNULL(TRY_CAST(NULLIF('$(IncludeOrphanPersons)', '$' + '(IncludeOrphanPersons)') AS BIT), 0);
-- 1 = 清空全库共用的字辈表。默认 0，见下方第 5 段说明。
DECLARE @PurgeGenerationWords BIT = ISNULL(TRY_CAST(NULLIF('$(PurgeGenerationWords)', '$' + '(PurgeGenerationWords)') AS BIT), 0);
DECLARE @Op VARCHAR(30) = 'CLEAN-DEFAULT';
DECLARE @Now DATETIME = GETDATE();

DECLARE @ClanId INT = (
    SELECT TOP 1 DataID FROM dbo.FamilyTree_Clan
    WHERE ClanCode = @ClanCode AND IsDeleted = 0
);

IF @ClanId IS NULL
BEGIN
    PRINT N'未找到 ClanCode=' + @ClanCode + N' 的家族，无需清理。';
    RETURN;
END

PRINT N'目标家族 DataID=' + CAST(@ClanId AS VARCHAR(20)) + N' (' + @ClanCode + N')';

IF OBJECT_ID('tempdb..#FtPerson') IS NOT NULL DROP TABLE #FtPerson;
IF OBJECT_ID('tempdb..#FtUser') IS NOT NULL DROP TABLE #FtUser;

SELECT p.DataID AS PersonId
INTO #FtPerson
FROM dbo.FamilyTree_Person p
WHERE p.ClanId = @ClanId
   OR (@IncludeOrphanPersons = 1 AND p.ClanId IS NULL);

-- 入族用户 + 人物 Owner/Bind（可能未写 UserClan）
SELECT DISTINCT uid AS UserId
INTO #FtUser
FROM (
    SELECT uc.UserId AS uid
    FROM dbo.FamilyTree_UserClan uc
    WHERE uc.IsDeleted = 0 AND uc.ClanId = @ClanId
    UNION
    SELECT p.OwnerUserId FROM dbo.FamilyTree_Person p
    INNER JOIN #FtPerson fp ON fp.PersonId = p.DataID
    WHERE p.OwnerUserId > 0
    UNION
    SELECT p.BindUserId FROM dbo.FamilyTree_Person p
    INNER JOIN #FtPerson fp ON fp.PersonId = p.DataID
    WHERE p.BindUserId IS NOT NULL AND p.BindUserId > 0
) x
WHERE uid IS NOT NULL AND uid > 0;

DECLARE @PersonCnt INT = (SELECT COUNT(*) FROM #FtPerson);
DECLARE @UserCnt INT = (SELECT COUNT(*) FROM #FtUser);
PRINT N'将清理人物 ' + CAST(@PersonCnt AS VARCHAR(20)) + N' 人，关联用户 ' + CAST(@UserCnt AS VARCHAR(20)) + N' 个。';

BEGIN TRANSACTION;

BEGIN TRY
    /* ----- 1. 族谱待办 / 事件实例（FamilyTree 应用，按外键顺序删除） ----- */
    IF OBJECT_ID('tempdb..#FtEventInst') IS NOT NULL DROP TABLE #FtEventInst;
    IF OBJECT_ID('tempdb..#FtTodo') IS NOT NULL DROP TABLE #FtTodo;
    IF OBJECT_ID('tempdb..#FtTodoGrp') IS NOT NULL DROP TABLE #FtTodoGrp;

    CREATE TABLE #FtEventInst (InstanceId BIGINT NOT NULL PRIMARY KEY);

    INSERT INTO #FtEventInst (InstanceId)
    SELECT ei.DataID
    FROM dbo.Tbl_E_EventInstance ei
    WHERE ei.AppCode = 'FamilyTree'
      AND (
          ei.TriggerUserID IN (SELECT UserId FROM #FtUser)
          OR EXISTS (
              SELECT 1 FROM #FtPerson fp
              WHERE ei.ObjectKey = CAST(fp.PersonId AS NVARCHAR(100))
          )
      );

    SELECT t.DataID AS TodoId
    INTO #FtTodo
    FROM dbo.Tbl_E_TodoTask t
    WHERE t.AppCode = 'FamilyTree'
      AND (
          t.UserID IN (SELECT UserId FROM #FtUser)
          OR EXISTS (
              SELECT 1 FROM #FtPerson fp
              WHERE t.ObjectKey = CAST(fp.PersonId AS NVARCHAR(100))
          )
          OR t.EventInstanceID IN (SELECT InstanceId FROM #FtEventInst)
      );

    INSERT INTO #FtEventInst (InstanceId)
    SELECT DISTINCT t.EventInstanceID
    FROM dbo.Tbl_E_TodoTask t
    INNER JOIN #FtTodo x ON x.TodoId = t.DataID
    WHERE t.EventInstanceID IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM #FtEventInst e WHERE e.InstanceId = t.EventInstanceID);

    SELECT g.DataID AS GroupId
    INTO #FtTodoGrp
    FROM dbo.Tbl_E_TodoGroup g
    WHERE g.AppCode = 'FamilyTree'
      AND (
          g.EventInstanceID IN (SELECT InstanceId FROM #FtEventInst)
          OR EXISTS (
              SELECT 1 FROM #FtPerson fp
              WHERE g.ObjectKey = CAST(fp.PersonId AS NVARCHAR(100))
          )
      );

    INSERT INTO #FtEventInst (InstanceId)
    SELECT DISTINCT g.EventInstanceID
    FROM dbo.Tbl_E_TodoGroup g
    INNER JOIN #FtTodoGrp x ON x.GroupId = g.DataID
    WHERE g.EventInstanceID IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM #FtEventInst e WHERE e.InstanceId = g.EventInstanceID);

    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tbl_E_TodoTaskLog')
        DELETE l
        FROM dbo.Tbl_E_TodoTaskLog l
        INNER JOIN #FtTodo x ON x.TodoId = l.TodoTaskID;

    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tbl_E_TodoCandidate')
    BEGIN
        DELETE c
        FROM dbo.Tbl_E_TodoCandidate c
        INNER JOIN #FtTodo x ON x.TodoId = c.TodoTaskID;

        DELETE c
        FROM dbo.Tbl_E_TodoCandidate c
        INNER JOIN #FtTodoGrp g ON g.GroupId = c.TodoGroupID;
    END

    DELETE t
    FROM dbo.Tbl_E_TodoTask t
    INNER JOIN #FtTodo x ON x.TodoId = t.DataID;

    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tbl_E_TodoGroup')
        DELETE g
        FROM dbo.Tbl_E_TodoGroup g
        INNER JOIN #FtTodoGrp x ON x.GroupId = g.DataID;

    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tbl_E_EventReceiver')
        DELETE er
        FROM dbo.Tbl_E_EventReceiver er
        INNER JOIN #FtEventInst ei ON ei.InstanceId = er.EventInstanceID;

    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tbl_E_EventLog')
    BEGIN
        DELETE el
        FROM dbo.Tbl_E_EventLog el
        INNER JOIN #FtEventInst ei ON ei.InstanceId = el.EventInstanceID;

        DELETE el
        FROM dbo.Tbl_E_EventLog el
        WHERE el.AppCode = 'FamilyTree'
          AND el.EventInstanceID IS NULL
          AND (
              el.UserID IN (SELECT UserId FROM #FtUser)
              OR EXISTS (
                  SELECT 1 FROM #FtPerson fp
                  WHERE el.ObjectKey = CAST(fp.PersonId AS NVARCHAR(100))
              )
          );
    END

    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tbl_E_EventDelivery')
        DELETE ed
        FROM dbo.Tbl_E_EventDelivery ed
        INNER JOIN #FtEventInst ei ON ei.InstanceId = ed.EventInstanceID;

    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tbl_E_EventInstance')
        DELETE ei
        FROM dbo.Tbl_E_EventInstance ei
        INNER JOIN #FtEventInst x ON x.InstanceId = ei.DataID;

    /* ----- 2. 外链 / 支链申请 / 邀请 ----- */
    IF OBJECT_ID(N'dbo.FamilyTree_PeerBridge', N'U') IS NOT NULL
        DELETE pb
        FROM dbo.FamilyTree_PeerBridge pb
        INNER JOIN #FtPerson fp ON fp.PersonId = pb.LocalPersonId;

    IF OBJECT_ID(N'dbo.FamilyTree_PeerInvite', N'U') IS NOT NULL
        DELETE pi
        FROM dbo.FamilyTree_PeerInvite pi
        INNER JOIN #FtPerson fp ON fp.PersonId = pi.LocalPersonId;

    IF OBJECT_ID(N'dbo.FamilyTree_BranchAdminApply', N'U') IS NOT NULL
        DELETE ba
        FROM dbo.FamilyTree_BranchAdminApply ba
        WHERE ba.ApplyUserId IN (SELECT UserId FROM #FtUser);

    IF OBJECT_ID(N'dbo.FamilyTree_ClanCreateInvite', N'U') IS NOT NULL
        DELETE ci
        FROM dbo.FamilyTree_ClanCreateInvite ci
        WHERE ci.UsedClanId = @ClanId;

    /* ----- 3. 匹配 / 链入 / 冲突 ----- */
    IF OBJECT_ID(N'dbo.FamilyTree_MatchExclude', N'U') IS NOT NULL
        DELETE me
        FROM dbo.FamilyTree_MatchExclude me
        WHERE me.PersonLoId IN (SELECT PersonId FROM #FtPerson)
           OR me.PersonHiId IN (SELECT PersonId FROM #FtPerson);

    IF OBJECT_ID(N'dbo.FamilyTree_MatchConflict', N'U') IS NOT NULL
        DELETE mc
        FROM dbo.FamilyTree_MatchConflict mc
        WHERE mc.SourcePersonId IN (SELECT PersonId FROM #FtPerson)
           OR mc.TargetPersonId IN (SELECT PersonId FROM #FtPerson);

    IF OBJECT_ID(N'dbo.FamilyTree_PersonLink', N'U') IS NOT NULL
        DELETE pl
        FROM dbo.FamilyTree_PersonLink pl
        WHERE pl.SourcePersonId IN (SELECT PersonId FROM #FtPerson)
           OR pl.TargetMainPersonId IN (SELECT PersonId FROM #FtPerson);

    /* ----- 4. 配偶 / 草稿 / 日志 ----- */
    IF OBJECT_ID(N'dbo.FamilyTree_PersonMarry', N'U') IS NOT NULL
        DELETE m
        FROM dbo.FamilyTree_PersonMarry m
        INNER JOIN #FtPerson fp ON fp.PersonId = m.PersonId;

    IF OBJECT_ID(N'dbo.FamilyTree_PersonDraft', N'U') IS NOT NULL
        DELETE d
        FROM dbo.FamilyTree_PersonDraft d
        WHERE d.SubmitUserId IN (SELECT UserId FROM #FtUser);

    IF OBJECT_ID(N'dbo.FamilyTree_OpLog', N'U') IS NOT NULL
        DELETE o
        FROM dbo.FamilyTree_OpLog o
        WHERE o.OpUserId IN (SELECT UserId FROM #FtUser)
           OR o.ObjectKey IN (SELECT CAST(PersonId AS VARCHAR(64)) FROM #FtPerson);

    /* ----- 5. 字辈 -----
       原先这里是 DELETE FROM dbo.FamilyTree_GenerationWord;（无 WHERE）——
       守卫只检查「不存在其他族」，而那恰好就是单租户生产环境的常态，
       于是这条语句在最典型的部署上会清空整张字辈表。
       字辈表没有 ClanId 列、确实是全库共用的，因此改为：默认不动，
       只有把 @PurgeGenerationWords 显式设为 1 才清，且清之前打印行数。 */
    IF OBJECT_ID(N'dbo.FamilyTree_GenerationWord', N'U') IS NOT NULL
    BEGIN
        IF @PurgeGenerationWords = 1
        BEGIN
            DECLARE @GwCount INT;
            SELECT @GwCount = COUNT(*) FROM dbo.FamilyTree_GenerationWord;
            PRINT N'即将清空 FamilyTree_GenerationWord，行数 = ' + CAST(@GwCount AS NVARCHAR(20));
            DELETE FROM dbo.FamilyTree_GenerationWord;
        END
        ELSE
            PRINT N'跳过 FamilyTree_GenerationWord（全库共用表）。如确需清空，请设 @PurgeGenerationWords = 1。';
    END

    /* ----- 6. 支链（TopPerson / Admin 关联默认族人物） ----- */
    IF OBJECT_ID(N'dbo.FamilyTree_Branch', N'U') IS NOT NULL
        DELETE b
        FROM dbo.FamilyTree_Branch b
        WHERE b.TopPersonId IN (SELECT PersonId FROM #FtPerson)
           OR b.AdminUserId IN (SELECT UserId FROM #FtUser)
           OR EXISTS (
               SELECT 1 FROM dbo.FamilyTree_Person p
               INNER JOIN #FtPerson fp ON fp.PersonId = p.DataID
               WHERE p.BranchId = b.DataID
           );

    /* ----- 7. 人物主档 ----- */
    DELETE p
    FROM dbo.FamilyTree_Person p
    INNER JOIN #FtPerson fp ON fp.PersonId = p.DataID;

    /* ----- 8. 入族 / 家族 ----- */
    DELETE FROM dbo.FamilyTree_UserClan
    WHERE ClanId = @ClanId;

    DELETE FROM dbo.FamilyTree_Clan
    WHERE DataID = @ClanId;

    /* ----- 9. 可选：删除测试族人账号（保留 cfadmin） ----- */
    IF @PurgeMemberUsers = 1
    BEGIN
        DECLARE @FtPos TABLE (PosId INT PRIMARY KEY);
        INSERT INTO @FtPos (PosId)
        SELECT p.DataID
        FROM dbo.Tbl_E_Position p
        WHERE p.IsDeleted = 0
          AND p.PostCode IN (N'FT_MEMBER', N'FT_BRANCH_ADMIN', N'FT_CLAN_ADMIN');

        DELETE up
        FROM dbo.Tbl_E_UserPosition up
        INNER JOIN @FtPos fp ON fp.PosId = up.PosID
        INNER JOIN #FtUser u ON u.UserId = up.UserID
        INNER JOIN dbo.Tbl_E_Users usr ON usr.DataID = u.UserId
        WHERE usr.LoginId <> N'cfadmin' AND usr.IsDeleted = 0;

        IF OBJECT_ID(N'dbo.FamilyTree_ApiToken', N'U') IS NOT NULL
            DELETE t
            FROM dbo.FamilyTree_ApiToken t
            INNER JOIN #FtUser u ON u.UserId = t.UserId
            INNER JOIN dbo.Tbl_E_Users usr ON usr.DataID = u.UserId
            WHERE usr.LoginId <> N'cfadmin';

        IF OBJECT_ID(N'dbo.FamilyTree_AccountBind', N'U') IS NOT NULL
            DELETE b
            FROM dbo.FamilyTree_AccountBind b
            INNER JOIN #FtUser u ON u.UserId = b.UserId
            INNER JOIN dbo.Tbl_E_Users usr ON usr.DataID = u.UserId
            WHERE usr.LoginId <> N'cfadmin';

        UPDATE usr SET IsDeleted = 1, BStatus = '2', AmendDate = @Now, Operator = @Op
        FROM dbo.Tbl_E_Users usr
        INNER JOIN #FtUser u ON u.UserId = usr.DataID
        WHERE usr.LoginId <> N'cfadmin' AND usr.IsDeleted = 0;

        PRINT N'已软删除默认家族关联测试用户（cfadmin 保留）。';
    END

    COMMIT TRANSACTION;
    PRINT N'默认家族数据清理完成。';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Msg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'清理失败，已回滚：%s', 16, 1, @Msg);
END CATCH
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'48-Clean_DefaultClan_Data.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'48-Clean_DefaultClan_Data.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'48-Clean_DefaultClan_Data.sql');
GO
