using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Helpers;

public static class FtApiJson
{
    public static IActionResult Ok(object? data = null, string? message = null) =>
        new JsonResult(new { ok = true, message, data });

    public static IActionResult Fail(string message, int status = 400) =>
        new JsonResult(new { ok = false, message, data = (object?)null }) { StatusCode = status };
}
