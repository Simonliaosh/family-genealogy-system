# 家族族谱 · 微信小程序

用微信开发者工具打开本目录 `miniprogram/`。

## 配置

**接口地址不用手改。** `utils/config.js` 按小程序运行环境（`develop` / `trial` / `release`）
自动选择：

| 环境 | 地址 |
|---|---|
| 开发版（开发者工具 / 本机） | `http://127.0.0.1:5070` |
| 体验版 | `https://staging.example.com` |
| 正式版 | `https://example.com` |

体验版与正式版的域名请按实际部署改这一个文件。微信生产环境要求 **HTTPS 且域名已备案**，
明文 `http://` 只在开发版可用。

真机预览时后端在你的电脑上，需要临时关闭域名校验：复制
`project.private.config.json.example` 为 `project.private.config.json`（已被忽略，不会进仓库）。
不要再把 `urlCheck: false` 提交进 `project.config.json`——那会对每个克隆者永久生效。

小程序 AppId 填在 `project.config.json` 的 `appid`（没有账号时用 `touristappid`），
对应的 `AppId` / `AppSecret` 填在后端 `source/appsettings.json` 的 `FamilyTree:WeChat`。
**密钥只存在于服务端**，本目录不含任何密钥。

## 建表

族谱表由 `scripts/29-CreateTbl_FamilyTree_Core.sql` 创建（含小程序登录用的 `FamilyTree_ApiToken`）。
执行顺序见仓库根目录的 [README](../README.md)。

## 能力

| 页 | 调用 | 说明 |
|----|------|------|
| 登录 | `/api/FtAuth/*` | 微信 code2session 或登录名/身份证；首次注册 |
| 录入人物 | `/api/FtDraft` | 本人/父母/兄弟/子女；点完成才写人物表 |
| 族谱 | `/api/FtTree` | 下延 / 上溯 / 左右；主谱或个人小树 |
| 档案 | `/api/FtProfile` | 隐私开关、照片上传 |
| 申请岗 | `/api/FtBranchApply` | 申请成为支链管理员 |
| 消息 | `/api/FtMessage` | 待办 + 链入结果 |

向上加祖先、纳入主谱、解链仍只在管理端。匹配与解锁不在小程序另写一套。

## 约定

**接口信封**：所有 `api/*` 返回 `{ ok, message, data }`；HTTP 状态码同时有意义
（401 未登录、429 触发限流、5xx 服务端错误）。

**网络层**（`utils/request.js`）：
- 每个请求 12 秒超时（上传 36 秒），`app.json` 里另有一层全局兜底。
- GET 幂等，网络类失败自动退避重试 2 次；POST 绝不重试。
- 错误带 `kind`（`NETWORK` / `AUTH` / `BUSINESS` / `SERVER`），页面据此决定是提示重试还是跳登录。
- 令牌连同本地过期时间一起存；`hasValidToken()` 让页面在进入前就能判断，而不是等一个 401。

**页面三态**（`utils/page.js`）：`loading` / `loadError` / 正常。
加载失败时页面显示失败原因和重试按钮，不再和「确实没有数据」混在一起。

**客户端校验**（`utils/validate.js`）：身份证走 GB 11643 校验位，登录名与口令按服务端同一套规则先验一遍。

## 隐私

`app.json` 里配了 `__usePrivacyCheck__` 与 `permission`。本应用收集真实姓名、身份证号、
微信号与照片，**提审前必须在小程序后台填写并发布《用户隐私保护指引》**，否则会被驳回。

`sitemap.json` 只放行登录落地页，个人档案与草稿编辑页不被索引。
