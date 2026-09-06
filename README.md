# family-genealogy-system

家族族谱登记系统。族人录入本人及亲属信息，经匹配与审批汇入主族谱，形成完整、可查阅、可导出的电子族谱。

包含三部分：

| 目录 | 内容 |
|---|---|
| `source/` | ASP.NET Core 8 MVC 服务端（Web 后台 + 供小程序调用的 `api/Ft*`） |
| `miniprogram/` | 微信小程序（族人端：录入草稿、看树、我的档案、待办） |
| `scripts/` | 手写编号 SQL 脚本（建库、建表、种子、运维），按编号顺序执行 |

设计上已经定下来的选择记在 [DECISIONS.md](DECISIONS.md)。

## 前置条件

- .NET 8 SDK
- SQL Server（脚本按兼容级别 100 编写，2008 R2 及以上均可；建议使用仍受支持的版本）
- `sqlcmd`，或打开了 **SQLCMD 模式** 的 SSMS —— 种子脚本用 sqlcmd 变量传入初始口令
- 微信开发者工具（仅调试小程序时需要）

## 建库

脚本严格按文件名编号顺序执行，全部幂等，可重复运行。每个脚本执行后会在
`dbo.SchemaScriptLog` 里登记一条记录，随时可以问一个数据库「你跑过哪些脚本」：

```sql
SELECT ScriptName, AppliedAt, RunCount FROM dbo.SchemaScriptLog ORDER BY ScriptName;
```

```bash
# 1) 框架基础库：建库 + 33 张框架表 + 索引 + 外键
sqlcmd -S <server> -i scripts/10-EFrame.sql

# 2) 看板表（可选）
sqlcmd -S <server> -i scripts/11-CreateTbl_Dash_All.sql

# 3) 框架种子。22 与 26 需要传入初始管理员口令——脚本内不写死任何口令，
#    不传该变量会直接报错退出。口令仅在账号首次插入时设置，重跑不会重置已有账号。
for f in scripts/2*.sql; do
  sqlcmd -S <server> -v AdminPassword="你的强口令" -i "$f"
done

# 4) 族谱核心表 + 索引 + rowversion
sqlcmd -S <server> -i scripts/29-CreateTbl_FamilyTree_Core.sql

# 5) 族谱种子与补丁
for f in scripts/3*.sql scripts/4*.sql scripts/50-*.sql; do
  sqlcmd -S <server> -v AdminPassword="你的强口令" -i "$f"
done
```

编号区间的含义：

| 区间 | 内容 |
|---|---|
| `10`–`11` | 建库与框架/看板建表 |
| `20`–`27` | 框架种子：组织、用户、菜单、事件、看板、可选的贸易公司演示数据 |
| `29` | 族谱核心 10 张表、索引、乐观并发列 |
| `31`–`47` | 族谱资源、权限、家族、联邦、认证码等增量补丁与种子 |
| `48`–`49` | 运维清理脚本（**会删数据**，开关走 sqlcmd 变量，默认全部最保守） |
| `50` | 族谱完整种子（含超管账号与全部菜单权限） |

> `48-Clean_DefaultClan_Data.sql` 会物理删除数据且无法撤销。执行前先看它打印的预览计数。

## 配置

复制 `source/appsettings.json.example` 为 `source/appsettings.json` 并填写。该文件被 `.gitignore` 忽略。

必须改的两项：

- **`ConnectionStrings:DefaultConnection`**
- **`FamilyTree:IdCardHashSalt`** —— 全部身份证号都用它做哈希。生产环境若仍是默认值，应用会拒绝启动。
  换盐会让已有哈希全部失配，请一次性定好。

其余键（微信 AppId/AppSecret、联邦对端白名单、看板自由 SQL 开关等）在示例文件里逐条有注释。

## 运行

```bash
dotnet build FamilyTree.sln
dotnet run --project source           # 默认 http://localhost:5070
dotnet test FamilyTree.sln            # 单元测试
```

| 地址 | 说明 |
|---|---|
| `/` | 登录页 |
| `/health` | 匿名探活，只回 Healthy / Degraded / Unhealthy |

## 小程序

用微信开发者工具打开 `miniprogram/`。接口地址由 `miniprogram/utils/config.js` 决定，
按 `envVersion`（开发版 / 体验版 / 正式版）自动切换，不需要手改文件。

正式版必须是已备案的 HTTPS 域名——微信生产环境不接受明文 HTTP。

## 发布

```bash
dotnet publish source -c Release -o <发布目录>
```

发布产物会自带 `web.config`。IIS 站点需安装 **.NET 8 Hosting Bundle**。

发布与备份流程必须**排除 `source/Keys/`**：那是 DataProtection 的密钥环，明文 XML，
拿到它就能为任意用户伪造认证 Cookie。

## 许可证

[MIT](LICENSE)。仓库内再分发的第三方前端组件保留各自的原始许可证，详见 LICENSE 末尾。
