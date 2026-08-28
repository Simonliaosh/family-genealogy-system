using FamilyTree.Helpers;
using FamilyTree.Services;
using FamilyTree.Services.Import;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTree.Controllers;

/// <summary>
/// 投产数据导入（Excel + 框架 SQL），框架内置运维能力。
/// </summary>
[Authorize]
public class EDataImportController : Controller
{
    private readonly ProductionDataImportService _import;

    public EDataImportController(ProductionDataImportService import)
    {
        _import = import;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (!CanImport()) return Forbid();

        _import.TryResolveScriptsDir(out var scriptsDir);
        ViewBag.ScriptsDir = scriptsDir;
        ViewBag.ScriptsDirOk = Directory.Exists(scriptsDir);
        ViewBag.DataDirs = _import.GetConfiguredDataDirs();
        ViewBag.Report = TempData["ImportReport"] as string;
        ViewBag.Error = TempData["ImportError"] as string;
        ViewBag.Success = TempData["ImportSuccess"] as bool?;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(200 * 1024 * 1024)]
    public async Task<IActionResult> Import(
        string importMode,
        string? serverDataDir,
        IFormFile? zipFile,
        List<IFormFile>? excelFiles,
        bool initFramework = true,
        bool replaceExisting = true,
        bool skipDash = false,
        bool dryRun = false,
        CancellationToken ct = default)
    {
        if (!CanImport()) return Forbid();

        var operatorName = User.Identity?.Name ?? User.FindFirst(FrameworkClaimTypes.MemberId)?.Value ?? "WEB-IMPORT";
        var request = new ProductionDataImportRequest
        {
            InitFramework = initFramework,
            ReplaceExistingOrg = replaceExisting,
            SkipDash = skipDash,
            DryRun = dryRun,
            Operator = operatorName
        };

        ProductionDataImportResult result;
        var mode = (importMode ?? "server").Trim().ToLowerInvariant();

        switch (mode)
        {
            case "server":
                if (!_import.TryResolveServerDataDir(serverDataDir, out var dir, out var dirErr))
                {
                    TempData["ImportError"] = dirErr;
                    return RedirectToAction(nameof(Index));
                }
                result = await _import.RunFromDirectoryAsync(dir, request, ct);
                break;

            case "folder":
                if (excelFiles == null || excelFiles.Count == 0)
                {
                    TempData["ImportError"] = "请选择本地文件夹（一次选中整个目录，无需逐个文件）。";
                    return RedirectToAction(nameof(Index));
                }
                result = await _import.RunFromUploadedExcelAsync(excelFiles, request, ct);
                break;

            case "zip":
                if (zipFile == null || zipFile.Length == 0)
                {
                    TempData["ImportError"] = "请选择 ZIP 文件。";
                    return RedirectToAction(nameof(Index));
                }
                if (!string.Equals(Path.GetExtension(zipFile.FileName), ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ImportError"] = "请上传 .zip 文件。";
                    return RedirectToAction(nameof(Index));
                }
                await using (var stream = zipFile.OpenReadStream())
                {
                    result = await _import.RunFromZipAsync(stream, request, ct);
                }
                break;

            default:
                TempData["ImportError"] = "未知的导入方式。";
                return RedirectToAction(nameof(Index));
        }

        TempData["ImportSuccess"] = result.Success;
        TempData["ImportReport"] = result.Report;
        if (!result.Success)
            TempData["ImportError"] = result.ErrorMessage ?? "导入失败";

        return RedirectToAction(nameof(Index));
    }

    private bool CanImport()
    {
        var lim = HttpContext.Items["PubFunctionLimit"] as string;
        return FunctionLimitUi.CanCreate(lim, false);
    }
}
