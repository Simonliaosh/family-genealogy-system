using FamilyTree.Helpers;
using FamilyTree.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyTree.Filters;

/// <summary>人事模块访问前检查 Tbl_E_HrLeaveRequest 是否已建表。</summary>
public sealed class HrLeaveSchemaFilter : IAsyncActionFilter
{
    private const string TableName = "Tbl_E_HrLeaveRequest";
    private static readonly HashSet<string> HrControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "HrLeave", "HrHome"
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor is not Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor cad
            || !HrControllers.Contains(cad.ControllerName))
        {
            await next();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<FrameworkDbContext>();
        var exists = await DatabaseSchemaHelper.TableExistsAsync(db, TableName, context.HttpContext.RequestAborted);
        if (exists)
        {
            await next();
            return;
        }

        context.Result = new ViewResult
        {
            ViewName = "~/Views/Shared/HrSchemaNotReady.cshtml"
        };
    }
}
