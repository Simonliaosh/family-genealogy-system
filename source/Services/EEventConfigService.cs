using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class EEventConfigService
{
    private readonly FrameworkDbContext _db;
    private readonly DictService _dict;

    public EEventConfigService(FrameworkDbContext db, DictService dict)
    {
        _db = db;
        _dict = dict;
    }

    public string Normalize(string? value, int maxLen, string fallback = "")
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) v = fallback;
        return v.Length <= maxLen ? v : v[..maxLen];
    }

    public static IReadOnlyDictionary<string, string> ExecTypeMap { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["SYNC"] = "同步执行",
        ["ASYNC"] = "异步执行",
        ["MANUAL"] = "手动触发"
    };

    public static IReadOnlyDictionary<string, string> EventTypeMap { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["APPROVAL"] = "审批",
        ["NOTICE"] = "通知",
        ["TASK"] = "任务",
        ["SYSTEM"] = "系统",
        ["TODO"] = "任务"
    };

    public static IReadOnlyDictionary<string, string> HandleModeMap { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["SINGLE"] = "单人",
        ["ALL"] = "会签(全部)",
        ["ANY"] = "任一",
        ["CLAIM"] = "抢单"
    };

    public static List<(string Value, string Text)> BuildAppCodeFormOptions() =>
    [
        ("FRAME", "FRAME 框架"),
        ("CRM", "CRM"),
        ("OA", "OA")
    ];

    private static List<(string Value, string Text)> FallbackExecTypeFormOptions() =>
        ExecTypeMap.Select(x => (x.Key, x.Value)).ToList();

    private static List<(string Value, string Text)> FallbackEventTypeFormOptions()
    {
        var list = new List<(string Value, string Text)> { ("", "（无）") };
        list.AddRange(EventTypeMap.Where(x => x.Key != "TODO").Select(x => (x.Key, x.Value)));
        return list;
    }

    private static List<(string Value, string Text)> FallbackHandleModeFormOptions() =>
        HandleModeMap.Select(x => (x.Key, x.Value)).ToList();

    public Task<List<(string Value, string Text)>> GetExecTypeFilterOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.ExecType, forFilter: true, formEmptyLabel: null,
            FallbackExecTypeFormOptions(), ct: ct);

    public Task<List<(string Value, string Text)>> GetExecTypeFormOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.ExecType, forFilter: false, formEmptyLabel: null,
            FallbackExecTypeFormOptions(), ct: ct);

    public Task<List<(string Value, string Text)>> GetEventTypeFormOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.EventType, forFilter: false, formEmptyLabel: "（无）",
            FallbackEventTypeFormOptions(), ct: ct);

    public Task<List<(string Value, string Text)>> GetHandleModeFormOptionsAsync(CancellationToken ct) =>
        _dict.GetSelectOptionsOrFallbackAsync(
            DictCodes.HandleMode, forFilter: false, formEmptyLabel: null,
            FallbackHandleModeFormOptions(), ct: ct);

    public Task<Dictionary<string, string>> GetExecTypeMapAsync(CancellationToken ct) =>
        _dict.GetItemMapMergedAsync(DictCodes.ExecType, ExecTypeMap, ct: ct);

    public Task<Dictionary<string, string>> GetEventTypeMapAsync(CancellationToken ct) =>
        _dict.GetItemMapMergedAsync(DictCodes.EventType, EventTypeMap, ct: ct);

    public Task<Dictionary<string, string>> GetHandleModeMapAsync(CancellationToken ct) =>
        _dict.GetItemMapMergedAsync(DictCodes.HandleMode, HandleModeMap, ct: ct);

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

    public static List<(string Value, string Text)> BuildSearchFieldOptions() =>
    [
        ("EventCode", "事件编码"),
        ("EventName", "事件名称"),
        ("AppCode", "应用编码"),
        ("PageUrl", "页面地址"),
        ("Remark", "备注")
    ];

    public static string NormalizeExecType(string? value)
    {
        var v = (value ?? "").Trim().ToUpperInvariant();
        return v switch
        {
            "0" or "SYNC" => "SYNC",
            "1" or "ASYNC" => "ASYNC",
            "2" or "3" or "MANUAL" => "MANUAL",
            _ => ExecTypeMap.ContainsKey(v) ? v : "ASYNC"
        };
    }

    public static string NormalizeHandleMode(string? value)
    {
        var v = (value ?? "").Trim();
        if (v.Equals("Single", StringComparison.OrdinalIgnoreCase)) return "SINGLE";
        if (v.Equals("AnyOne", StringComparison.OrdinalIgnoreCase)) return "ANY";
        if (v.Equals("All", StringComparison.OrdinalIgnoreCase)) return "ALL";
        if (v.Equals("Order", StringComparison.OrdinalIgnoreCase)) return "SINGLE";
        var u = v.ToUpperInvariant();
        return HandleModeMap.ContainsKey(u) ? u : "SINGLE";
    }

    public async Task<List<(string Value, string Text)>> LoadActiveMenuGroupOptionsAsync(string? appCode, CancellationToken ct)
    {
        var ac = (appCode ?? "").Trim();
        var q = _db.EMenuGroups.AsNoTracking().Where(x => !x.IsDeleted);
        if (ac.Length > 0)
            q = q.Where(x => x.AppCode == ac);

        var rows = await q.WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.DispSeq).ThenBy(x => x.MenuGroupCode)
            .ToListAsync(ct);
        return rows
            .Select(x => (x.MenuGroupCode, $"{x.MenuGroupCode} - {x.MenuGroupName}".Trim()))
            .ToList();
    }

    public string NormalizeBStatus(string? value) => EBStatusHelper.NormalizeBStatusForSave(value);

    public static string ToStatusDisplay(string? value)
    {
        var v = (value ?? "").Trim();
        return v switch
        {
            "1" or "启用" => "启用",
            "2" or "停用" => "停用",
            _ => v
        };
    }

    public EEventConfigFormVm ToForm(EEventConfig row) => new()
    {
        DataId = row.DataId,
        AppCode = string.IsNullOrWhiteSpace(row.AppCode) ? "FRAME" : row.AppCode,
        EventCode = row.EventCode ?? "",
        EventName = row.EventName ?? "",
        PageUrl = row.PageUrl ?? "",
        MenuGroupCode = row.MenuGroupCode ?? "",
        ExecType = NormalizeExecType(row.ExecType),
        EventType = string.IsNullOrWhiteSpace(row.EventType) ? null : row.EventType.Trim().ToUpperInvariant(),
        IsGenerateTodo = row.IsGenerateTodo,
        TodoTitle = row.TodoTitle ?? "",
        HandleMode = NormalizeHandleMode(row.HandleMode),
        DefaultDueMinutes = row.DefaultDueMinutes,
        DispSeq = row.DispSeq < 0 ? 0 : row.DispSeq,
        BStatus = NormalizeBStatus(row.BStatus),
        Remark = row.Remark ?? ""
    };

    public void NormalizeFormForSave(EEventConfigFormVm model)
    {
        model.AppCode = Normalize(model.AppCode, 50, "FRAME");
        model.EventCode = Normalize(model.EventCode, 100);
        model.EventName = Normalize(model.EventName, 100);
        var pageUrl = (model.PageUrl ?? "").Trim();
        model.PageUrl = pageUrl.Length == 0 ? null : (pageUrl.Length <= 300 ? pageUrl : pageUrl[..300]);
        var menuGroupCode = (model.MenuGroupCode ?? "").Trim();
        model.MenuGroupCode = menuGroupCode.Length == 0 ? null : (menuGroupCode.Length <= 50 ? menuGroupCode.ToUpperInvariant() : menuGroupCode[..50].ToUpperInvariant());
        model.ExecType = NormalizeExecType(model.ExecType);
        var eventType = (model.EventType ?? "").Trim().ToUpperInvariant();
        model.EventType = eventType.Length == 0 ? null : (eventType.Length <= 50 ? eventType : eventType[..50]);
        var todoTitle = (model.TodoTitle ?? "").Trim();
        model.TodoTitle = todoTitle.Length == 0 ? null : (todoTitle.Length <= 200 ? todoTitle : todoTitle[..200]);
        model.HandleMode = NormalizeHandleMode(model.HandleMode);
        model.BStatus = NormalizeBStatus(model.BStatus);
        model.Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark.Trim();
        if (!model.IsGenerateTodo)
            model.TodoTitle = null;
    }

    public async Task<(IReadOnlyList<EEventConfigListRowVm> pageRows, int totalRecords, int totalPages, int page)> GetIndexPageAsync(
        string? bStatus1,
        string? execType1,
        string? menuGroupCode1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EEventConfigs.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(ct);
        rows = rows.Where(x => MatchBStatusFilter(x.BStatus, bStatus1)
                            && MatchExecTypeFilter(x.ExecType, execType1)
                            && MatchMenuGroupFilter(x.MenuGroupCode, menuGroupCode1)).ToList();

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            rows = (searchField ?? "EventCode") switch
            {
                "EventCode" => rows.Where(x => (x.EventCode ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "EventName" => rows.Where(x => (x.EventName ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "AppCode" => rows.Where(x => (x.AppCode ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "PageUrl" => rows.Where(x => (x.PageUrl ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "Remark" => rows.Where(x => (x.Remark ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows
            };
        }

        rows = SortRows(rows, sortField, sortArrow);

        var execMap = await GetExecTypeMapAsync(ct);
        var eventMap = await GetEventTypeMapAsync(ct);
        var handleMap = await GetHandleModeMapAsync(ct);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EEventConfigListRowVm
        {
            DataId = x.DataId,
            AppCode = x.AppCode,
            EventCode = x.EventCode ?? "",
            EventName = x.EventName ?? "",
            PageUrl = string.IsNullOrWhiteSpace(x.PageUrl) ? "-" : x.PageUrl!,
            ExecTypeText = DictDisplay(NormalizeExecType(x.ExecType), execMap),
            EventTypeText = string.IsNullOrWhiteSpace(x.EventType)
                ? "-"
                : DictDisplay(x.EventType.Trim().ToUpperInvariant(), eventMap),
            IsGenerateTodoText = x.IsGenerateTodo ? "是" : "否",
            HandleModeText = DictDisplay(NormalizeHandleMode(x.HandleMode), handleMap),
            DispSeq = x.DispSeq,
            BStatusText = ToStatusDisplay(x.BStatus)
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<List<(string Value, string Text)>> BuildMenuGroupFilterOptionsAsync(CancellationToken ct)
    {
        var list = new List<(string Value, string Text)> { ("", "全部") };
        var options = await LoadActiveMenuGroupOptionsAsync(null, ct);
        list.AddRange(options);
        return list;
    }

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(EEventConfigFormVm model, bool isEdit, CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();

        var execMap = await GetExecTypeMapAsync(ct);
        var handleMap = await GetHandleModeMapAsync(ct);
        var eventMap = await GetEventTypeMapAsync(ct);

        if (!execMap.ContainsKey(NormalizeExecType(model.ExecType)))
            errors.Add((nameof(model.ExecType), "执行类型无效。"));
        if (!handleMap.ContainsKey(NormalizeHandleMode(model.HandleMode)))
            errors.Add((nameof(model.HandleMode), "处理模式无效。"));
        if (!string.IsNullOrWhiteSpace(model.EventType) && !eventMap.ContainsKey(model.EventType))
            errors.Add((nameof(model.EventType), "事件分类无效。"));

        if (model.IsGenerateTodo && string.IsNullOrWhiteSpace(model.TodoTitle))
            errors.Add((nameof(model.TodoTitle), "生成待办时，待办标题不能为空。"));

        var dup = await _db.EEventConfigs.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted
                             && x.AppCode == model.AppCode
                             && x.EventCode == model.EventCode
                             && (!isEdit || x.DataId != model.DataId), ct);
        if (dup)
            errors.Add((nameof(model.EventCode), "该应用下事件编码已存在。"));

        if (!string.IsNullOrWhiteSpace(model.MenuGroupCode))
        {
            var ok = await _db.EMenuGroups.AsNoTracking()
                .AnyAsync(x => x.MenuGroupCode == model.MenuGroupCode && !x.IsDeleted, ct);
            if (!ok)
                errors.Add((nameof(model.MenuGroupCode), "菜单组编码不存在。"));
        }

        return errors;
    }

    public async Task CreateAsync(EEventConfigFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var row = MapToEntity(model, operatorId, now);
        _db.EEventConfigs.Add(row);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("该应用下事件编码已存在，请换一个。", ex);
        }
    }

    public async Task<bool> TryUpdateAsync(int id, EEventConfigFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.EEventConfigs.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return false;

        ApplyFormToRow(row, model);
        row.AmendDate = DateTime.Now;
        row.OperatorName = operatorId;
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("该应用下事件编码已存在，请换一个。", ex);
        }
        return true;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.EEventConfigs.FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);
        if (row == null) return;
        row.IsDeleted = true;
        row.BStatus = "2";
        row.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task BatchDeleteAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return;
        var rows = await _db.EEventConfigs.Where(x => ids.Contains(x.DataId) && !x.IsDeleted).ToListAsync(ct);
        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.BStatus = "2";
            row.AmendDate = DateTime.Now;
        }
        if (rows.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    public async Task<EEventConfig?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.EEventConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id && !x.IsDeleted, ct);

    private static EEventConfig MapToEntity(EEventConfigFormVm model, string operatorId, DateTime now) =>
        new()
        {
            AppCode = model.AppCode,
            EventCode = model.EventCode,
            EventName = model.EventName,
            PageUrl = model.PageUrl,
            MenuGroupCode = model.MenuGroupCode,
            ExecType = model.ExecType,
            EventType = model.EventType,
            IsGenerateTodo = model.IsGenerateTodo,
            TodoTitle = model.TodoTitle,
            HandleMode = model.HandleMode,
            DefaultDueMinutes = model.DefaultDueMinutes,
            DispSeq = model.DispSeq,
            BStatus = model.BStatus,
            Remark = model.Remark,
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        };

    private static void ApplyFormToRow(EEventConfig row, EEventConfigFormVm model)
    {
        row.AppCode = model.AppCode;
        row.EventName = model.EventName;
        row.PageUrl = model.PageUrl;
        row.MenuGroupCode = model.MenuGroupCode;
        row.ExecType = model.ExecType;
        row.EventType = model.EventType;
        row.IsGenerateTodo = model.IsGenerateTodo;
        row.TodoTitle = model.TodoTitle;
        row.HandleMode = model.HandleMode;
        row.DefaultDueMinutes = model.DefaultDueMinutes;
        row.DispSeq = model.DispSeq;
        row.BStatus = model.BStatus;
        row.Remark = model.Remark;
    }

    private static List<EEventConfig> SortRows(List<EEventConfig> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        return field switch
        {
            "EventCode" => desc ? rows.OrderByDescending(x => x.EventCode).ToList() : rows.OrderBy(x => x.EventCode).ToList(),
            "EventName" => desc ? rows.OrderByDescending(x => x.EventName).ToList() : rows.OrderBy(x => x.EventName).ToList(),
            "ExecType" => desc ? rows.OrderByDescending(x => x.ExecType).ToList() : rows.OrderBy(x => x.ExecType).ToList(),
            "DispSeq" => desc ? rows.OrderByDescending(x => x.DispSeq).ToList() : rows.OrderBy(x => x.DispSeq).ToList(),
            "BStatus" => desc ? rows.OrderByDescending(x => x.BStatus).ToList() : rows.OrderBy(x => x.BStatus).ToList(),
            "CreateDate" => desc ? rows.OrderByDescending(x => x.CreateDate).ToList() : rows.OrderBy(x => x.CreateDate).ToList(),
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

    private static bool MatchExecTypeFilter(string? value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        return string.Equals(NormalizeExecType(value), NormalizeExecType(selected), StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchMenuGroupFilter(string? value, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        return string.Equals((value ?? "").Trim(), selected.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string DictDisplay(string code, IReadOnlyDictionary<string, string> map) =>
        map.TryGetValue(code, out var t) ? t : code;

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
