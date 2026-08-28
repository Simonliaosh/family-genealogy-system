# -*- coding: utf-8 -*-
"""生成 25-Seed_Dashboard.sql（GBK），含 9 类图表类型预置指标。兼容 SQL Server 2008（无 MERGE）。"""
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "25-Seed_Dashboard.sql")

CONTENT = r"""/*
==============================================================================
  EFrame 种子 06 - 仪表盘预置指标与模板（9 类 ChartType 全覆盖）
==============================================================================
  前置：11-CreateTbl_Dash_All.sql + 23-Seed_Menus.sql
  ChartType：1 KPI | 2 折线 | 3 柱状 | 4 饼图 | 5 表格 | 6 漏斗 | 7 进度 | 8 菜单 | 9 列表
  说明：删索引与 INSERT 必须分 GO 批次（同批编译时仍看到旧索引会 1934）
  编码：ANSI (GBK)
==============================================================================
*/

/* ==================== 0. 写入前暂删 Dash 非聚集索引（仅 PK） ==================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Tbl_Dash_Indicator', N'U') IS NULL
    RAISERROR(N'未找到 dbo.Tbl_Dash_Indicator，请先执行 11-CreateTbl_Dash_All.sql 并确认当前库正确。', 16, 1);

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_Dash_Indicator_Code' AND object_id = OBJECT_ID(N'dbo.Tbl_Dash_Indicator'))
    DROP INDEX [UQ_Tbl_Dash_Indicator_Code] ON [dbo].[Tbl_Dash_Indicator];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_Dash_Indicator_App_Status' AND object_id = OBJECT_ID(N'dbo.Tbl_Dash_Indicator'))
    DROP INDEX [IX_Tbl_Dash_Indicator_App_Status] ON [dbo].[Tbl_Dash_Indicator];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_Dash_PosTemplate_Pos_Ind' AND object_id = OBJECT_ID(N'dbo.Tbl_Dash_PosTemplate'))
    DROP INDEX [UQ_Tbl_Dash_PosTemplate_Pos_Ind] ON [dbo].[Tbl_Dash_PosTemplate];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_Dash_UserCard_UserPos_Ind' AND object_id = OBJECT_ID(N'dbo.Tbl_Dash_UserCard'))
    DROP INDEX [UQ_Tbl_Dash_UserCard_UserPos_Ind] ON [dbo].[Tbl_Dash_UserCard];

DECLARE @sql NVARCHAR(MAX);
SET @sql = N'';
SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(kc.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(kc.parent_object_id))
    + N' DROP CONSTRAINT ' + QUOTENAME(kc.name) + N';' + CHAR(13)
FROM sys.key_constraints kc
WHERE kc.parent_object_id IN (OBJECT_ID(N'dbo.Tbl_Dash_Indicator'), OBJECT_ID(N'dbo.Tbl_Dash_PosTemplate'), OBJECT_ID(N'dbo.Tbl_Dash_UserCard'))
  AND kc.type = N'UQ';
IF LEN(@sql) > 0 EXEC sp_executesql @sql;

SET @sql = N'';
SELECT @sql = @sql + N'DROP INDEX ' + QUOTENAME(i.name) + N' ON ' + QUOTENAME(OBJECT_SCHEMA_NAME(i.object_id)) + N'.' + QUOTENAME(OBJECT_NAME(i.object_id)) + N';' + CHAR(13)
FROM sys.indexes i
INNER JOIN sys.tables t ON t.object_id = i.object_id
WHERE t.name IN (N'Tbl_Dash_Indicator', N'Tbl_Dash_PosTemplate', N'Tbl_Dash_UserCard')
  AND i.type = 2;
IF LEN(@sql) > 0 EXEC sp_executesql @sql;

IF EXISTS (
    SELECT 1 FROM sys.indexes i
    INNER JOIN sys.tables t ON t.object_id = i.object_id
    WHERE t.name IN (N'Tbl_Dash_Indicator', N'Tbl_Dash_PosTemplate', N'Tbl_Dash_UserCard') AND i.type = 2)
    RAISERROR(N'Dash 非聚集索引未能全部删除，请检查权限或手动 DROP 后再执行本脚本。', 16, 1);
GO

/* ==================== 5~7. 写入种子数据（新批次重新编译，不再绑定旧索引） ==================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Now DATETIME = GETDATE();
DECLARE @Op VARCHAR(30) = 'SEED-DASH';

DECLARE @CalcEmpty NVARCHAR(MAX) = N'{"sqlText":"","globalLink":null,"itemLinkConfig":null,"filterDefault":{},"menuList":[]}';
DECLARE @CalcTodoCount NVARCHAR(MAX) = N'{"sqlText":"","globalLink":{"routePath":"/ETodoTask/Index","linkType":1,"paramPlaceholder":[]},"itemLinkConfig":null,"filterDefault":{"timeType":3},"menuList":[]}';
DECLARE @CalcTodoList NVARCHAR(MAX) = N'{"sqlText":"","globalLink":{"routePath":"/ETodoTask/Index","linkType":1,"paramPlaceholder":[]},"itemLinkConfig":{"itemRoute":"/ETodoTask/Details/{todoId}","bindDataField":["todoId"],"publicParam":[]},"filterDefault":{},"menuList":[]}';
DECLARE @CalcEventRecent NVARCHAR(MAX) = N'{"sqlText":"","globalLink":{"routePath":"/EEventInstanceQuery/Index","linkType":1,"paramPlaceholder":[]},"itemLinkConfig":{"itemRoute":"/EEventInstanceQuery/Details/{instanceId}","bindDataField":["instanceId"],"publicParam":[]},"filterDefault":{},"menuList":[]}';
DECLARE @CalcOrgStats NVARCHAR(MAX) = N'{"sqlText":"","globalLink":null,"itemLinkConfig":null,"filterDefault":{},"menuList":[]}';
DECLARE @CalcQuickMenu NVARCHAR(MAX) = N'{"sqlText":"","globalLink":null,"itemLinkConfig":null,"filterDefault":{},"menuList":[{"icon":"todo","name":"我的待办","routePath":"/ETodoTask/Index","bgColor":"#409eff"},{"icon":"event","name":"事件查询","routePath":"/EEventInstanceQuery/Index","bgColor":"#67c23a"},{"icon":"leave","name":"请假申请","routePath":"/HrLeave/Create","bgColor":"#e6a23c"},{"icon":"dash","name":"指标库","routePath":"/EDashIndicator/Index","bgColor":"#909399"}]}';

IF OBJECT_ID('tempdb..#DashIndSeed') IS NOT NULL DROP TABLE #DashIndSeed;
CREATE TABLE #DashIndSeed (
    IndicatorCode VARCHAR(50) NOT NULL,
    IndicatorName NVARCHAR(100) NOT NULL,
    ChartType TINYINT NOT NULL,
    DataSource NVARCHAR(100) NOT NULL,
    CalcRule NVARCHAR(MAX) NOT NULL,
    DispSeq INT NOT NULL,
    Remark NVARCHAR(200) NULL
);

INSERT INTO #DashIndSeed (IndicatorCode, IndicatorName, ChartType, DataSource, CalcRule, DispSeq, Remark) VALUES
(N'FRAME_TODO_COUNT',   N'待办数量',     1, N'Tbl_E_TodoTask',      @CalcTodoCount,   10, N'ChartType=1 KPI 卡片'),
(N'FRAME_ORG_STATS',    N'组织概览',     1, N'Tbl_E_Users',         @CalcOrgStats,    11, N'ChartType=1 KPI 卡片'),
(N'FRAME_DEMO_LINE',    N'登录趋势',     2, N'Tbl_E_LoginLog',      @CalcEmpty,       20, N'ChartType=2 折线图演示'),
(N'FRAME_DEMO_BAR',     N'菜单组资源',   3, N'Tbl_E_Resource',      @CalcEmpty,       30, N'ChartType=3 柱状图演示'),
(N'FRAME_DEMO_PIE',     N'用户类型分布', 4, N'Tbl_E_Users',         @CalcEmpty,       40, N'ChartType=4 饼图演示'),
(N'FRAME_DEMO_TABLE',   N'最近登录',     5, N'Tbl_E_LoginLog',      @CalcEmpty,       50, N'ChartType=5 明细表格演示'),
(N'FRAME_DEMO_FUNNEL',  N'事件状态漏斗', 6, N'Tbl_E_EventInstance', @CalcEmpty,       60, N'ChartType=6 漏斗图演示'),
(N'FRAME_DEMO_GAUGE',   N'待办完成率',   7, N'Tbl_E_TodoTask',      @CalcEmpty,       70, N'ChartType=7 进度仪表盘演示'),
(N'FRAME_QUICK_MENU',   N'快捷入口',     8, N'Tbl_E_Resource',      @CalcQuickMenu,   80, N'ChartType=8 快捷菜单'),
(N'FRAME_TODO_LIST',    N'我的待办',     9, N'Tbl_E_TodoTask',      @CalcTodoList,    90, N'ChartType=9 滚动列表'),
(N'FRAME_EVENT_RECENT', N'近期事件',     9, N'Tbl_E_EventInstance', @CalcEventRecent, 91, N'ChartType=9 滚动列表');

DECLARE @Code VARCHAR(50), @Name NVARCHAR(100), @ChartType TINYINT, @DataSource NVARCHAR(100), @CalcRule NVARCHAR(MAX), @DispSeq INT, @Remark NVARCHAR(200);
DECLARE cur_ind CURSOR LOCAL FAST_FORWARD FOR
    SELECT IndicatorCode, IndicatorName, ChartType, DataSource, CalcRule, DispSeq, Remark FROM #DashIndSeed;
OPEN cur_ind;
FETCH NEXT FROM cur_ind INTO @Code, @Name, @ChartType, @DataSource, @CalcRule, @DispSeq, @Remark;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_Dash_Indicator WHERE IndicatorCode = @Code AND IsDeleted = 0)
        INSERT INTO dbo.Tbl_Dash_Indicator (IndicatorCode, IndicatorName, AppCode, ChartType, DataSource, CalcRule, DefaultTimeScope, IsLockCalc, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
        VALUES (@Code, @Name, 'FRAME', @ChartType, @DataSource, @CalcRule, 3, 1, @DispSeq, @Remark, '1', 0, @Now, @Now, @Op);
    ELSE
        UPDATE dbo.Tbl_Dash_Indicator
        SET IndicatorName = @Name, ChartType = @ChartType, DataSource = @DataSource, CalcRule = @CalcRule,
            DispSeq = @DispSeq, Remark = @Remark, BStatus = '1', IsDeleted = 0, AmendDate = @Now, Operator = @Op
        WHERE IndicatorCode = @Code AND IsDeleted = 0;
    FETCH NEXT FROM cur_ind INTO @Code, @Name, @ChartType, @DataSource, @CalcRule, @DispSeq, @Remark;
END
CLOSE cur_ind;
DEALLOCATE cur_ind;
DROP TABLE #DashIndSeed;

INSERT INTO dbo.Tbl_Dash_PosIndicatorPerm (PosID, IndicatorID, BStatus, CreateDate, AmendDate, Operator)
SELECT p.DataID, i.DataID, '1', @Now, @Now, @Op
FROM dbo.Tbl_E_Position p
CROSS JOIN dbo.Tbl_Dash_Indicator i
WHERE p.PostCode IN (N'CF_ADMIN', N'CF_STAFF') AND p.IsDeleted = 0
  AND i.IndicatorCode LIKE N'FRAME[_]%' AND i.IsDeleted = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Tbl_Dash_PosIndicatorPerm x
      WHERE x.PosID = p.DataID AND x.IndicatorID = i.DataID AND x.BStatus = '1');

IF OBJECT_ID('tempdb..#DashTplSeed') IS NOT NULL DROP TABLE #DashTplSeed;
CREATE TABLE #DashTplSeed (
    PosCode VARCHAR(50) NOT NULL,
    IndicatorCode VARCHAR(50) NOT NULL,
    LayoutRow INT NOT NULL,
    LayoutCol INT NOT NULL,
    ColSpan TINYINT NOT NULL,
    IsLock BIT NOT NULL,
    CardTitle NVARCHAR(100) NOT NULL,
    DefaultFilterJson NVARCHAR(MAX) NOT NULL
);

INSERT INTO #DashTplSeed (PosCode, IndicatorCode, LayoutRow, LayoutCol, ColSpan, IsLock, CardTitle, DefaultFilterJson) VALUES
(N'CF_ADMIN', N'FRAME_QUICK_MENU',   1, 1, 4, 0, N'快捷入口(8)',     N'{}'),
(N'CF_ADMIN', N'FRAME_TODO_COUNT',   2, 1, 1, 1, N'待办数量(1)',     N'{}'),
(N'CF_ADMIN', N'FRAME_DEMO_GAUGE',   2, 2, 1, 0, N'待办完成率(7)',   N'{}'),
(N'CF_ADMIN', N'FRAME_ORG_STATS',    2, 3, 1, 0, N'组织概览(1)',     N'{}'),
(N'CF_ADMIN', N'FRAME_DEMO_PIE',     2, 4, 1, 0, N'用户类型(4)',     N'{}'),
(N'CF_ADMIN', N'FRAME_DEMO_LINE',    3, 1, 2, 0, N'登录趋势(2)',     N'{}'),
(N'CF_ADMIN', N'FRAME_DEMO_BAR',     3, 3, 2, 0, N'菜单组资源(3)',   N'{}'),
(N'CF_ADMIN', N'FRAME_DEMO_TABLE',   4, 1, 2, 0, N'最近登录(5)',     N'{}'),
(N'CF_ADMIN', N'FRAME_DEMO_FUNNEL',  4, 3, 2, 0, N'事件漏斗(6)',     N'{}'),
(N'CF_ADMIN', N'FRAME_TODO_LIST',    5, 1, 2, 0, N'我的待办(9)',     N'{}'),
(N'CF_ADMIN', N'FRAME_EVENT_RECENT', 5, 3, 2, 0, N'近期事件(9)',     N'{}'),
(N'CF_STAFF', N'FRAME_QUICK_MENU',   1, 1, 4, 0, N'快捷入口',       N'{}'),
(N'CF_STAFF', N'FRAME_TODO_COUNT',   2, 1, 1, 0, N'待办数量',       N'{}'),
(N'CF_STAFF', N'FRAME_DEMO_GAUGE',   2, 2, 1, 0, N'待办完成率',     N'{}'),
(N'CF_STAFF', N'FRAME_TODO_LIST',    3, 1, 2, 0, N'我的待办',       N'{}');

DECLARE @PosCode VARCHAR(50), @IndCode VARCHAR(50), @LR INT, @LC INT, @CS TINYINT, @Lock BIT, @Title NVARCHAR(100), @Filter NVARCHAR(MAX);
DECLARE @PosId INT, @IndId INT;
DECLARE cur_tpl CURSOR LOCAL FAST_FORWARD FOR
    SELECT PosCode, IndicatorCode, LayoutRow, LayoutCol, ColSpan, IsLock, CardTitle, DefaultFilterJson FROM #DashTplSeed;
OPEN cur_tpl;
FETCH NEXT FROM cur_tpl INTO @PosCode, @IndCode, @LR, @LC, @CS, @Lock, @Title, @Filter;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @PosId = (SELECT DataID FROM dbo.Tbl_E_Position WHERE PostCode = @PosCode AND IsDeleted = 0);
    SET @IndId = (SELECT DataID FROM dbo.Tbl_Dash_Indicator WHERE IndicatorCode = @IndCode AND IsDeleted = 0);
    IF @PosId IS NOT NULL AND @IndId IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_Dash_PosTemplate WHERE PosID = @PosId AND IndicatorID = @IndId AND IsDeleted = 0)
            INSERT INTO dbo.Tbl_Dash_PosTemplate (PosID, IndicatorID, LayoutRow, LayoutCol, ColSpan, IsLock, DefaultFilterJson, CardTitle, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
            VALUES (@PosId, @IndId, @LR, @LC, @CS, @Lock, @Filter, @Title, 99, '1', 0, @Now, @Now, @Op);
        ELSE
            UPDATE dbo.Tbl_Dash_PosTemplate
            SET LayoutRow = @LR, LayoutCol = @LC, ColSpan = @CS, IsLock = @Lock,
                DefaultFilterJson = @Filter, CardTitle = @Title, BStatus = '1', IsDeleted = 0, AmendDate = @Now, Operator = @Op
            WHERE PosID = @PosId AND IndicatorID = @IndId AND IsDeleted = 0;
    END
    FETCH NEXT FROM cur_tpl INTO @PosCode, @IndCode, @LR, @LC, @CS, @Lock, @Title, @Filter;
END
CLOSE cur_tpl;
DEALLOCATE cur_tpl;
DROP TABLE #DashTplSeed;
GO

/* ==================== 8. 重建 Dash 复合索引（非筛选） ==================== */
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.Tbl_Dash_Indicator', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_Dash_Indicator_Code' AND object_id = OBJECT_ID(N'dbo.Tbl_Dash_Indicator'))
        CREATE UNIQUE INDEX [UQ_Tbl_Dash_Indicator_Code] ON [dbo].[Tbl_Dash_Indicator]([IndicatorCode], [IsDeleted]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tbl_Dash_Indicator_App_Status' AND object_id = OBJECT_ID(N'dbo.Tbl_Dash_Indicator'))
        CREATE INDEX [IX_Tbl_Dash_Indicator_App_Status] ON [dbo].[Tbl_Dash_Indicator]([IsDeleted], [AppCode], [BStatus], [DispSeq]);
END
IF OBJECT_ID(N'dbo.Tbl_Dash_PosTemplate', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_Dash_PosTemplate_Pos_Ind' AND object_id = OBJECT_ID(N'dbo.Tbl_Dash_PosTemplate'))
        CREATE UNIQUE INDEX [UQ_Tbl_Dash_PosTemplate_Pos_Ind] ON [dbo].[Tbl_Dash_PosTemplate]([PosID], [IndicatorID], [IsDeleted]);
END
IF OBJECT_ID(N'dbo.Tbl_Dash_UserCard', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Tbl_Dash_UserCard_UserPos_Ind' AND object_id = OBJECT_ID(N'dbo.Tbl_Dash_UserCard'))
        CREATE UNIQUE INDEX [UQ_Tbl_Dash_UserCard_UserPos_Ind] ON [dbo].[Tbl_Dash_UserCard]([UserID], [UserPosID], [IndicatorID], [IsDeleted]);
END

PRINT N'';
PRINT N'========== 25-Seed_Dashboard 完成 ==========';
PRINT N'预置 11 条指标，覆盖 ChartType 1~9（类型 1/9 各 2 条演示）';
PRINT N'CF_ADMIN 模板含全部 9 类图表；请 cfadmin 重新登录后访问 /EDashBoard/Index';
PRINT N'若已有个人卡片，请在仪表盘点击「重置为岗位默认」';
PRINT N'==================================================';
GO
"""

with open(OUT, "w", encoding="gbk", errors="replace") as f:
    f.write(CONTENT)
print("Wrote", OUT)
