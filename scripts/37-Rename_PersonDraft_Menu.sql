/*
  补丁：菜单文案「未完成填报」→「录入人物」
  可重复执行。
*/
SET NOCOUNT ON;
DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'PATCH-DRAFT-NAME';

UPDATE dbo.Tbl_E_Resource
SET ResourceName = N'录入人物', AmendDate = @Now, Operator = @Op
WHERE ResourceID = 'RES.FT.PersonDraft' AND IsDeleted = 0;

UPDATE dbo.Tbl_E_MenuGroup
SET MenuGroupName = N'录入人物', AmendDate = @Now, Operator = @Op
WHERE MenuGroupCode = 'FT_AUDIT' AND IsDeleted = 0;

PRINT N'已更新 RES.FT.PersonDraft / FT_AUDIT 显示名为「录入人物」。';
