using FamilyTree.Models;
using FamilyTree.Services;
using Xunit;

namespace FamilyTree.Tests;

/// <summary>登录失败计数与锁定（P1-4）：MVC 与 API 两条路径共用这一份逻辑。</summary>
public class LoginAttemptsTests
{
    private static EUser NewUser(int maxErrors = 3) => new()
    {
        DataId = 1,
        LoginId = "u1",
        IsEnabled = true,
        MaxPwdErrorCount = maxErrors,
        MaxLoginCount = 9999
    };

    [Fact]
    public void MarkFailure_IncrementsCountAndRecordsTime()
    {
        var u = NewUser();
        LoginAttempts.MarkFailure(u);
        Assert.Equal(1, u.PwdErrorCount);
        Assert.NotNull(u.LastPwdErrorTime);
        Assert.False(u.IsLocked);
    }

    [Fact]
    public void MarkFailure_LocksAccountAtThreshold()
    {
        var u = NewUser(maxErrors: 3);
        LoginAttempts.MarkFailure(u);
        LoginAttempts.MarkFailure(u);
        Assert.False(u.IsLocked);
        LoginAttempts.MarkFailure(u);
        Assert.True(u.IsLocked);
    }

    [Fact]
    public void MarkSuccess_ClearsFailureStateAndCountsLogin()
    {
        var u = NewUser();
        LoginAttempts.MarkFailure(u);
        LoginAttempts.MarkSuccess(u);
        Assert.Equal(0, u.PwdErrorCount);
        Assert.Null(u.LastPwdErrorTime);
        Assert.Equal(1, u.LoginCount);
        Assert.NotNull(u.LastLoginTime);
    }

    [Fact]
    public void IsUsable_RejectsDisabledLockedDeletedAndOverLimit()
    {
        Assert.True(LoginAttempts.IsUsable(NewUser()));

        var disabled = NewUser(); disabled.IsEnabled = false;
        Assert.False(LoginAttempts.IsUsable(disabled));

        var locked = NewUser(); locked.IsLocked = true;
        Assert.False(LoginAttempts.IsUsable(locked));

        var deleted = NewUser(); deleted.IsDeleted = true;
        Assert.False(LoginAttempts.IsUsable(deleted));

        var over = NewUser(); over.MaxLoginCount = 5; over.LoginCount = 5;
        Assert.False(LoginAttempts.IsUsable(over));
    }
}
