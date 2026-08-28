using System.Linq.Expressions;

namespace FamilyTree.Helpers;

/// <summary>
/// EF Core 可翻译的 BStatus 筛选（与 <see cref="EBStatusHelper.IsActiveBStatus"/> 语义一致）。
/// 表达式仅使用等值比较，可映射为 SQL Server 2008 兼容的 WHERE 子句。
/// </summary>
public static class EBStatusQuery
{
    public static IQueryable<T> WhereActiveBStatus<T>(
        this IQueryable<T> source,
        Expression<Func<T, string?>> statusSelector)
    {
        var param = statusSelector.Parameters[0];
        var s = statusSelector.Body;
        Expression body = Expression.Equal(s, Expression.Constant(null, typeof(string)));
        body = Or(body, s, "");
        body = Or(body, s, "启用");
        body = Or(body, s, "1");
        body = Or(body, s, "A");
        body = Or(body, s, "a");
        return source.Where(Expression.Lambda<Func<T, bool>>(body, param));
    }

    private static Expression Or(Expression left, Expression status, string value) =>
        Expression.OrElse(left, Expression.Equal(status, Expression.Constant(value, typeof(string))));
}
