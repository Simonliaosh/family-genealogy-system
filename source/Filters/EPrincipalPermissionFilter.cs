using System.Net;
using FamilyTree.Configuration;
using FamilyTree.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace FamilyTree.Filters;

/// <summary>E 用户登录后，按岗位→职责→订阅→资源校验 MVC 动作并写入 PubFunctionLimit。</summary>
public sealed class EPrincipalPermissionFilter : IAsyncActionFilter
{
    private readonly FrameworkRbacOptions _opt;
    private readonly EPrincipalAccessService _eAccess;

    public EPrincipalPermissionFilter(IOptions<FrameworkRbacOptions> opt, EPrincipalAccessService eAccess)
    {
        _opt = opt.Value;
        _eAccess = eAccess;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            await next();
            return;
        }

        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        if (context.ActionDescriptor is not ControllerActionDescriptor cad)
        {
            await next();
            return;
        }

        var ctrl = cad.ControllerName;
        var act = cad.ActionName;

        if (string.Equals(
                user.FindFirst(FrameworkClaimTypes.AuthPrincipalKind)?.Value,
                FrameworkClaimTypes.KindEUsers,
                StringComparison.OrdinalIgnoreCase)
            && user.FindFirst(FrameworkClaimTypes.EUserId)?.Value is { Length: > 0 } eUserStr
            && int.TryParse(eUserStr, out var eUserId))
        {
            if (string.Equals(ctrl, "Account", StringComparison.OrdinalIgnoreCase) ||
                _opt.SkipPermissionControllers.Contains(ctrl))
            {
                await next();
                return;
            }

            var auth = await _eAccess.TryAuthorizeMvcAsync(
                eUserId,
                ctrl,
                act,
                context.HttpContext.RequestAborted,
                context.HttpContext.Request.Path.Value);
            if (auth != null)
            {
                var (fnLimit, busLimit) = auth.Value;
                context.HttpContext.Items["PubFunctionLimit"] = fnLimit ?? "";
                context.HttpContext.Items["MaintainLimit"] = busLimit ?? "";
                await next();
                return;
            }

            context.Result = new ContentResult
            {
                ContentType = "text/html; charset=utf-8",
                StatusCode = (int)HttpStatusCode.Forbidden,
                Content = """
                    <!DOCTYPE html><html><head><meta charset="utf-8"/><title>提示</title></head>
                    <body style="font-family:微软雅黑;text-align:center;padding:40px;">
                    <h2>温馨提示</h2>
                    <p>对不起，你未获得该功能的授权，请与相关负责人联系！</p>
                    </body></html>
                    """
            };
            return;
        }

        await next();
    }
}
