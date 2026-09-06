using FamilyTree.Services;
using Xunit;

namespace FamilyTree.Tests;

/// <summary>口令哈希。重点是 P0-4：未知算法不得再回落到无盐 MD5_16。</summary>
public class PasswordHasherTests
{
    [Fact]
    public void HashPbkdf2_ProducesFourPartFormatWithRandomSalt()
    {
        var a = PasswordHasher.HashPbkdf2("hunter2");
        var b = PasswordHasher.HashPbkdf2("hunter2");
        Assert.StartsWith("PBKDF2|100000|", a);
        Assert.Equal(4, a.Split('|').Length);
        Assert.NotEqual(a, b);   // 每次都是新盐
    }

    [Fact]
    public void Verify_AcceptsCorrectPbkdf2Password()
    {
        var (hash, algo, _) = PasswordHasher.HashForStore("correct horse");
        Assert.True(PasswordHasher.Verify("correct horse", hash, algo));
        Assert.False(PasswordHasher.Verify("wrong horse", hash, algo));
    }

    [Fact]
    public void Verify_DetectsPbkdf2FromStoredPrefixWhenAlgoColumnIsMissing()
    {
        var hash = PasswordHasher.HashPbkdf2("abc123");
        Assert.True(PasswordHasher.Verify("abc123", hash, null));
        Assert.True(PasswordHasher.Verify("abc123", hash, ""));
    }

    [Fact]
    public void Verify_StillAcceptsMd5_16ForLegacyRows()
    {
        // 兼容路径必须保留：登录成功后 ShouldUpgrade 会把它升级为 PBKDF2
        var legacy = PasswordHasher.HashMd5_16("123456");
        Assert.Equal("49ba59abbe56e057", legacy);
        Assert.True(PasswordHasher.Verify("123456", legacy, PasswordHasher.AlgoMd5_16));
    }

    [Fact]
    public void Verify_RejectsUnknownAlgorithmInsteadOfFallingBackToMd5()
    {
        // 回归测试（P0-4）：原实现的兜底分支会在算法字段异常时用 MD5_16 校验，
        // 于是把一个 MD5_16 哈希标成任意算法名，仍然能用弱哈希登录进来。
        var md5 = PasswordHasher.HashMd5_16("123456");
        Assert.False(PasswordHasher.Verify("123456", md5, "SOMETHING_ELSE"));
        Assert.False(PasswordHasher.Verify("123456", md5, "BCRYPT"));
        Assert.False(PasswordHasher.Verify("123456", md5, "PBKDF2"));
    }

    [Fact]
    public void Verify_RejectsEmptyInput()
    {
        var (hash, algo, _) = PasswordHasher.HashForStore("x");
        Assert.False(PasswordHasher.Verify("", hash, algo));
        Assert.False(PasswordHasher.Verify("x", "", algo));
    }

    [Fact]
    public void Verify_DoesNotThrowOnNonAsciiPasswordAgainstMd5Row()
    {
        // SeedMd5Hash 对非 ASCII 抛 NotSupportedException；Verify 必须吞掉并返回 false，
        // 否则中文口令用户撞上历史 MD5 行会得到 500 而不是「密码错误」。
        var legacy = PasswordHasher.HashMd5_16("123456");
        Assert.False(PasswordHasher.Verify("口令中文", legacy, PasswordHasher.AlgoMd5_16));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("MD5_16", true)]
    [InlineData("md5_16", true)]
    [InlineData("PBKDF2", false)]
    public void ShouldUpgrade_FlagsLegacyAlgorithms(string? algo, bool expected) =>
        Assert.Equal(expected, PasswordHasher.ShouldUpgrade(algo));
}
