using System.Net;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FamilyTree.Helpers;

public static class PolistReturnHtmlExtensions
{
    /// <summary>列表页状态回传隐藏域（不依赖 _PolistReturnHidden 局部视图文件）。</summary>
    public static IHtmlContent PolistReturnHidden(this IHtmlHelper html, string? polistRt)
    {
        if (string.IsNullOrWhiteSpace(polistRt))
            return HtmlString.Empty;
        var enc = WebUtility.HtmlEncode(polistRt);
        return new HtmlString($"<input type=\"hidden\" name=\"polistRt\" value=\"{enc}\" />");
    }
}
