using FamilyTree.Models.ViewModels;
using FamilyTree.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FamilyTree.ViewComponents;

public class MenuViewComponent : ViewComponent
{
    private readonly EPrincipalAccessService _eAccess;

    public MenuViewComponent(EPrincipalAccessService eAccess)
    {
        _eAccess = eAccess;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (UserClaimsPrincipal?.Identity?.IsAuthenticated == true
            && string.Equals(
                UserClaimsPrincipal.FindFirst(FrameworkClaimTypes.AuthPrincipalKind)?.Value,
                FrameworkClaimTypes.KindEUsers,
                StringComparison.OrdinalIgnoreCase)
            && UserClaimsPrincipal.FindFirst(FrameworkClaimTypes.EUserId)?.Value is { Length: > 0 } eUserStr
            && int.TryParse(eUserStr, out var eUserId))
        {
            var groups = await _eAccess.GetSidebarMenuAsync(eUserId, ViewContext.HttpContext.RequestAborted);
            return View(new MenuSidebarVm { MenuGroups = groups });
        }

        return View(new MenuSidebarVm { MenuGroups = new List<MenuGroupModel>() });
    }
}
