# EFrame 管理框架（CFramework.Web）

本目录为 **EFrame 新架构** 的 ASP.NET Core 8 MVC 应用，与上级 `docs/`、`scripts/` 配套使用。

## 1. 首次配置

```powershell
cd d:\work\NewSys\EFrame\prg
copy appsettings.Development.json.example appsettings.Development.json
```

编辑 `appsettings.Development.json`（可选，仅覆盖本机端口与日志）：

- **EventFlow:PublicBaseUrl** → 与 `dotnet run` 端口一致（默认 `http://localhost:5070`）

> 数据库连接串在 **`appsettings.json`**，开发与 IIS 生产共用；改库只改这一处。

## 2. 数据库（空库顺序）

**目标库：SQL Server 2008（兼容级别 100）**。`appsettings.json` 中 `Database:CompatibilityLevel` 默认为 **100**，与 `scripts\10-EFrame.sql` 一致；若使用更高版本 SQL Server 可改为 110/120 等。

在 SSMS 对目标库依次执行：

1. `..\scripts\10-EFrame.sql`（或 `..\docs\EFrame_CreateTables.sql` + `..\docs\EFrame_v2_supplement.sql`）
2. `..\scripts\11-CreateTbl_Dash_All.sql`（仪表盘，可选）
3. **框架种子（推荐，按序）**  
   - `..\scripts\20-Seed_Foundation.sql` → `21` → `22` → `23` → `24` → `25-Seed_Dashboard.sql`（可选）  
   - 说明见 `..\scripts\20-Seed_README.sql`  
   - 账号：`cfadmin` / `frameop` / `testuser` / `hrstaff` / `hrmgr`，密码均为 **123456**
4. 旧版一键种子仍可用：`..\scripts\old\SeedTestData_Complete.sql`（SQLCMD）
5. 若登录异常：`..\scripts\Patch_ResetTestUsers_Login.sql`
6. 待办页报「列名 ClaimTime 无效」等：`..\scripts\Patch_Tbl_E_TodoTask_ModuleC.sql`
7. 权限诊断：`..\scripts\Diag_Login_And_Permission_Full.sql`

## 3. 运行与发布

### 本机开发（无需 web.config）


```powershell
dotnet build
dotnet run
```

| 地址 | 说明 |
|------|------|
| 控制台显示的 URL（默认 http://localhost:5070） | 登录页 |
| `/health` | 数据库连通探活 |

### 发布到 IIS（需要 web.config）

```powershell
dotnet publish -c Release -o D:\website\EFrame\pub
# 或一键（含 Views 同步到 source）：..\scripts\Deploy_Publish_To_IIS.ps1
```

编辑页列表回传已改为 `@Html.PolistReturnHidden(...)`（`Helpers/PolistReturnHtmlExtensions.cs`），**不再依赖** `_PolistReturnHidden.cshtml`。若仍报该 partial 找不到，说明运行的仍是旧 DLL/旧 Views，请务必从 **prg** 执行 `dotnet publish` 到 `pub` 并重启 IIS；若从 `source` 编译，请把 prg 下 `Views` 与 `Helpers` 同步到 source 后重新生成。

发布目录会包含自动生成的 `web.config`；仓库内 `prg\web.config` 为模板（入口 `CFramework.Web.dll`）。IIS 站点需安装 **.NET 8 Hosting Bundle**。

发布后确认 **`pub\appsettings.json`** 含 `ConnectionStrings:DefaultConnection`（与源码 `prg\appsettings.json` 一致）。IIS 生产环境会直接读该文件。

对外站点可将 `EventFlow:PublicBaseUrl` 改为实际域名（如 `http://hc.easydo88.cn`），可写在 `appsettings.json` 或单独 `appsettings.Production.json` 覆盖。

**测试账号**（种子默认）：

| 登录名 | 密码 |
|--------|------|
| cfadmin | 123456 |
| frameop | 123456 |
| testuser | 123456 |

种子密码 `123456` 对应 `PwdHash`：`49ba59abbe56e057`（首次登录成功后可升级为 PBKDF2）。

## 4. 相关文档

- **[框架系统使用说明书.md](../docs/框架系统使用说明书.md)**（从零登录、新事件/订阅/流转配置，推荐实施与用户培训）
- [环境搭建_v2.md](../docs/环境搭建_v2.md)
- [联调与验收.md](../docs/联调与验收.md)
- [新架构实施清单.md](../docs/新架构实施清单.md)
