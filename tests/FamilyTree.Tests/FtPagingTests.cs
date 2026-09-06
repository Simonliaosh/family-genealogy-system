using FamilyTree.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FamilyTree.Tests;

public class FtPagingTests
{
    private static List<int> Items(int n) => Enumerable.Range(1, n).ToList();

    /// <summary>PageAsync 走的是 EF 的异步算子，需要真正的 EF 查询提供程序。</summary>
    private sealed class Ctx : DbContext
    {
        public Ctx(DbContextOptions<Ctx> o) : base(o) { }
        public DbSet<Row> Rows => Set<Row>();
    }

    public sealed class Row
    {
        public int Id { get; set; }
    }

    private static Ctx NewCtx(int rowCount)
    {
        var opt = new DbContextOptionsBuilder<Ctx>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new Ctx(opt);
        ctx.Rows.AddRange(Enumerable.Range(1, rowCount).Select(i => new Row { Id = i }));
        ctx.SaveChanges();
        return ctx;
    }

    [Fact]
    public void Page_ReturnsRequestedSlice()
    {
        var (rows, total, pages, page) = FtPaging.Page(Items(50), 2, 16);
        Assert.Equal(50, total);
        Assert.Equal(4, pages);
        Assert.Equal(2, page);
        Assert.Equal(16, rows.Count);
        Assert.Equal(17, rows[0]);
    }

    [Fact]
    public void Page_ClampsPageBeyondEnd()
    {
        var (rows, _, pages, page) = FtPaging.Page(Items(10), 99, 16);
        Assert.Equal(1, pages);
        Assert.Equal(1, page);
        Assert.Equal(10, rows.Count);
    }

    [Fact]
    public void Page_HandlesEmptySource()
    {
        // 空集合仍报 1 页：视图里的分页条依赖这个约定
        var (rows, total, pages, page) = FtPaging.Page(Items(0), 1, 16);
        Assert.Empty(rows);
        Assert.Equal(0, total);
        Assert.Equal(1, pages);
        Assert.Equal(1, page);
    }

    [Fact]
    public void Page_CapsPageSizeAt200()
    {
        var (rows, _, _, _) = FtPaging.Page(Items(500), 1, 10_000);
        Assert.Equal(200, rows.Count);
    }

    [Fact]
    public void Page_DefaultsNonPositivePageSize()
    {
        var (rows, _, _, _) = FtPaging.Page(Items(50), 1, 0);
        Assert.Equal(16, rows.Count);
    }

    [Fact]
    public async Task PageAsync_MatchesInMemoryPageSemantics()
    {
        // 库侧分页与内存分页必须给出同样的四元组，否则逐个列表迁移时会静默改变行为
        using var ctx = NewCtx(50);
        var src = Items(50);
        foreach (var (p, size) in new[] { (1, 16), (2, 16), (4, 16), (99, 16), (1, 0) })
        {
            var mem = FtPaging.Page(src, p, size);
            var db = await FtPaging.PageAsync(ctx.Rows.OrderBy(x => x.Id), p, size, default);
            Assert.Equal(mem.total, db.total);
            Assert.Equal(mem.totalPages, db.totalPages);
            Assert.Equal(mem.page, db.page);
            Assert.Equal(mem.pageRows, db.pageRows.Select(r => r.Id).ToList());
        }
    }

    [Fact]
    public async Task PageAsync_HandlesEmptySource()
    {
        using var ctx = NewCtx(0);
        var (rows, total, pages, page) = await FtPaging.PageAsync(ctx.Rows.OrderBy(x => x.Id), 3, 16, default);
        Assert.Empty(rows);
        Assert.Equal(0, total);
        Assert.Equal(1, pages);
        Assert.Equal(1, page);
    }
}
