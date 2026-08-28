using FamilyTree.Configuration;
using FamilyTree.Filters;
using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Services;
using FamilyTree.Services.Import;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<EPrincipalPermissionFilter>();
    options.Filters.Add<HrLeaveSchemaFilter>();
    options.Filters.Add<FtSchemaFilter>();
});

builder.Services.Configure<FrameworkRbacOptions>(builder.Configuration.GetSection(FrameworkRbacOptions.SectionName));
builder.Services.Configure<EventFlowOptions>(builder.Configuration.GetSection(EventFlowOptions.SectionName));
builder.Services.AddHttpClient("EventFlowCallApi");
builder.Services.AddHttpClient("WeChat", c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 3 * 1024 * 1024);

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
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "FamilyTree.Auth";
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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

var app = builder.Build();

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

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (FrameworkDbContext db, CancellationToken ct) =>
{
    try
    {
        var ok = await db.Database.CanConnectAsync(ct);
        if (!ok)
            return Results.Json(new { status = "Unhealthy", database = "CannotConnect" }, statusCode: StatusCodes.Status503ServiceUnavailable);

        var checks = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["Tbl_E_HrLeaveRequest"] = await DatabaseSchemaHelper.TableExistsAsync(db, "Tbl_E_HrLeaveRequest", ct),
            ["Tbl_Dash_Indicator"] = await DatabaseSchemaHelper.TableExistsAsync(db, "Tbl_Dash_Indicator", ct),
            ["FamilyTree_Person"] = await DatabaseSchemaHelper.TableExistsAsync(db, "FamilyTree_Person", ct),
            ["FamilyTree_ApiToken"] = await DatabaseSchemaHelper.TableExistsAsync(db, "FamilyTree_ApiToken", ct)
        };
        var schemaOk = checks.Values.All(x => x);
        return Results.Ok(new
        {
            status = schemaOk ? "Healthy" : "Degraded",
            database = "Connected",
            tables = checks
        });
    }
    catch (Exception ex)
    {
        return Results.Json(
            new { status = "Unhealthy", error = ex.Message },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
