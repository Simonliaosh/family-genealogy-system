# 家族族谱 · 微信小程序

用微信开发者工具打开本目录 `miniprogram/`。

## 配置

1. 后端 `source/appsettings.json` 的 `FamilyTree:WeChat`：
   - 有正式 AppId 后填写 `AppId`、`AppSecret`，`PublicBaseUrl` 填 HTTPS 域名。
   - **现在没有小程序账号**：保持 AppId 为空，`AllowDevMock` 为 `true`，登录页「微信登录」会用本机模拟 OpenID。
2. 把 `utils/config.js` 的 `apiBase` 改成后端地址。开发者工具可用 `http://127.0.0.1:5070`；真机请改成电脑局域网 IP，并在工具里关闭「不校验合法域名」。
3. `project.config.json` 的 `appid` 换成你的小程序 AppId（没有账号时可用 `touristappid`）。

## 能力

| 页 | 调用 | 说明 |
|----|------|------|
| 登录 | `/api/FtAuth/*` | 微信 code2session 或身份证；首次注册 |
| 录入人物 | `/api/FtDraft` | 本人/父母/兄弟/子女；完成才写人物表 |
| 族谱 | `/api/FtTree` | 下延 / 上溯 / 左右；主谱或个人小树 |
| 档案 | `/api/FtProfile` | 隐私开关、照片上传 |
| 申请岗 | `/api/FtBranchApply` | 申请成为分支管理员 |
| 消息 | `/api/FtMessage` | 待办 + 链入结果 |

向上加祖先、纳入主谱、解链仍只在管理端。匹配与解锁不在小程序另写一套。

## 建表

已有库需补执行 `docs/04-数据结构.sql` 中的 `FamilyTree_ApiToken` 段（脚本可重复执行）。
