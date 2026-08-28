namespace FamilyTree.Services.Import;

public sealed class ImportOptions
{
    public string DataDir { get; init; } = "";
    public string ConnectionString { get; init; } = "";
    public bool DryRun { get; init; }
    public string Operator { get; init; } = "DATA-IMPORT";
    public bool SkipDash { get; init; }

    /// <summary>
    /// 导入前清空原有组织/用户/订阅/事件配置（默认 true，全量覆盖 cfadmin 等种子用户）。
    /// 使用 --keep-existing 可改为增量合并。
    /// </summary>
    public bool ReplaceExistingOrg { get; init; } = true;

    /// <summary>
    /// Excel 导入前先执行框架 SQL（10/11/20/23）。默认 true；--skip-framework 跳过。
    /// </summary>
    public bool InitFramework { get; init; } = true;

    /// <summary>
    /// 框架 SQL 目录，默认仓库 scripts/。
    /// </summary>
    public string ScriptsDir { get; init; } = "";
}

public sealed class ImportRowResult
{
    public int RowNumber { get; init; }
    public string Key { get; init; } = "";
    public string Action { get; init; } = "";
    public string? Message { get; init; }
}

public sealed class ImportFileResult
{
    public string FileName { get; init; } = "";
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; } = new();
    public List<ImportRowResult> Rows { get; } = new();
}
