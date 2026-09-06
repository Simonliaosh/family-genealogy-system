using FamilyTree.Helpers;
using Xunit;

namespace FamilyTree.Tests;

/// <summary>文本归一化与身份证处理。这些是纯静态函数，决定了姓名索引和 PII 脱敏的行为。</summary>
public class FtTextTests
{
    [Theory]
    [InlineData(" 张三 ", "张三")]
    [InlineData("张　三", "张三")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void NormName_TrimsAndCollapses(string? input, string expected) =>
        Assert.Equal(expected, FtText.NormName(input));

    [Theory]
    [InlineData("11010119900307231X", true)]
    [InlineData("110101199003072311", true)]
    [InlineData("11010119900307231", false)]   // 17 位
    [InlineData("11010119900307231A", false)]  // 末位非 X
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsIdCard_AcceptsOnly18DigitForm(string? input, bool expected) =>
        Assert.Equal(expected, FtText.IsIdCard(input));

    [Fact]
    public void MaskLoginId_MasksIdCardKeepingHead3Tail4()
    {
        var masked = FtText.MaskLoginId("11010119900307231X");
        Assert.Equal("110***********231X", masked);
        Assert.Equal(18, masked.Length);
        Assert.DoesNotContain("19900307", masked);
    }

    [Theory]
    [InlineData("cfadmin")]
    [InlineData("13800138000")]      // 手机号不是 18 位身份证，原样保留
    [InlineData("")]
    public void MaskLoginId_LeavesNonIdCardUntouched(string input) =>
        Assert.Equal(input, FtText.MaskLoginId(input));

    [Fact]
    public void MaskLoginId_HandlesNull() => Assert.Equal("", FtText.MaskLoginId(null));

    [Fact]
    public void HashIdCard_IsSaltSensitive()
    {
        var a = FtText.HashIdCard("11010119900307231X", "salt-a");
        var b = FtText.HashIdCard("11010119900307231X", "salt-b");
        Assert.NotEqual(a, b);
        Assert.Equal(64, a.Length);
    }

    [Fact]
    public void HashIdCard_IsStableForSameInput() =>
        Assert.Equal(
            FtText.HashIdCard("11010119900307231X", "s"),
            FtText.HashIdCard("11010119900307231X", "s"));

    [Theory]
    [InlineData("1990-01-01", 1990)]
    [InlineData("1990年1月", 1990)]
    [InlineData("不详", null)]
    [InlineData("", null)]
    public void ParseBirthYear_ExtractsFourDigitYear(string input, int? expected) =>
        Assert.Equal(expected, FtText.ParseBirthYear(input));

    [Fact]
    public void ClipReq_FallsBackWhenEmpty() => Assert.Equal("默认", FtText.ClipReq("  ", 10, "默认"));

    [Fact]
    public void ClipReq_TruncatesToMaxLength() => Assert.Equal("abcde", FtText.ClipReq("abcdefgh", 5));
}
