using FamilyTree.Helpers;
using FamilyTree.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FamilyTree.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class FtApiAllowAnonymousAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class FtApiAuthorizeAttribute : TypeFilterAttribute
{
    public FtApiAuthorizeAttribute() : base(typeof(FtApiAuthFilter))
    {
    }
}

public sealed class FtApiAuthFilter : IAsyncActionFilter
{
    private readonly FtApiAuthService _auth;

    public FtApiAuthFilter(FtApiAuthService auth) => _auth = auth;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<FtApiAllowAnonymousAttribute>().Any())
        {
            await next();
            return;
        }

        var header = context.HttpContext.Request.Headers.Authorization.ToString();
        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : (context.HttpContext.Request.Query["access_token"].ToString() ?? "").Trim();
        var userId = await _auth.ValidateTokenAsync(token, context.HttpContext.RequestAborted);
        if (userId == null)
        {
            context.Result = new JsonResult(new { ok = false, message = "未登录或令牌已失效。" })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        context.HttpContext.Items[FtApiAuthService.HttpUserIdKey] = userId.Value;
        await next();
    }
}

public static class FtApiHttp
{
    public static int UserId(HttpContext http) =>
        http.Items.TryGetValue(FtApiAuthService.HttpUserIdKey, out var v) && v is int id ? id : 0;

    public static string Operator(HttpContext http)
    {
        var uid = UserId(http);
        return uid > 0 ? FtText.ClipReq("FTU" + uid, 30) : "FT-API";
    }
}
