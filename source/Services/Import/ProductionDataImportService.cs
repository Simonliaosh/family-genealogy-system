using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;

namespace FamilyTree.Services.Import;

public sealed class ProductionDataImportRequest
{
    public bool InitFramework { get; init; } = true;
    public bool ReplaceExistingOrg { get; init; } = true;
    public bool SkipDash { get; init; }
    public bool DryRun { get; init; }
    public string Operator { get; init; } = "WEB-IMPORT";
}

public sealed class ProductionDataImportResult
{
    public bool Success { get; init; }
    public string Report { get; init; } = "";
    public IReadOnlyList<ImportFileResult> FileResults { get; init; } = Array.Empty<ImportFileResult>();
    public string? ErrorMessage { get; init; }
}

public sealed class ProductionDataImportService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public ProductionDataImportService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public string ResolveScriptsDir()
    {
        if (TryResolveScriptsDir(out var dir))
            return dir;
        throw new DirectoryNotFoundException(
            "未找到框架 SQL 目录。请在 appsettings.json 配置 ProductionImport:ScriptsDir（例如 D:\\website\\EFrameDash\\scripts）。");
    }

    public bool TryResolveScriptsDir(out string scriptsDir)
    {
        var configured = _configuration["ProductionImport:ScriptsDir"];
        if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
        {
            scriptsDir = Path.GetFullPath(configured);
            return true;
        }

        var candidate = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "scripts"));
        if (Directory.Exists(candidate))
        {
            scriptsDir = candidate;
            return true;
        }

        scriptsDir = configured ?? candidate;
        return false;
    }

    /// <summary>appsettings 中允许直接读取的服务器 Excel 目录。</summary>
    public IReadOnlyList<string> GetConfiguredDataDirs()
    {
        var list = new List<string>();
        var primary = _configuration["ProductionImport:DataDir"];
        if (!string.IsNullOrWhiteSpace(primary))
            list.Add(primary.Trim());

        foreach (var child in _configuration.GetSection("ProductionImport:AllowedDataDirs").GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(child.Value))
                list.Add(child.Value!.Trim());
        }

        return list
            .Select(p => { try { return Path.GetFullPath(p); } catch { return p; } })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool TryResolveServerDataDir(string? input, out string resolved, out string? error)
    {
        error = null;
        var allowed = GetConfiguredDataDirs();
        if (allowed.Count == 0)
        {
            resolved = "";
            error = "未配置 ProductionImport:DataDir，无法使用服务器目录导入。";
            return false;
        }

        var pick = string.IsNullOrWhiteSpace(input) ? allowed[0] : input.Trim();
        string full;
        try { full = Path.GetFullPath(pick); }
        catch (Exception ex)
        {
            resolved = pick;
            error = "目录路径无效：" + ex.Message;
            return false;
        }

        if (!allowed.Any(a => IsPathUnderRoot(full, a)))
        {
            resolved = full;
            error = "目录不在允许范围内，请在 appsettings.json 的 ProductionImport:DataDir 中配置。";
            return false;
        }

        if (!Directory.Exists(full))
        {
            resolved = full;
            error = "目录不存在：" + full;
            return false;
        }

        if (!Directory.EnumerateFiles(full, "*.xlsx").Any())
        {
            resolved = full;
            error = "目录下未找到 .xlsx 文件。";
            return false;
        }

        resolved = full;
        return true;
    }

    public async Task<ProductionDataImportResult> RunFromUploadedExcelAsync(
        IEnumerable<IFormFile> files,
        ProductionDataImportRequest request,
        CancellationToken ct = default)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "EFrameImport_" + Guid.NewGuid().ToString("N"));
        var dataDir = Path.Combine(tempRoot, "data");
        Directory.CreateDirectory(dataDir);

        try
        {
            var count = 0;
            foreach (var file in files)
            {
                if (file.Length == 0) continue;
                var name = Path.GetFileName(file.FileName);
                if (string.IsNullOrEmpty(name) ||
                    !name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    continue;

                var dest = Path.Combine(dataDir, name);
                await using var src = file.OpenReadStream();
                await using var dst = File.Create(dest);
                await src.CopyToAsync(dst, ct);
                count++;
            }

            if (count == 0)
            {
                return new ProductionDataImportResult
                {
                    Success = false,
                    ErrorMessage = "未收到任何 .xlsx 文件。请使用「选择文件夹」一次选中含 01~17 的目录。"
                };
            }

            return await RunFromDirectoryAsync(dataDir, request, ct);
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { /* best effort */ }
        }
    }

    private static bool IsPathUnderRoot(string fullPath, string rootPath)
    {
        var full = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                   + Path.DirectorySeparatorChar;
        var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                   + Path.DirectorySeparatorChar;
        return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ProductionDataImportResult> RunFromZipAsync(
        Stream zipStream,
        ProductionDataImportRequest request,
        CancellationToken ct = default)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "EFrameImport_" + Guid.NewGuid().ToString("N"));
        var dataDir = Path.Combine(tempRoot, "data");
        Directory.CreateDirectory(dataDir);

        try
        {
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith('/') || string.IsNullOrEmpty(entry.Name))
                        continue;
                    if (!entry.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var dest = Path.Combine(dataDir, entry.Name);
                    await using var src = entry.Open();
                    await using var dst = File.Create(dest);
                    await src.CopyToAsync(dst, ct);
                }
            }

            if (!Directory.EnumerateFiles(dataDir, "*.xlsx").Any())
            {
                return new ProductionDataImportResult
                {
                    Success = false,
                    ErrorMessage = "ZIP 中未找到任何 .xlsx 文件（请打包 01~17 模板）。"
                };
            }

            return await RunFromDirectoryAsync(dataDir, request, ct);
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { /* temp cleanup best effort */ }
        }
    }

    public async Task<ProductionDataImportResult> RunFromDirectoryAsync(
        string dataDir,
        ProductionDataImportRequest request,
        CancellationToken ct = default)
    {
        var connStr = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            return new ProductionDataImportResult
            {
                Success = false,
                ErrorMessage = "未配置 ConnectionStrings:DefaultConnection"
            };
        }

        var scriptsDir = request.InitFramework ? ResolveScriptsDir() :
            (TryResolveScriptsDir(out var s) ? s : "");

        var opt = new ImportOptions
        {
            DataDir = dataDir,
            ConnectionString = connStr,
            DryRun = request.DryRun,
            SkipDash = request.SkipDash,
            ReplaceExistingOrg = request.ReplaceExistingOrg,
            InitFramework = request.InitFramework,
            ScriptsDir = scriptsDir,
            Operator = request.Operator
        };

        try
        {
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync(ct);

            var framework = new FrameworkSqlRunner(opt, conn);
            await framework.RunAsync(ct);

            var cache = new CodeCache(conn);
            var svc = new DataImportService(opt, conn, cache);
            var results = await svc.RunAsync(ct);
            var report = DataImportService.FormatReport(results, svc.IssuedAccounts);

            return new ProductionDataImportResult
            {
                Success = true,
                Report = report,
                FileResults = results
            };
        }
        catch (Exception ex)
        {
            return new ProductionDataImportResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Report = ex.ToString()
            };
        }
    }
}
