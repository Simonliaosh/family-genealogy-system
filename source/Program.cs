using FamilyTree.Configuration;
using FamilyTree.Filters;
using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Services;
using FamilyTree.Services.Import;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 服务注册是 60 多行手维护的 AddScoped 墙，漏一行或接出循环依赖都是运行时才炸。
// 打开容器校验：构造阶段就把「注册了但造不出来」和循环依赖变成启动失败。
builder.Host.UseDefaultServiceProvider(o =>
{
    o.ValidateScopes = true;
    o.ValidateOnBuild = true;
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<EPrincipalPermissionFilter>();
    options.Filters.Add<HrLeaveSchemaFilter>();
    options.Filters.Add<FtSchemaFilter>();
    options.Filters.Add<FtApiExceptionFilter>();
})
// TempData 默认走 CookieTempDataProvider——导入报告这类大文本会整份往返浏览器 Cookie，
// 里面曾经带着明文口令清单。改用 Session 存储，浏览器只拿到一个 session id。
.AddSessionStateTempDataProvider();

builder.Services.Configure<FrameworkRbacOptions>(builder.Configuration.GetSection(FrameworkRbacOptions.SectionName));
builder.Services.Configure<EventFlowOptions>(builder.Configuration.GetSection(EventFlowOptions.SectionName));
builder.Services.AddHttpClient("EventFlowCallApi");
builder.Services.AddHttpClient("WeChat", c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 3 * 1024 * 1024);

// 1.0 MB 的 echarts.min.js 原先是未压缩传输的
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes
        .Concat(new[] { "application/javascript", "text/css", "image/svg+xml", "application/json" });
});

var sqlCompatLevel = builder.Configuration.GetValue("Database:CompatibilityLevel", 100);
if (sqlCompatLevel < 100 || sqlCompatLevel > 160)
    throw new InvalidOperationException("Database:CompatibilityLevel 须在 100~160 之间（100=SQL Server 2008）。");

builder.Services.AddDbContext<FrameworkDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseCompatibilityLevel(sqlCompatLevel)));

var keysPath = Path.Combine(builder.Environment.ContentRootPath, "Keys");
if (!Directory.Exists(keysPath))
    Directory.CreateDirectory(keysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("FamilyTree")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        // SameAsRequest 意味着任何一次明文 HTTP 请求上认证 Cookie 都不带 Secure。
        // 本机开发（Development）保留 SameAsRequest 以便 http://localhost 调试，其余环境强制 Secure。
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "FamilyTree.Auth";
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // Session 里存着待登录用户 id 与微信 OAuth state，原先既无 SecurePolicy 也无 SameSite
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// 登录与匿名认证接口的限流。全站原先零限流，/api/FtAuth/IdCardLogin 可无限次爆破。
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Login, http =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy(RateLimitPolicies.AnonymousApi, http =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<EUsersService>();
builder.Services.AddScoped<EPrincipalAccessService>();
builder.Services.AddScoped<HomeWorkbenchService>();
builder.Services.AddScoped<EDepartmentService>();
builder.Services.AddScoped<EPositionService>();
builder.Services.AddScoped<EUserPositionService>();
builder.Services.AddScoped<EDutyService>();
builder.Services.AddScoped<EMenuGroupService>();
builder.Services.AddScoped<EResourceService>();
builder.Services.AddScoped<EPositionDutyService>();
builder.Services.AddScoped<EEventConfigService>();
builder.Services.AddScoped<ESubscriptionService>();
builder.Services.AddScoped<EManagerSubordinateService>();
builder.Services.AddScoped<EResourcePermissionService>();
builder.Services.AddScoped<EEventFlowRuleService>();
builder.Services.AddScoped<EEventLogService>();
builder.Services.AddScoped<ETodoTaskService>();
builder.Services.AddScoped<DictService>();
builder.Services.AddScoped<EDictQueryService>();
builder.Services.AddScoped<EDictTypeService>();
builder.Services.AddScoped<EDictItemService>();
builder.Services.AddScoped<EAppModuleQueryService>();
builder.Services.AddScoped<EAppModuleService>();
builder.Services.AddScoped<EEventInstanceQueryService>();
builder.Services.AddScoped<EventPublisherService>();
builder.Services.AddScoped<ELoginLogService>();
builder.Services.AddScoped<EUserHandoverService>();
builder.Services.AddScoped<EMemberService>();
builder.Services.AddScoped<HrLeaveService>();
builder.Services.AddScoped<HrHomeService>();
builder.Services.AddScoped<DashCalcRuleExecutor>();
builder.Services.AddScoped<DashIndicatorService>();
builder.Services.AddScoped<DashPosIndicatorPermService>();
builder.Services.AddScoped<DashPosTemplateService>();
builder.Services.AddScoped<DashBoardService>();
builder.Services.AddScoped<ProductionDataImportService>();
builder.Services.AddHttpClient("FtPeer", c => c.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<FamilyTreeOptions>(builder.Configuration.GetSection(FamilyTreeOptions.SectionName));
builder.Services.AddScoped<FtDutyAccess>();
builder.Services.AddScoped<FtOpLogService>();
builder.Services.AddScoped<FtMatchService>();
builder.Services.AddScoped<FtTreeHealthService>();
builder.Services.AddScoped<FtClanService>();
builder.Services.AddScoped<FtPersonService>();
builder.Services.AddScoped<FtPersonDraftService>();
builder.Services.AddScoped<FtPersonLinkService>();
builder.Services.AddScoped<FtPeerService>();
builder.Services.AddScoped<FtGenerationWordService>();
builder.Services.AddScoped<FtTreeService>();
builder.Services.AddScoped<FtPersonMarryService>();
builder.Services.AddScoped<FtAccountService>();
builder.Services.AddScoped<FtConflictService>();
builder.Services.AddScoped<FtBranchAdminService>();
builder.Services.AddScoped<FtBranchAdminApplyService>();
builder.Services.AddScoped<FtMyProfileService>();
builder.Services.AddScoped<FtPhotoService>();
builder.Services.AddScoped<FtApiAuthService>();
builder.Services.AddScoped<FtMemberCookieSignIn>();
builder.Services.AddScoped<FtApiAuthFilter>();
builder.Services.AddScoped<FtBatchMatchService>();
builder.Services.AddScoped<FtOfflineExportService>();
builder.Services.AddScoped<EPrincipalPermissionFilter>();
builder.Services.AddScoped<FtApiExceptionFilter>();

var app = builder.Build();

// AllowDevMock 开启即为完整认证绕过：提交受害者的 openid 就能以其身份登录。
// 生产环境不接受这个开关，启动即失败，而不是带着它悄悄跑起来。
if (!app.Environment.IsDevelopment())
{
    var ftOpt = app.Services.GetRequiredService<IOptions<FamilyTreeOptions>>().Value;
    if (ftOpt.WeChat.AllowDevMock)
        throw new InvalidOperationException(
            "FamilyTree:WeChat:AllowDevMock 只能在 Development 环境开启：它允许用任意 openid 冒名登录。");
    if (string.Equals(ftOpt.IdCardHashSalt, new FamilyTreeOptions().IdCardHashSalt, StringComparison.Ordinal))
        throw new InvalidOperationException(
            "FamilyTree:IdCardHashSalt 仍是默认值，请改成本部署专属的随机盐——全部身份证号都用它做哈希。");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

if (app.Configuration.GetValue("Hosting:EnableHttpsRedirection", !app.Environment.IsDevelopment()))
    app.UseHttpsRedirection();

// 安全响应头。wwwroot/uploads 下直接对外提供用户上传的照片，而上传校验只到扩展名一层，
// 没有 magic byte 校验——所以 nosniff 在这里不是可选项。
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "SAMEORIGIN";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["Content-Security-Policy"] =
        "default-src 'self'; " +
        "img-src 'self' data: blob:; " +
        // 视图层有 397 处内联 style、58 个 onchange、54 个 onclick，
        // 收紧到禁用 inline 需要先把这些搬进 site.css / site.js，属于后续工作。
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'";
    await next();
});

// 原先是裸 app.UseStaticFiles()：无 Cache-Control、无压缩。
// asp-append-version 已经给静态资源加了内容指纹，因此可以放心长缓存 + immutable。
app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var isVersioned = ctx.Context.Request.Query.ContainsKey("v");
        var isVendor = ctx.Context.Request.Path.StartsWithSegments("/lib");
        // 用户上传的照片按 id 分目录、文件名由服务端生成，可以缓存但不要设成 immutable
        var isUpload = ctx.Context.Request.Path.StartsWithSegments("/uploads");

        if (isUpload)
            ctx.Context.Response.Headers.CacheControl = "private, max-age=3600";
        else if (isVersioned || isVendor)
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        else
            ctx.Context.Response.Headers.CacheControl = "public, max-age=3600";

        // 上传目录同时开 nosniff（全局中间件已设，这里对静态文件再确认一次）
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    }
});
app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// 匿名探活。既不回显异常详情（SqlException.Message 会带出服务器名/库名/登录名），
// 也不向匿名调用者枚举内部表名——只回 Healthy / Degraded / Unhealthy。
app.MapGet("/health", async (FrameworkDbContext db, ILoggerFactory lf, CancellationToken ct) =>
{
    try
    {
        var ok = await db.Database.CanConnectAsync(ct);
        if (!ok)
            return Results.Json(new { status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);

        var required = new[] { "Tbl_E_HrLeaveRequest", "Tbl_Dash_Indicator", "FamilyTree_Person", "FamilyTree_ApiToken" };
        var missing = 0;
        foreach (var t in required)
            if (!await DatabaseSchemaHelper.TableExistsAsync(db, t, ct)) missing++;

        return Results.Ok(new { status = missing == 0 ? "Healthy" : "Degraded" });
    }
    catch (Exception ex)
    {
        lf.CreateLogger("Health").LogError(ex, "健康检查失败");
        return Results.Json(new { status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous().RequireRateLimiting(RateLimitPolicies.AnonymousApi);

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
