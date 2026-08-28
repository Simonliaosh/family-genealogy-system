-- =============================================
-- 超管菜单：去掉人物档案 / 我的填报 / 匹配链入 / 族谱视图等业务菜单及二级项
-- 只保留「家族与族谱管理员」。可重复执行。
-- =============================================
USE [FamilyTree];
GO

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-SUPER-MENU';
DECLARE @SuperDuty INT = (SELECT TOP 1 DataID FROM dbo.Tbl_E_Duty WHERE DutyCode=N'FT_SUPER_ADMIN' AND IsDeleted=0);

IF @SuperDuty IS NULL
BEGIN
    PRINT N'FT_SUPER_ADMIN missing.';
END
ELSE
BEGIN
    -- 按菜单组去掉：人物档案 FT_PERSON、填报 FT_AUDIT、链入 FT_LINK、族谱视图 FT_TREE、运维 FT_OPS
    -- 以及组织里的支链岗 RES.FT.BranchAdmin（超管不管支链）
    UPDATE s
    SET s.IsDeleted = 1, s.AmendDate = @Now, s.Operator = @Op
    FROM dbo.Tbl_E_Subscription s
    INNER JOIN dbo.Tbl_E_Resource r ON r.ResourceID = s.ResourceID AND r.IsDeleted = 0
    WHERE s.DutyID = @SuperDuty
      AND s.IsDeleted = 0
      AND s.SubType = 'RESOURCE'
      AND (
            r.MenuGroupCode IN (N'FT_PERSON', N'FT_AUDIT', N'FT_LINK', N'FT_TREE', N'FT_OPS')
            OR s.ResourceID IN (
                N'RES.FT.BranchAdmin',
                N'RES.FT.Person', N'RES.FT.PersonCreate', N'RES.FT.PersonMarry', N'RES.FT.MyProfile',
                N'RES.FT.MainTree', N'RES.FT.GenerationWord',
                N'RES.FT.PersonDraft',
                N'RES.FT.PersonLink', N'RES.FT.LinkAudit', N'RES.FT.Conflict',
                N'RES.FT.TreeView', N'RES.FT.Export',
                N'RES.FT.OpLog', N'RES.FT.BatchMatch', N'RES.FT.TreeHealth', N'RES.FT.Peer'
            )
          )
      AND s.ResourceID <> N'RES.FT.ClanAdmin';

    UPDATE rp
    SET rp.IsDeleted = 1, rp.AmendDate = @Now, rp.Operator = @Op
    FROM dbo.Tbl_E_ResourcePermission rp
    LEFT JOIN dbo.Tbl_E_Resource r ON r.ResourceID = rp.ResourceID
    WHERE rp.DutyID = @SuperDuty
      AND rp.IsDeleted = 0
      AND rp.ResourceID <> N'RES.FT.ClanAdmin'
      AND (
            rp.ResourceID LIKE N'RES.FT.%'
            OR ISNULL(r.MenuGroupCode, N'') IN (N'FT_PERSON', N'FT_AUDIT', N'FT_LINK', N'FT_TREE', N'FT_OPS')
          );

    -- 只保留 / 恢复家族管理
    IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_Subscription WHERE DutyID=@SuperDuty AND ResourceID=N'RES.FT.ClanAdmin' AND IsDeleted=0)
        INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, IsPrimary, FunctionLimit, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
        VALUES (@SuperDuty, 'FamilyTree', 'RESOURCE', NULL, N'RES.FT.ClanAdmin', 1, '111000', 5, '1', 0, @Now, @Now, @Op);
    ELSE
        UPDATE dbo.Tbl_E_Subscription
        SET IsDeleted=0, BStatus='1', FunctionLimit='111000', AmendDate=@Now, Operator=@Op
        WHERE DutyID=@SuperDuty AND ResourceID=N'RES.FT.ClanAdmin';

    IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_E_ResourcePermission WHERE DutyID=@SuperDuty AND ResourceID=N'RES.FT.ClanAdmin' AND IsDeleted=0)
        INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
        VALUES (@SuperDuty, N'RES.FT.ClanAdmin', 1, 1, 0, 1, 0, 0, '1', 0, @Now, @Now, @Op);
    ELSE
        UPDATE dbo.Tbl_E_ResourcePermission
        SET IsDeleted=0, CanCreate=1, CanUpdate=1, CanQuery=1, BStatus='1', AmendDate=@Now, Operator=@Op
        WHERE DutyID=@SuperDuty AND ResourceID=N'RES.FT.ClanAdmin';

    UPDATE dbo.Tbl_E_Resource
    SET ResourceName = N'家族与族谱管理员', AmendDate=@Now, Operator=@Op
    WHERE ResourceID=N'RES.FT.ClanAdmin' AND IsDeleted=0;

    PRINT N'Super admin menu: only RES.FT.ClanAdmin (no Person/Draft/Link/Tree groups).';
END
GO
