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
