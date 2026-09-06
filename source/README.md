# source/ — ASP.NET Core 8 服务端

建库、配置、运行与发布见仓库根目录的 [README.md](../README.md)。这里只记服务端自身的结构。

## 目录

| 目录 | 内容 |
|---|---|
| `Controllers/` | MVC 控制器；`Controllers/Api/` 是供小程序调用的 `api/Ft*` |
| `Services/` | 业务服务。`Ft*` 是族谱域，`E*` / `Dash*` / `Hr*` 是企业框架脚手架 |
| `Models/` | EF 实体与 `FrameworkDbContext`；`Models/ViewModels/` 是表单与列表 VM |
| `Views/` | Razor 视图 |
| `Filters/` | 全局过滤器：RBAC 鉴权、表存在性探测、API 异常转 JSON |
| `Helpers/` | 文本归一化、分页、二维码、Claims 读取等 |
| `Configuration/` | 强类型配置项与限流策略名 |
| `wwwroot/` | 静态资源；`wwwroot/uploads/person/` 是用户上传的照片 |

## 权限模型

两层，缺一不可：

1. **资源位**（`FunctionLimit` 六位串，来自 RBAC 订阅）——决定按钮和菜单渲染，
   由 `EPrincipalPermissionFilter` 写进 `HttpContext.Items["PubFunctionLimit"]`。
2. **族 / 对象归属**——决定这条数据是不是你的。`FtClanService.UserSharesClanAsync`
   是统一入口，`CanViewAsync` / `CanCertifyAsync` / `CanSetMainAsync` / `ApproveAsync` 都走它。

第 2 层不能只放在视图里。视图层的判断只应影响显示，服务层必须自己再判一次。

## 口令

`PasswordHasher`：新口令一律 PBKDF2-SHA256 / 10 万轮 / 16 字节 CSPRNG 盐 / `FixedTimeEquals` 比较。

`MD5_16` 只保留一条兼容路径——历史行登录时校验一次，随即由
`ShouldUpgrade` + `ApplyStoredPassword` 透明升级为 PBKDF2。未知算法一律返回 false，
不再回落到 MD5。批量导入也一律写 PBKDF2。

## 时间

全代码使用 `DateTime.Now`（服务器本地时间）。夏令时回拨时会出现歧义时间戳；
迁移到 .NET 8 的 `TimeProvider` 是待办项。

## 已知待办

- 列表仍是「全表 `ToListAsync` 后在内存里过滤分页」。`FtOpLogService` 与 `FtConflictService`
  已改为库侧分页（`FtPaging.PageAsync`），其余 28 个 `GetIndexPageAsync` 待逐个迁移。
- `OnModelCreating` 只配了索引，没有配外键。加外键之前须先跑一次 `FtTreeHealthService`
  清掉已有的悬空父边。
- `Ft*` 控制器的 `ModelState` 检查仍不完整，`FtPersonDraftController` / `FtPersonLinkController`
  等仍未逐个补齐。
- 视图层有大量复制粘贴（分页工具条、内联 `<style>`、`onchange="this.form.submit()"`），
  收敛之前 CSP 无法去掉 `'unsafe-inline'`。
