using FamilyTree.Helpers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FamilyTree.Filters;

/// <summary>
/// <c>api/*</c> 下的未处理异常统一转成 <c>{ok,message,data}</c> 信封的 JSON。
/// 原先这些控制器只 catch <c>InvalidOperationException</c>，其余异常一路走到
/// <c>UseExceptionHandler("/Home/Error")</c>，于是小程序拿到一个 HTML 错误页并解析失败；
/// Development 下更是直接吐完整堆栈。
/// </summary>
public sealed class FtApiExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<FtApiExceptionFilter> _logger;
    private readonly IHostEnvironment _env;

    public FtApiExceptionFilter(ILogger<FtApiExceptionFilter> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        var path = context.HttpContext.Request.Path.Value ?? "";
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        _logger.LogError(context.Exception, "API 未处理异常: {Method} {Path}",
            context.HttpContext.Request.Method, path);

        // 生产环境只给一句话；异常细节留在服务端日志里
        var msg = _env.IsDevelopment()
            ? context.Exception.Message
            : "服务器处理请求时出错，请稍后重试。";
        context.Result = FtApiJson.Fail(msg, StatusCodes.Status500InternalServerError);
        context.ExceptionHandled = true;
        return Task.CompletedTask;
    }
}
