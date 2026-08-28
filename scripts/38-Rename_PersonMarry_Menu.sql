/*
  补丁：菜单文案「婚姻房支」→「配偶信息」
  可重复执行。
*/
SET NOCOUNT ON;
DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'PATCH-MARRY-NAME';

UPDATE dbo.Tbl_E_Resource
SET ResourceName = N'配偶信息', AmendDate = @Now, Operator = @Op
WHERE ResourceID = 'RES.FT.PersonMarry' AND IsDeleted = 0;

PRINT N'已更新 RES.FT.PersonMarry 显示名为「配偶信息」。';
