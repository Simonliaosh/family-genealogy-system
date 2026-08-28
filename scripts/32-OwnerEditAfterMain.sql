-- =============================================
-- ���׺�¼�����Կɸģ����˿�������/�����ġ��ġ�
-- ���ظ�ִ�С������������ѡ�����ݣ�����Ȩ�޲Ż�ˢ�¡�
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
