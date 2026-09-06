using FamilyTree.Models;

namespace FamilyTree.Services;

/// <summary>
/// 登录失败计数与锁定。MVC（<c>AccountController</c>）与 API（<c>FtApiAuthService</c>）两条路径共用同一份逻辑：
/// 原先只有 MVC 路径会累加 <c>PwdErrorCount</c> / 置 <c>IsLocked</c>，API 路径完全绕过锁定，可无限次猜测口令。
/// 只改实体，落库由调用方的 <c>SaveChangesAsync</c> 完成。
/// </summary>
public static class LoginAttempts
{
    /// <summary>口令校验失败：累加错误次数，达到上限即锁定账号。</summary>
    public static void MarkFailure(EUser user)
    {
        var now = DateTime.Now;
        user.PwdErrorCount++;
        user.LastPwdErrorTime = now;
        user.AmendDate = now;
        if (user.PwdErrorCount >= user.MaxPwdErrorCount)
            user.IsLocked = true;
    }

    /// <summary>登录成功：清零错误计数并记录本次登录。</summary>
    public static void MarkSuccess(EUser user)
    {
        var now = DateTime.Now;
        user.PwdErrorCount = 0;
        user.LastPwdErrorTime = null;
        user.LoginCount++;
        user.LastLoginTime = now;
        user.AmendDate = now;
    }

    /// <summary>账号当前是否可用于登录（未删、未停用、未锁定、未超最大登录次数）。</summary>
    public static bool IsUsable(EUser user) =>
        !user.IsDeleted && user.IsEnabled && !user.IsLocked && user.LoginCount < user.MaxLoginCount;
}
