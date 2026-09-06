using FamilyTree.Helpers;
using FamilyTree.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyTree.Filters;

public sealed class FtSchemaFilter : IAsyncActionFilter
{
    private const string TableName = "FamilyTree_Person";
    private static readonly HashSet<string> FtControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "FtPerson", "FtPersonCreate", "FtPersonDraft", "FtPersonLink", "FtLinkAudit", "FtConflict",
        "FtTree", "FtMainTree", "FtPersonMarry", "FtMyProfile", "FtOpLog", "FtBatchMatch",
        "FtBranchAdmin", "FtBranchApply", "FtExport", "FtGenerationWord", "Member", "FtCertify", "FtPeer",
        "FtAuthApi", "FtProfileApi", "FtDraftApi", "FtTreeApi", "FtMessageApi", "FtBranchApplyApi", "FtPeerApi"
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor is not Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor cad
            || !FtControllers.Contains(cad.ControllerName))
        {
            await next();
            return;
        }

        if (string.Equals(cad.ControllerName, "Member", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(cad.ActionName, "Register", StringComparison.OrdinalIgnoreCase)
                || string.Equals(cad.ActionName, "Join", StringComparison.OrdinalIgnoreCase)))
        {
            await next();
            return;
        }

        if (string.Equals(cad.ControllerName, "FtAuthApi", StringComparison.OrdinalIgnoreCase)
            && string.Equals(cad.ActionName, "Config", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<FrameworkDbContext>();
        var exists = await DatabaseSchemaHelper.TableExistsAsync(db, TableName, context.HttpContext.RequestAborted);
        if (exists)
        {
            if (cad.ControllerName.EndsWith("Api", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(cad.ActionName, "Config", StringComparison.OrdinalIgnoreCase))
            {
                var tokenOk = await DatabaseSchemaHelper.TableExistsAsync(db, "FamilyTree_ApiToken", context.HttpContext.RequestAborted);
                if (!tokenOk)
                {
                    context.Result = new JsonResult(new { ok = false, message = "请补执行 scripts/29-CreateTbl_FamilyTree_Core.sql（含 FamilyTree_ApiToken 表）。" })
                    {
                        StatusCode = 503
                    };
                    return;
                }
            }
            await next();
            return;
        }

        if (cad.ControllerName.EndsWith("Api", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new JsonResult(new { ok = false, message = "族谱数据表尚未创建，请先执行 scripts/29-CreateTbl_FamilyTree_Core.sql。" })
            {
                StatusCode = 503
            };
            return;
        }

        context.Result = new ViewResult { ViewName = "~/Views/Shared/FtSchemaNotReady.cshtml" };
    }
}
