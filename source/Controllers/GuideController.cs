using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

/// <summary>
/// 实施指引页。原先是 wwwroot 下的静态页，任何人免登录即可读到默认凭据和管理入口清单，
/// 现改为登录后可见的视图。
/// </summary>
[Authorize]
public class GuideController : Controller
{
    [HttpGet]
    public IActionResult ImplementFlow() => View();
}
