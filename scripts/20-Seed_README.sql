/* 库名统一为 FamilyTree：本脚本原先没有 USE，会落在执行工具当时选中的库上。 */
USE [FamilyTree];
GO

/*
================================================================================
  EFrame 框架种子数据 - 执行说明
================================================================================
  前置（空库，二选一建表）：
    A) scripts\10-EFrame.sql               （SSMS 导出的完整建表）
    B) ..\docs\EFrame_CreateTables.sql
       + ..\docs\EFrame_v2_supplement.sql
    仪表盘表：scripts\11-CreateTbl_Dash_All.sql（可选）

  种子（按顺序在 SSMS 中执行，或 SQLCMD 模式执行本目录 20~25）：
    20-Seed_Foundation.sql      应用模块、菜单组、字典
    21-Seed_Organization.sql    部门、岗位、职责、岗位职责
    22-Seed_Users.sql           用户、任岗、人员档案
    23-Seed_Menus.sql           菜单资源、订阅、按钮权限
    24-Seed_Events.sql          事件配置、流转规则
    25-Seed_Dashboard.sql       仪表盘指标/模板（可选，需 Dash 表）

  贸易公司默认模板（可选，在 20~25 之后）：
    26-Seed_Trade_Company_Default.sql  贸易组织/职责/事件/订阅/演示账号

  初始账号（口令由 sqlcmd -v AdminPassword 传入，脚本内不写明文，仅首次插入时设置）：
    cfadmin   超级管理员（全菜单）
    frameop   框架运维
    testuser  普通用户
    hrstaff   人事员工
    hrmgr     人事主管
    执行示例：sqlcmd -S <server> -d <db> -v AdminPassword="你的强口令" -i 22-Seed_Users.sql

  说明：
    - 数据参考 EFrame.xls，幂等可重复执行
    - 不含历史事件实例/待办/请假单等业务流水（保持库干净）
    - 执行后请重新登录以刷新侧栏菜单
================================================================================
*/
PRINT N'请按顺序执行 20-Seed_Foundation.sql ~ 25-Seed_Dashboard.sql';
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
IF EXISTS (SELECT 1 FROM dbo.SchemaScriptLog WHERE ScriptName = N'20-Seed_README.sql')
    UPDATE dbo.SchemaScriptLog
       SET AppliedAt = GETDATE(), AppliedBy = SUSER_SNAME(), RunCount = RunCount + 1
     WHERE ScriptName = N'20-Seed_README.sql';
ELSE
    INSERT INTO dbo.SchemaScriptLog (ScriptName) VALUES (N'20-Seed_README.sql');
GO
