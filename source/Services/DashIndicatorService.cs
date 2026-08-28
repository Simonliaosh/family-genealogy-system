using System.Text.Json;
using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class DashIndicatorService
{
    private readonly FrameworkDbContext _db;

    public DashIndicatorService(FrameworkDbContext db)
    {
        _db = db;
    }

    public string Normalize(string? value, int maxLen, string fallback = "")
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) v = fallback;
        return v.Length <= maxLen ? v : v[..maxLen];
    }

    public static List<(string Value, string Text)> BuildBStatusFilterOptions() =>
    [
        ("", "全部"),
        ("1", "启用"),
        ("2", "停用")
    ];

    public static List<(string Value, string Text)> BuildBStatusFormOptions() =>
    [
        ("1", "启用"),
        ("2", "停用")
    ];

    public static List<(string Value, string Text)> BuildChartTypeFormOptions()
    {
        var list = new List<(string Value, string Text)>();
        for (byte i = 1; i <= 9; i++)
            list.Add((i.ToString(), DashChartTypes.GetName(i)));
        return list;
    }

    public async Task<List<(string Value, string Text)>> GetAppCodeFormOptionsAsync(CancellationToken ct)
    {
        var rows = await _db.EAppModules.AsNoTracking()
            .Where(x => !x.IsDeleted).WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.AppCode)
            .Select(x => new { x.AppCode, x.AppName })
            .ToListAsync(ct);
        if (rows.Count == 0)
            return [("FRAME", "FRAME")];
        return rows.Select(x => (x.AppCode ?? "FRAME", $"{x.AppCode} - {x.AppName}")).ToList();
    }

    public string NormalizeBStatus(string? value) => EBStatusHelper.NormalizeBStatusForSave(value);

    public string ToStatusDisplay(string? value)
    {
        var v = (value ?? "").Trim();
        return v switch
        {
            "1" or "启用" => "启用",
            "2" or "停用" => "停用",
            _ => v
        };
    }

    public DashIndicatorFormVm ToForm(DashIndicator row) => new()
    {
        DataId = row.DataId,
        IndicatorCode = row.IndicatorCode ?? "",
        IndicatorName = row.IndicatorName ?? "",
        AppCode = row.AppCode ?? "FRAME",
        ChartType = row.ChartType,
        DataSource = row.DataSource ?? "",
        CalcRule = row.CalcRule ?? "{}",
        DefaultTimeScope = row.DefaultTimeScope,
        IsLockCalc = row.IsLockCalc,
        DispSeq = row.DispSeq,
        Remark = row.Remark,
        BStatus = NormalizeBStatus(row.BStatus)
    };

    public void NormalizeFormForSave(DashIndicatorFormVm model)
    {
        model.IndicatorCode = Normalize(model.IndicatorCode, 50).ToUpperInvariant();
        model.IndicatorName = Normalize(model.IndicatorName, 100);
        model.AppCode = Normalize(model.AppCode, 50, "FRAME");
        model.DataSource = Normalize(model.DataSource, 100);
        model.CalcRule = (model.CalcRule ?? "").Trim();
        if (string.IsNullOrEmpty(model.CalcRule)) model.CalcRule = "{}";
        model.BStatus = NormalizeBStatus(model.BStatus);
        model.Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark.Trim();
    }

    public async Task<(IReadOnlyList<DashIndicatorListRowVm> pageRows, int totalRecords, int totalPages, int page)> GetIndexPageAsync(
        string? bStatus1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.DashIndicators.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(ct);

        rows = rows.Where(x => MatchBStatusFilter(x.BStatus, bStatus1)).ToList();

        var sc = (searchContent ?? "").Trim();
        if (sc.Length > 0)
        {
            rows = (searchField ?? "IndicatorCode") switch
            {
                "IndicatorCode" => rows.Where(x => (x.IndicatorCode ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "IndicatorName" => rows.Where(x => (x.IndicatorName ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new DashIndicatorListRowVm
        {
            DataId = x.DataId,
            IndicatorCode = x.IndicatorCode ?? "",
            IndicatorName = x.IndicatorName ?? "",
            AppCode = x.AppCode ?? "",
            ChartType = x.ChartType,
            ChartTypeName = DashChartTypes.GetName(x.ChartType),
            DataSource = x.DataSource ?? "",
            BStatus = ToStatusDisplay(x.BStatus),
            DispSeq = x.DispSeq,
            AmendDate = x.AmendDate
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(
        DashIndicatorFormVm model, bool isEdit, CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();

        if (!DashChartTypes.IsValid(model.ChartType))
            errors.Add((nameof(DashIndicatorFormVm.ChartType), "图表类型无效。"));

        try
        {
            JsonDocument.Parse(model.CalcRule);
        }
        catch (JsonException)
        {
            errors.Add((nameof(DashIndicatorFormVm.CalcRule), "CalcRule 必须是合法 JSON。"));
        }

        var dup = await _db.DashIndicators.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.IndicatorCode == model.IndicatorCode && (!isEdit || x.DataId != model.DataId), ct);
        if (dup)
            errors.Add((nameof(DashIndicatorFormVm.IndicatorCode), "指标编码已存在，请换一个。"));

        return errors;
    }

    public async Task CreateAsync(DashIndicatorFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var entity = new DashIndicator
        {
            IndicatorCode = model.IndicatorCode,
            IndicatorName = model.IndicatorName,
            AppCode = model.AppCode,
            ChartType = model.ChartType,
            DataSource = model.DataSource,
            CalcRule = model.CalcRule,
            DefaultTimeScope = model.DefaultTimeScope,
            IsLockCalc = model.IsLockCalc,
            DispSeq = model.DispSeq,
            Remark = model.Remark,
            BStatus = NormalizeBStatus(model.BStatus),
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        };
        _db.DashIndicators.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("指标编码已存在，请换一个。", ex);
        }
    }

    public async Task<bool> TryUpdateAsync(int id, DashIndicatorFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.DashIndicators.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return false;

        row.IndicatorCode = model.IndicatorCode;
        row.IndicatorName = model.IndicatorName;
        row.AppCode = model.AppCode;
        row.ChartType = model.ChartType;
        row.DataSource = model.DataSource;
        row.CalcRule = model.CalcRule;
        row.DefaultTimeScope = model.DefaultTimeScope;
        row.IsLockCalc = model.IsLockCalc;
        row.DispSeq = model.DispSeq;
        row.Remark = model.Remark;
        row.BStatus = NormalizeBStatus(model.BStatus);
        row.AmendDate = DateTime.Now;
        row.OperatorName = operatorId;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("指标编码已存在，请换一个。", ex);
        }
        return true;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.DashIndicators.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return;
        row.IsDeleted = true;
        row.BStatus = "2";
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<DashIndicator?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.DashIndicators.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);

    private static List<DashIndicator> SortRows(List<DashIndicator> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        return field switch
        {
            "IndicatorCode" => desc ? rows.OrderByDescending(x => x.IndicatorCode).ToList() : rows.OrderBy(x => x.IndicatorCode).ToList(),
            "IndicatorName" => desc ? rows.OrderByDescending(x => x.IndicatorName).ToList() : rows.OrderBy(x => x.IndicatorName).ToList(),
            "DispSeq" => desc ? rows.OrderByDescending(x => x.DispSeq).ToList() : rows.OrderBy(x => x.DispSeq).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            _ => rows.OrderByDescending(x => x.DataId).ToList()
        };
    }

    private static bool MatchBStatusFilter(string? value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        var v = (value ?? "").Trim();
        return selected switch
        {
            "1" => v == "1" || v == "启用",
            "2" => v == "2" || v == "停用",
            _ => true
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        for (var e = ex.InnerException; e != null; e = e.InnerException)
        {
            if (e is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
                return true;
        }
        return false;
    }
}
