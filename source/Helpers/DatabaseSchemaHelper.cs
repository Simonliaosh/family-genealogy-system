using FamilyTree.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Helpers;

public static class DatabaseSchemaHelper
{
    public static async Task<bool> TableExistsAsync(FrameworkDbContext db, string tableName, CancellationToken ct = default)
    {
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "SELECT CASE WHEN OBJECT_ID(@t, 'U') IS NOT NULL THEN 1 ELSE 0 END";
            var p = cmd.CreateParameter();
            p.ParameterName = "@t";
            p.Value = "dbo." + tableName;
            cmd.Parameters.Add(p);
            var result = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(result) == 1;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
