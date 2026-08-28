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

DECLARE @ClanCode NVARCHAR(16) = N'DEFAULT';
DECLARE @PurgeMemberUsers BIT = 0;       -- 改为 1 则同时软删该族测试用户（保留 cfadmin）
DECLARE @IncludeOrphanPersons BIT = 0;   -- 改为 1 则一并清理 ClanId IS NULL 的历史人物
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

    /* ----- 5. 字辈（全库共用表；仅默认族时清空） ----- */
    IF OBJECT_ID(N'dbo.FamilyTree_GenerationWord', N'U') IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM dbo.FamilyTree_Clan c
           WHERE c.IsDeleted = 0 AND c.DataID <> @ClanId
       )
        DELETE FROM dbo.FamilyTree_GenerationWord;

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
