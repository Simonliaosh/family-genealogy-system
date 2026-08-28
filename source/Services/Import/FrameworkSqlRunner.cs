using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;

namespace FamilyTree.Services.Import;

/// <summary>
/// 执行 EFrame 框架 SQL（建表 + 基础种子）。Excel 导入前默认运行。
/// </summary>
public sealed class FrameworkSqlRunner
{
    private static readonly int[] IgnorableErrorNumbers =
    {
        1801,  // 数据库已存在
        15023, // 用户/登录已存在
        2714,  // 对象已存在
        1913,  // 索引/约束已存在
        2720,  // 约束已存在
        2705,  // 列已存在
        3701,  // 无法 drop（不存在）
        4924,  // 列不存在
    };

    /// <summary>
    /// Excel 投产路径：建表 + 字典/模块 + 框架菜单；组织/用户/贸易由 Excel 覆盖。
    /// </summary>
    public static readonly (string File, string Note, bool SkipWhenNoDash)[] ExcelImportScripts =
    {
        ("10-EFrame.sql", "核心表结构", false),
        ("11-CreateTbl_Dash_All.sql", "仪表盘表（可选）", true),
        ("20-Seed_Foundation.sql", "应用模块/菜单组/字典", false),
        ("23-Seed_Menus.sql", "框架菜单 RES.CF.* / RES.HR.*", false),
    };

    private readonly ImportOptions _opt;
    private readonly SqlConnection _conn;

    public FrameworkSqlRunner(ImportOptions opt, SqlConnection conn)
    {
        _opt = opt;
        _conn = conn;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        if (!_opt.InitFramework)
            return;

        if (!Directory.Exists(_opt.ScriptsDir))
            throw new DirectoryNotFoundException($"框架 SQL 目录不存在: {_opt.ScriptsDir}");

        Console.WriteLine();
        Console.WriteLine("======== 执行框架 SQL ========");
        Console.WriteLine($"脚本目录: {_opt.ScriptsDir}");

        if (_opt.DryRun)
        {
            foreach (var (file, note, skipWhenNoDash) in ExcelImportScripts)
            {
                if (_opt.SkipDash && skipWhenNoDash) continue;
                var path = Path.Combine(_opt.ScriptsDir, file);
                Console.WriteLine($"  [DryRun] {(File.Exists(path) ? "将执行" : "缺失")}: {file} — {note}");
            }
            return;
        }

        foreach (var (file, note, skipWhenNoDash) in ExcelImportScripts)
        {
            if (_opt.SkipDash && skipWhenNoDash)
            {
                Console.WriteLine($"  跳过: {file}（--skip-dash）");
                continue;
            }

            var path = Path.Combine(_opt.ScriptsDir, file);
            if (!File.Exists(path))
                throw new FileNotFoundException($"缺少框架 SQL: {path}");

            Console.WriteLine($"  执行: {file} — {note}");
            var text = ReadSqlFile(path);
            if (file.StartsWith("10-", StringComparison.Ordinal))
                text = FilterSchemaOnlyScript(text);

            await ExecuteScriptAsync(text, file, ct);
        }

        Console.WriteLine("框架 SQL 执行完成。");
    }

    private static string FilterSchemaOnlyScript(string sql)
    {
        // 10-EFrame.sql 含 CREATE DATABASE / 固定路径，仅保留 USE [EFrame] 之后的表结构部分
        var idx = sql.IndexOf("USE [EFrame]", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            idx = sql.IndexOf("USE EFrame", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
            sql = sql[idx..];

        return sql;
    }

    private static string ReadSqlFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        var utf8 = Encoding.UTF8.GetString(bytes);
        if (utf8.Contains('\uFFFD') || (!utf8.Contains("EFrame") && !utf8.Contains("Tbl_E_")))
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(936).GetString(bytes);
        }
        return utf8;
    }

    private async Task ExecuteScriptAsync(string sql, string fileName, CancellationToken ct)
    {
        var batches = SplitBatches(sql);
        var executed = 0;
        var skipped = 0;

        foreach (var batch in batches)
        {
            var trimmed = batch.Trim();
            if (trimmed.Length == 0) continue;
            if (ShouldSkipBatch(trimmed))
            {
                skipped++;
                continue;
            }

            await using var cmd = new SqlCommand(trimmed, _conn) { CommandTimeout = 0 };
            try
            {
                await cmd.ExecuteNonQueryAsync(ct);
                executed++;
            }
            catch (SqlException ex) when (IsIgnorable(ex, trimmed))
            {
                skipped++;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(
                    $"框架 SQL {fileName} 执行失败 (批次 {executed + skipped + 1}): {ex.Message}\n---\n{Truncate(trimmed, 500)}", ex);
            }
        }

        Console.WriteLine($"    完成 {fileName}: 执行 {executed} 批，跳过 {skipped} 批");
    }

    private static bool ShouldSkipBatch(string batch)
    {
        var upper = batch.ToUpperInvariant();
        if (Regex.IsMatch(batch, @"^\s*USE\s+\[?(MASTER|EFrame)\]?\s*;?\s*$", RegexOptions.IgnoreCase))
            return true;
        if (upper.Contains("CREATE DATABASE"))
            return true;
        if (upper.Contains("ALTER DATABASE") && upper.Contains("SET "))
            return true;
        if (Regex.IsMatch(batch, @"^\s*CREATE\s+USER\s+", RegexOptions.IgnoreCase))
            return true;
        if (Regex.IsMatch(batch, @"^\s*EXEC\s+SYS\.SP_DB_VARDECIMAL", RegexOptions.IgnoreCase))
            return true;
        if (upper.Contains("FULLTEXTSERVICEPROPERTY"))
            return true;
        return false;
    }

    private static bool IsIgnorable(SqlException ex, string batch)
    {
        foreach (SqlError err in ex.Errors)
        {
            if (IgnorableErrorNumbers.Contains(err.Number))
                return true;
        }

        // 扩展属性/描述已存在
        if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            return true;
        if (ex.Message.Contains("已存在", StringComparison.OrdinalIgnoreCase))
            return true;
        if (batch.Contains("sp_addextendedproperty", StringComparison.OrdinalIgnoreCase)
            && ex.Number is 15233 or 15234)
            return true;

        return false;
    }

    private static IEnumerable<string> SplitBatches(string sql)
    {
        return Regex.Split(sql, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";
}
