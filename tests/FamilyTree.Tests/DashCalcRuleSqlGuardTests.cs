using FamilyTree.Services;
using Xunit;

namespace FamilyTree.Tests;

/// <summary>
/// P1-6：指标 CalcRule 里的自由 SQL 用的是应用自身的 SQL 身份。
/// 该通道默认关闭；即便显式打开，也只放行单条只读 SELECT。
/// </summary>
public class DashCalcRuleSqlGuardTests
{
    [Theory]
    [InlineData("SELECT COUNT(*) AS v FROM dbo.Tbl_E_Users WHERE IsDeleted = 0")]
    [InlineData("  select 1 as v  ")]
    [InlineData("SELECT TOP 10 DataID FROM dbo.Tbl_E_Duty WHERE DataID > @userId")]
    public void IsReadOnlySelect_AllowsPlainSelect(string sql) =>
        Assert.True(DashCalcRuleExecutor.IsReadOnlySelect(sql));

    [Theory]
    [InlineData("SELECT 1; DROP TABLE dbo.Tbl_E_Users")]           // 批处理
    [InlineData("SELECT 1 -- comment")]                             // 行注释
    [InlineData("SELECT 1 /* block */")]                            // 块注释
    [InlineData("UPDATE dbo.Tbl_E_Users SET IsLocked = 0")]         // 写入
    [InlineData("DELETE FROM dbo.FamilyTree_Person")]
    [InlineData("INSERT INTO dbo.Tbl_E_Users DEFAULT VALUES")]
    [InlineData("EXEC sp_who")]
    [InlineData("SELECT * INTO #t FROM dbo.Tbl_E_Users")]           // SELECT ... INTO 是写入
    [InlineData("SELECT * FROM OPENROWSET('SQLNCLI','','SELECT 1')")]
    [InlineData("WAITFOR DELAY '00:00:10'")]
    [InlineData("DROP TABLE dbo.Tbl_E_Users")]
    [InlineData("")]
    [InlineData(null)]
    public void IsReadOnlySelect_RejectsEverythingElse(string? sql) =>
        Assert.False(DashCalcRuleExecutor.IsReadOnlySelect(sql));

    [Fact]
    public void IsReadOnlySelect_DoesNotRejectParameterNamesContainingKeywords()
    {
        // @deleted / @created 这类参数名不该被关键字扫描误伤
        Assert.True(DashCalcRuleExecutor.IsReadOnlySelect("SELECT COUNT(*) AS v FROM dbo.Tbl_E_Duty WHERE DataID = @userId"));
    }
}
