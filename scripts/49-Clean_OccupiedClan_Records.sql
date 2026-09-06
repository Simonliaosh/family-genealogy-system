-- =============================================
-- 清理「被占用」但已失效的家族相关记录
--
-- 典型场景（如跑过 48-Clean_DefaultClan_Data.sql 之后）：
--   1. FamilyTree_ClanCreateInvite 为 USED，但 UsedClanId 指向的家族已删
--   2. FamilyTree_UserClan 仍挂着已删/不存在的 ClanId，导致无法再次建族/入族
--   3. FamilyTree_Person.ClanId 指向已删家族（可选清理）
--
-- 本脚本不删 Tbl_E_Users，不删仍有效的家族与邀请码。
-- 建议先备份；默认软删 UserClan，邀请码恢复为 OPEN（未过期时）。
-- =============================================
USE [FamilyTree];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Op VARCHAR(30) = 'CLEAN-OCCUPIED';
DECLARE @Now DATETIME = GETDATE();

-- 指定邀请码时只处理该码；NULL 表示处理全部不一致记录
DECLARE @InviteCode VARCHAR(16) = NULL;   -- 例：'0BC50B3319'
-- 1=即使家族仍存在也强制把指定邀请码改回 OPEN（慎用）
DECLARE @ForceResetInvite BIT = 0;
-- 1=把人物上指向已删家族的 ClanId 置 NULL
DECLARE @ClearPersonClanId BIT = 1;

PRINT N'=== 清理前预览 ===';

IF OBJECT_ID(N'dbo.FamilyTree_UserClan', N'U') IS NOT NULL
BEGIN
    SELECT uc.DataID, uc.UserId, uc.ClanId, uc.IsDeleted, N'悬空入族' AS Issue
    FROM dbo.FamilyTree_UserClan uc
    WHERE uc.IsDeleted = 0
      AND NOT EXISTS (
          SELECT 1 FROM dbo.FamilyTree_Clan c
          WHERE c.DataID = uc.ClanId AND c.IsDeleted = 0
      );
END

IF OBJECT_ID(N'dbo.FamilyTree_ClanCreateInvite', N'U') IS NOT NULL
BEGIN
    SELECT i.DataID, i.InviteCode, i.InviteStatus, i.UsedByUserId, i.UsedClanId, i.ExpireAt,
           CASE
               WHEN i.InviteStatus = 'USED' AND (
                   i.UsedClanId IS NULL
                   OR NOT EXISTS (
                       SELECT 1 FROM dbo.FamilyTree_Clan c
                       WHERE c.DataID = i.UsedClanId AND c.IsDeleted = 0
                   )
               ) THEN N'已占用但家族无效'
               WHEN i.InviteStatus = 'USED' AND @ForceResetInvite = 1
                    AND (@InviteCode IS NULL OR i.InviteCode = @InviteCode) THEN N'强制重置'
               ELSE N'（跳过）'
           END AS Issue
    FROM dbo.FamilyTree_ClanCreateInvite i
    WHERE i.IsDeleted = 0
      AND (
          (
              i.InviteStatus = 'USED'
              AND (
                  i.UsedClanId IS NULL
                  OR NOT EXISTS (
                      SELECT 1 FROM dbo.FamilyTree_Clan c
                      WHERE c.DataID = i.UsedClanId AND c.IsDeleted = 0
                  )
              )
          )
          OR (
              @ForceResetInvite = 1
              AND i.InviteStatus = 'USED'
              AND (@InviteCode IS NULL OR i.InviteCode = @InviteCode)
          )
      )
      AND (@InviteCode IS NULL OR i.InviteCode = @InviteCode);
END

IF @ClearPersonClanId = 1 AND OBJECT_ID(N'dbo.FamilyTree_Person', N'U') IS NOT NULL
BEGIN
    SELECT p.DataID, p.FullName, p.ClanId, p.OwnerUserId, N'人物 ClanId 悬空' AS Issue
    FROM dbo.FamilyTree_Person p
    WHERE p.IsDeleted = 0
      AND p.ClanId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM dbo.FamilyTree_Clan c
          WHERE c.DataID = p.ClanId AND c.IsDeleted = 0
      );
END

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @UserClanCnt INT = 0;
    DECLARE @InviteCnt INT = 0;
    DECLARE @PersonCnt INT = 0;

    /* ----- 1. 悬空入族：家族已不存在 ----- */
    IF OBJECT_ID(N'dbo.FamilyTree_UserClan', N'U') IS NOT NULL
    BEGIN
        UPDATE uc
        SET IsDeleted = 1, BStatus = '2', AmendDate = @Now, OperatorName = @Op
        FROM dbo.FamilyTree_UserClan uc
        WHERE uc.IsDeleted = 0
          AND NOT EXISTS (
              SELECT 1 FROM dbo.FamilyTree_Clan c
              WHERE c.DataID = uc.ClanId AND c.IsDeleted = 0
          );
        SET @UserClanCnt = @@ROWCOUNT;
    END

    /* ----- 2. 邀请码：USED 但家族无效 → 恢复 OPEN ----- */
    IF OBJECT_ID(N'dbo.FamilyTree_ClanCreateInvite', N'U') IS NOT NULL
    BEGIN
        UPDATE i
        SET InviteStatus = CASE
                WHEN i.ExpireAt < @Now THEN 'REVOKED'
                ELSE 'OPEN'
            END,
            UsedByUserId = NULL,
            UsedClanId = NULL,
            AmendDate = @Now,
            OperatorName = @Op
        FROM dbo.FamilyTree_ClanCreateInvite i
        WHERE i.IsDeleted = 0
          AND i.InviteStatus = 'USED'
          AND (@InviteCode IS NULL OR i.InviteCode = @InviteCode)
          AND (
              @ForceResetInvite = 1
              OR i.UsedClanId IS NULL
              OR NOT EXISTS (
                  SELECT 1 FROM dbo.FamilyTree_Clan c
                  WHERE c.DataID = i.UsedClanId AND c.IsDeleted = 0
              )
          );
        SET @InviteCnt = @@ROWCOUNT;
    END

    /* ----- 3. 人物：ClanId 指向已删家族 ----- */
    IF @ClearPersonClanId = 1 AND OBJECT_ID(N'dbo.FamilyTree_Person', N'U') IS NOT NULL
    BEGIN
        UPDATE p
        SET ClanId = NULL, AmendDate = @Now, Operator = @Op
        FROM dbo.FamilyTree_Person p
        WHERE p.IsDeleted = 0
          AND p.ClanId IS NOT NULL
          AND NOT EXISTS (
              SELECT 1 FROM dbo.FamilyTree_Clan c
              WHERE c.DataID = p.ClanId AND c.IsDeleted = 0
          );
        SET @PersonCnt = @@ROWCOUNT;
    END

    COMMIT TRANSACTION;

    PRINT N'清理完成：入族记录 ' + CAST(@UserClanCnt AS VARCHAR(20))
        + N' 条，邀请码 ' + CAST(@InviteCnt AS VARCHAR(20))
        + N' 条，人物 ClanId ' + CAST(@PersonCnt AS VARCHAR(20)) + N' 条。';
    IF @InviteCnt > 0
        PRINT N'提示：已恢复的邀请码若未过期可重新验证；已过期则状态为 REVOKED，需超管重新发码。';
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'49-Clean_OccupiedClan_Records.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'49-Clean_OccupiedClan_Records.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'49-Clean_OccupiedClan_Records.sql');
GO
