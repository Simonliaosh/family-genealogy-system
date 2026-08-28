# -*- coding: utf-8 -*-
"""生成 11-CreateTbl_Dash_All.sql（ANSI/GBK），与 EFrame 建表脚本风格一致。"""
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "11-CreateTbl_Dash_All.sql")

CONTENT = r"""/*
================================================================================
  EFrame 仪表盘模块：Tbl_Dash_* 六张表（建表 + 索引 + 外键 + MS_Description）
================================================================================
  执行顺序：在 docs/EFrame_CreateTables.sql 之后执行本脚本（幂等）。
  配套种子：scripts/25-Seed_Dashboard.sql
  编码：ANSI (GBK)
  说明：唯一索引采用 (业务键, IsDeleted) 复合键，非筛选索引，兼容 ANSI_PADDING OFF
================================================================================
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('tempdb..#SetDesc') IS NOT NULL DROP PROCEDURE #SetDesc;
GO
CREATE PROCEDURE #SetDesc @TableName SYSNAME, @ColumnName SYSNAME = NULL, @Description NVARCHAR(4000) AS
BEGIN SET NOCOUNT ON;
    IF @ColumnName IS NULL BEGIN
        IF EXISTS (SELECT 1 FROM sys.extended_properties ep JOIN sys.tables t ON ep.major_id=t.object_id JOIN sys.schemas s ON t.schema_id=s.schema_id
            WHERE ep.name=N'MS_Description' AND s.name=N'dbo' AND t.name=@TableName AND ep.minor_id=0)
            EXEC sys.sp_updateextendedproperty @name=N'MS_Description',@value=@Description,@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=@TableName;
        ELSE EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=@Description,@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=@TableName;
    END ELSE BEGIN
        IF EXISTS (SELECT 1 FROM sys.extended_properties ep JOIN sys.tables t ON ep.major_id=t.object_id JOIN sys.schemas s ON t.schema_id=s.schema_id
            JOIN sys.columns c ON c.object_id=t.object_id AND c.column_id=ep.minor_id
            WHERE ep.name=N'MS_Description' AND s.name=N'dbo' AND t.name=@TableName AND c.name=@ColumnName)
            EXEC sys.sp_updateextendedproperty @name=N'MS_Description',@value=@Description,@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=@TableName,@level2type=N'COLUMN',@level2name=@ColumnName;
        ELSE EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=@Description,@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=@TableName,@level2type=N'COLUMN',@level2name=@ColumnName;
    END
END;
GO

/* ---- 1. Tbl_Dash_Indicator 业务指标库 ---- */
IF OBJECT_ID(N'dbo.Tbl_Dash_Indicator', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Tbl_Dash_Indicator] (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [IndicatorCode] VARCHAR(50) NOT NULL,
        [IndicatorName] NVARCHAR(100) NOT NULL,
        [AppCode] VARCHAR(50) NOT NULL CONSTRAINT DF_Tbl_Dash_Indicator_AppCode DEFAULT ('FRAME'),
        [ChartType] TINYINT NOT NULL,
        [DataSource] NVARCHAR(100) NOT NULL,
        [CalcRule] NVARCHAR(MAX) NOT NULL,
        [DefaultTimeScope] TINYINT NOT NULL CONSTRAINT DF_Tbl_Dash_Indicator_DefaultTimeScope DEFAULT (3),
        [IsLockCalc] BIT NOT NULL CONSTRAINT DF_Tbl_Dash_Indicator_IsLockCalc DEFAULT (1),
        [DispSeq] INT NOT NULL CONSTRAINT DF_Tbl_Dash_Indicator_DispSeq DEFAULT (99),
        [Remark] NVARCHAR(500) NULL,
        [BStatus] VARCHAR(20) NOT NULL CONSTRAINT DF_Tbl_Dash_Indicator_BStatus DEFAULT ('1'),
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_Tbl_Dash_Indicator_IsDeleted DEFAULT (0),
        [CreateDate] DATETIME NOT NULL CONSTRAINT DF_Tbl_Dash_Indicator_CreateDate DEFAULT (GETDATE()),
        [CreateUserID] INT NULL,
        [AmendDate] DATETIME NOT NULL CONSTRAINT DF_Tbl_Dash_Indicator_AmendDate DEFAULT (GETDATE()),
        [AmendUserID] INT NULL,
        [Operator] VARCHAR(30) NULL,
        [RowVersion] ROWVERSION,
        CONSTRAINT [PK_Tbl_Dash_Indicator] PRIMARY KEY CLUSTERED ([DataID])
    );
    PRINT N'已创建 Tbl_Dash_Indicator';
END
GO
EXEC #SetDesc N'Tbl_Dash_Indicator', NULL, N'业务指标库：管理员维护的统计/列表业务定义';
EXEC #SetDesc N'Tbl_Dash_Indicator', N'DataID', N'主键';
EXEC #SetDesc N'Tbl_Dash_Indicator', N'IndicatorCode', N'指标编码（唯一）';
EXEC #SetDesc N'Tbl_Dash_Indicator', N'IndicatorName', N'指标名称';
EXEC #SetDesc N'Tbl_Dash_Indicator', N'AppCode', N'所属应用模块，默认 FRAME，关联 Tbl_E_AppModule.AppCode';
EXEC #SetDesc N'Tbl_Dash_Indicator', N'ChartType', N'图表类型枚举 1~9';
EXEC #SetDesc N'Tbl_Dash_Indicator', N'DataSource', N'数据来源表或视图说明';
EXEC #SetDesc N'Tbl_Dash_Indicator', N'CalcRule', N'JSON：sqlText/globalLink/itemLinkConfig/filterDefault/menuList';
EXEC #SetDesc N'Tbl_Dash_Indicator', N'DefaultTimeScope', N'默认时间范围 1今日 2本周 3本月 4本年';
EXEC #SetDesc N'Tbl_Dash_Indicator', N'IsLockCalc', N'是否锁定计算规则';
EXEC #SetDesc N'Tbl_Dash_Indicator', N'DispSeq', N'显示顺序';
EXEC #SetDesc N'Tbl_Dash_Indicator', N'Remark', N'备注';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UQ_Tbl_Dash_Indicator_Code' AND object_id=OBJECT_ID(N'dbo.Tbl_Dash_Indicator'))
    CREATE UNIQUE INDEX [UQ_Tbl_Dash_Indicator_Code] ON [dbo].[Tbl_Dash_Indicator]([IndicatorCode],[IsDeleted]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Tbl_Dash_Indicator_App_Status' AND object_id=OBJECT_ID(N'dbo.Tbl_Dash_Indicator'))
    CREATE INDEX [IX_Tbl_Dash_Indicator_App_Status] ON [dbo].[Tbl_Dash_Indicator]([IsDeleted],[AppCode],[BStatus],[DispSeq]);
GO

/* ---- 2. Tbl_Dash_PosTemplate 岗位标准模板 ---- */
IF OBJECT_ID(N'dbo.Tbl_Dash_PosTemplate', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Tbl_Dash_PosTemplate] (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [PosID] INT NOT NULL,
        [IndicatorID] INT NOT NULL,
        [LayoutRow] INT NOT NULL,
        [LayoutCol] INT NOT NULL,
        [ColSpan] TINYINT NOT NULL CONSTRAINT DF_Tbl_Dash_PosTemplate_ColSpan DEFAULT (2),
        [IsLock] BIT NOT NULL CONSTRAINT DF_Tbl_Dash_PosTemplate_IsLock DEFAULT (0),
        [DefaultFilterJson] NVARCHAR(MAX) NULL,
        [CardTitle] NVARCHAR(100) NOT NULL,
        [DispSeq] INT NOT NULL CONSTRAINT DF_Tbl_Dash_PosTemplate_DispSeq DEFAULT (99),
        [BStatus] VARCHAR(20) NOT NULL CONSTRAINT DF_Tbl_Dash_PosTemplate_BStatus DEFAULT ('1'),
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_Tbl_Dash_PosTemplate_IsDeleted DEFAULT (0),
        [CreateDate] DATETIME NOT NULL CONSTRAINT DF_Tbl_Dash_PosTemplate_CreateDate DEFAULT (GETDATE()),
        [CreateUserID] INT NULL,
        [AmendDate] DATETIME NOT NULL CONSTRAINT DF_Tbl_Dash_PosTemplate_AmendDate DEFAULT (GETDATE()),
        [AmendUserID] INT NULL,
        [Operator] VARCHAR(30) NULL,
        [RowVersion] ROWVERSION,
        CONSTRAINT [PK_Tbl_Dash_PosTemplate] PRIMARY KEY CLUSTERED ([DataID])
    );
    PRINT N'已创建 Tbl_Dash_PosTemplate';
END
GO
EXEC #SetDesc N'Tbl_Dash_PosTemplate', NULL, N'岗位标准模板：按 PosID 定义默认卡片布局（行/列/跨度/标题/锁定/默认筛选）';
EXEC #SetDesc N'Tbl_Dash_PosTemplate', N'PosID', N'岗位 ID，关联 Tbl_E_Position.DataID';
EXEC #SetDesc N'Tbl_Dash_PosTemplate', N'IndicatorID', N'业务指标 ID，关联 Tbl_Dash_Indicator.DataID';
EXEC #SetDesc N'Tbl_Dash_PosTemplate', N'LayoutRow', N'布局行号（4 列网格）';
EXEC #SetDesc N'Tbl_Dash_PosTemplate', N'LayoutCol', N'布局列号 1~4';
EXEC #SetDesc N'Tbl_Dash_PosTemplate', N'ColSpan', N'列跨度 1~4';
EXEC #SetDesc N'Tbl_Dash_PosTemplate', N'IsLock', N'是否锁定（用户不可删除/隐藏）';
EXEC #SetDesc N'Tbl_Dash_PosTemplate', N'CardTitle', N'卡片标题';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UQ_Tbl_Dash_PosTemplate_Pos_Ind' AND object_id=OBJECT_ID(N'dbo.Tbl_Dash_PosTemplate'))
    CREATE UNIQUE INDEX [UQ_Tbl_Dash_PosTemplate_Pos_Ind] ON [dbo].[Tbl_Dash_PosTemplate]([PosID],[IndicatorID],[IsDeleted]);
GO

/* ---- 3. Tbl_Dash_UserSetting 用户仪表盘偏好 ---- */
IF OBJECT_ID(N'dbo.Tbl_Dash_UserSetting', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Tbl_Dash_UserSetting] (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [UserID] INT NOT NULL,
        [CurrentUserPosID] INT NOT NULL,
        [GlobalFilterJson] NVARCHAR(MAX) NULL,
        [CreateDate] DATETIME NOT NULL CONSTRAINT DF_Tbl_Dash_UserSetting_CreateDate DEFAULT (GETDATE()),
        [AmendDate] DATETIME NOT NULL CONSTRAINT DF_Tbl_Dash_UserSetting_AmendDate DEFAULT (GETDATE()),
        [Operator] VARCHAR(30) NULL,
        CONSTRAINT [PK_Tbl_Dash_UserSetting] PRIMARY KEY CLUSTERED ([DataID])
    );
    PRINT N'已创建 Tbl_Dash_UserSetting';
END
GO
EXEC #SetDesc N'Tbl_Dash_UserSetting', NULL, N'用户仪表盘偏好：CurrentUserPosID 指向 Tbl_E_UserPosition.DataID';
EXEC #SetDesc N'Tbl_Dash_UserSetting', N'UserID', N'用户 ID，关联 Tbl_E_Users.DataID';
EXEC #SetDesc N'Tbl_Dash_UserSetting', N'CurrentUserPosID', N'当前生效任岗 ID';
EXEC #SetDesc N'Tbl_Dash_UserSetting', N'GlobalFilterJson', N'全局筛选 JSON（时间/部门等）';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UQ_Tbl_Dash_UserSetting_User' AND object_id=OBJECT_ID(N'dbo.Tbl_Dash_UserSetting'))
    CREATE UNIQUE INDEX [UQ_Tbl_Dash_UserSetting_User] ON [dbo].[Tbl_Dash_UserSetting]([UserID]);
GO

/* ---- 4. Tbl_Dash_UserCard 用户个性化卡片 ---- */
IF OBJECT_ID(N'dbo.Tbl_Dash_UserCard', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Tbl_Dash_UserCard] (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [UserID] INT NOT NULL,
        [UserPosID] INT NOT NULL,
        [PosID] INT NOT NULL,
        [DeptID] INT NOT NULL,
        [IndicatorID] INT NOT NULL,
        [LayoutRow] INT NOT NULL,
        [LayoutCol] INT NOT NULL,
        [ColSpan] TINYINT NOT NULL CONSTRAINT DF_Tbl_Dash_UserCard_ColSpan DEFAULT (2),
        [IsLock] BIT NOT NULL CONSTRAINT DF_Tbl_Dash_UserCard_IsLock DEFAULT (0),
        [UserFilterJson] NVARCHAR(MAX) NULL,
        [IsHide] BIT NOT NULL CONSTRAINT DF_Tbl_Dash_UserCard_IsHide DEFAULT (0),
        [CardTitle] NVARCHAR(100) NOT NULL,
        [BStatus] VARCHAR(20) NOT NULL CONSTRAINT DF_Tbl_Dash_UserCard_BStatus DEFAULT ('1'),
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_Tbl_Dash_UserCard_IsDeleted DEFAULT (0),
        [CreateDate] DATETIME NOT NULL CONSTRAINT DF_Tbl_Dash_UserCard_CreateDate DEFAULT (GETDATE()),
        [AmendDate] DATETIME NOT NULL CONSTRAINT DF_Tbl_Dash_UserCard_AmendDate DEFAULT (GETDATE()),
        [Operator] VARCHAR(30) NULL,
        CONSTRAINT [PK_Tbl_Dash_UserCard] PRIMARY KEY CLUSTERED ([DataID])
    );
    PRINT N'已创建 Tbl_Dash_UserCard';
END
GO
EXEC #SetDesc N'Tbl_Dash_UserCard', NULL, N'用户个性化卡片：按 UserPosID 隔离，首页渲染数据源';
EXEC #SetDesc N'Tbl_Dash_UserCard', N'UserPosID', N'任岗 ID，关联 Tbl_E_UserPosition.DataID';
EXEC #SetDesc N'Tbl_Dash_UserCard', N'PosID', N'岗位 ID（冗余便于查询）';
EXEC #SetDesc N'Tbl_Dash_UserCard', N'DeptID', N'部门 ID（冗余便于查询）';
EXEC #SetDesc N'Tbl_Dash_UserCard', N'IndicatorID', N'业务指标 ID';
EXEC #SetDesc N'Tbl_Dash_UserCard', N'IsHide', N'是否隐藏';
EXEC #SetDesc N'Tbl_Dash_UserCard', N'CardTitle', N'卡片标题';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UQ_Tbl_Dash_UserCard_UserPos_Ind' AND object_id=OBJECT_ID(N'dbo.Tbl_Dash_UserCard'))
    CREATE UNIQUE INDEX [UQ_Tbl_Dash_UserCard_UserPos_Ind] ON [dbo].[Tbl_Dash_UserCard]([UserID],[UserPosID],[IndicatorID],[IsDeleted]);
GO

/* ---- 5. Tbl_Dash_PosIndicatorPerm 岗位指标授权 ---- */
IF OBJECT_ID(N'dbo.Tbl_Dash_PosIndicatorPerm', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Tbl_Dash_PosIndicatorPerm] (
        [DataID] INT IDENTITY(1,1) NOT NULL,
        [PosID] INT NOT NULL,
        [IndicatorID] INT NOT NULL,
        [BStatus] VARCHAR(20) NOT NULL CONSTRAINT DF_Tbl_Dash_PosIndicatorPerm_BStatus DEFAULT ('1'),
        [CreateDate] DATETIME NOT NULL CONSTRAINT DF_Tbl_Dash_PosIndicatorPerm_CreateDate DEFAULT (GETDATE()),
        [AmendDate] DATETIME NOT NULL CONSTRAINT DF_Tbl_Dash_PosIndicatorPerm_AmendDate DEFAULT (GETDATE()),
        [Operator] VARCHAR(30) NULL,
        CONSTRAINT [PK_Tbl_Dash_PosIndicatorPerm] PRIMARY KEY CLUSTERED ([DataID])
    );
    PRINT N'已创建 Tbl_Dash_PosIndicatorPerm';
END
GO
EXEC #SetDesc N'Tbl_Dash_PosIndicatorPerm', NULL, N'岗位指标授权：该岗位可见的业务指标';
EXEC #SetDesc N'Tbl_Dash_PosIndicatorPerm', N'PosID', N'岗位 ID';
EXEC #SetDesc N'Tbl_Dash_PosIndicatorPerm', N'IndicatorID', N'业务指标 ID';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UQ_Tbl_Dash_PosIndicatorPerm' AND object_id=OBJECT_ID(N'dbo.Tbl_Dash_PosIndicatorPerm'))
    CREATE UNIQUE INDEX [UQ_Tbl_Dash_PosIndicatorPerm] ON [dbo].[Tbl_Dash_PosIndicatorPerm]([PosID],[IndicatorID]);
GO

/* ---- 6. Tbl_Dash_UserOperLog 用户操作日志 ---- */
IF OBJECT_ID(N'dbo.Tbl_Dash_UserOperLog', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Tbl_Dash_UserOperLog] (
        [DataID] BIGINT IDENTITY(1,1) NOT NULL,
        [UserID] INT NOT NULL,
        [PosID] INT NULL,
        [OperType] VARCHAR(30) NOT NULL,
        [OperContent] NVARCHAR(500) NULL,
        [OperTime] DATETIME NOT NULL CONSTRAINT DF_Tbl_Dash_UserOperLog_OperTime DEFAULT (GETDATE()),
        [Operator] VARCHAR(30) NULL,
        CONSTRAINT [PK_Tbl_Dash_UserOperLog] PRIMARY KEY CLUSTERED ([DataID])
    );
    PRINT N'已创建 Tbl_Dash_UserOperLog';
END
GO
EXEC #SetDesc N'Tbl_Dash_UserOperLog', NULL, N'用户仪表盘操作日志';
EXEC #SetDesc N'Tbl_Dash_UserOperLog', N'OperType', N'操作类型：INIT/ADD_CARD/RESET/SWITCH_POS 等';
EXEC #SetDesc N'Tbl_Dash_UserOperLog', N'OperContent', N'操作内容摘要';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Tbl_Dash_UserOperLog_User_Time' AND object_id=OBJECT_ID(N'dbo.Tbl_Dash_UserOperLog'))
    CREATE INDEX [IX_Tbl_Dash_UserOperLog_User_Time] ON [dbo].[Tbl_Dash_UserOperLog]([UserID],[OperTime] DESC);
GO

/* ---- 外键（与 EFrame 逻辑关联，可选） ---- */
IF OBJECT_ID(N'dbo.Tbl_Dash_UserSetting',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Tbl_E_Users',N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Tbl_Dash_UserSetting_User')
    ALTER TABLE [dbo].[Tbl_Dash_UserSetting] WITH CHECK ADD CONSTRAINT [FK_Tbl_Dash_UserSetting_User]
        FOREIGN KEY ([UserID]) REFERENCES [dbo].[Tbl_E_Users]([DataID]);
GO
IF OBJECT_ID(N'dbo.Tbl_Dash_UserSetting',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Tbl_E_UserPosition',N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Tbl_Dash_UserSetting_UserPos')
    ALTER TABLE [dbo].[Tbl_Dash_UserSetting] WITH CHECK ADD CONSTRAINT [FK_Tbl_Dash_UserSetting_UserPos]
        FOREIGN KEY ([CurrentUserPosID]) REFERENCES [dbo].[Tbl_E_UserPosition]([DataID]);
GO
IF OBJECT_ID(N'dbo.Tbl_Dash_UserCard',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Tbl_E_UserPosition',N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Tbl_Dash_UserCard_UserPos')
    ALTER TABLE [dbo].[Tbl_Dash_UserCard] WITH CHECK ADD CONSTRAINT [FK_Tbl_Dash_UserCard_UserPos]
        FOREIGN KEY ([UserPosID]) REFERENCES [dbo].[Tbl_E_UserPosition]([DataID]);
GO
IF OBJECT_ID(N'dbo.Tbl_Dash_PosTemplate',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Tbl_E_Position',N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Tbl_Dash_PosTemplate_Pos')
    ALTER TABLE [dbo].[Tbl_Dash_PosTemplate] WITH CHECK ADD CONSTRAINT [FK_Tbl_Dash_PosTemplate_Pos]
        FOREIGN KEY ([PosID]) REFERENCES [dbo].[Tbl_E_Position]([DataID]);
GO
IF OBJECT_ID(N'dbo.Tbl_Dash_PosIndicatorPerm',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Tbl_E_Position',N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Tbl_Dash_PosIndicatorPerm_Pos')
    ALTER TABLE [dbo].[Tbl_Dash_PosIndicatorPerm] WITH CHECK ADD CONSTRAINT [FK_Tbl_Dash_PosIndicatorPerm_Pos]
        FOREIGN KEY ([PosID]) REFERENCES [dbo].[Tbl_E_Position]([DataID]);
GO

IF OBJECT_ID('tempdb..#SetDesc') IS NOT NULL DROP PROCEDURE #SetDesc;
GO
PRINT N'11-CreateTbl_Dash_All 完成。请继续执行 25-Seed_Dashboard.sql';
GO
"""

with open(OUT, "w", encoding="gbk", errors="replace") as f:
    f.write(CONTENT)
print("Wrote", OUT, "encoding=gbk")
