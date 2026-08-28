using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>事件实例只读查询。</summary>
public sealed class EEventInstanceQueryService
{
    private readonly FrameworkDbContext _db;

    public EEventInstanceQueryService(FrameworkDbContext db)
    {
        _db = db;
    }

    public static List<(string Value, string Text)> BuildAppCodeFilterOptions() =>
    [
        ("", "全部"),
        ("FRAME", "FRAME"),
        ("CRM", "CRM"),
        ("OA", "OA")
    ];

    public static List<(string Value, string Text)> BuildStatusFilterOptions() =>
    [
        ("", "全部"),
        ("NEW", "NEW"),
        ("PROCESSING", "PROCESSING"),
        ("DONE", "DONE"),
        ("FAILED", "FAILED")
    ];

    public async Task<List<(string Value, string Text)>> LoadEventCodeFilterOptionsAsync(CancellationToken ct)
    {
        var rows = await _db.EEventConfigs.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .WhereActiveBStatus(x => x.BStatus)
            .OrderBy(x => x.AppCode).ThenBy(x => x.EventCode)
            .Select(x => new { x.EventCode, Label = x.AppCode + "." + x.EventCode })
            .ToListAsync(ct);
        var list = new List<(string, string)> { ("", "全部") };
        list.AddRange(rows.Select(x => (x.EventCode, x.Label)));
        return list;
    }

    public async Task<(IReadOnlyList<EEventInstanceListRowVm> pageRows, int total, int totalPages, int page)> GetIndexPageAsync(
        string? appCode1,
        string? eventCode1,
        string? eventStatus1,
        string? createTimeFrom,
        string? createTimeTo,
        string searchField,
        string searchContent,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var rows = await _db.EEventInstances.AsNoTracking().ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(appCode1))
            rows = rows.Where(x => string.Equals(x.AppCode, appCode1.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(eventCode1))
            rows = rows.Where(x => string.Equals(x.EventCode, eventCode1.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(eventStatus1))
            rows = rows.Where(x => string.Equals(x.EventStatus, eventStatus1.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        if (DateTime.TryParse(createTimeFrom, out var fromDt))
            rows = rows.Where(x => x.CreateTime >= fromDt).ToList();
        if (DateTime.TryParse(createTimeTo, out var toDt))
            rows = rows.Where(x => x.CreateTime < toDt.AddDays(1)).ToList();

        var kw = (searchContent ?? "").Trim();
        if (kw.Length > 0)
        {
            rows = (searchField ?? "ObjectKey") switch
            {
                "EventCode" => rows.Where(x => (x.EventCode ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                "ObjectType" => rows.Where(x => (x.ObjectType ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows.Where(x => (x.ObjectKey ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList()
            };
        }

        rows = rows.OrderByDescending(x => x.DataId).ToList();
        var ids = rows.Select(x => x.DataId).ToList();
        var recvCounts = await _db.EEventReceivers.AsNoTracking()
            .Where(r => ids.Contains(r.EventInstanceId))
            .GroupBy(r => r.EventInstanceId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var todoCounts = await _db.ETodoTasks.AsNoTracking()
            .Where(t => t.EventInstanceId.HasValue && ids.Contains(t.EventInstanceId.Value))
            .GroupBy(t => t.EventInstanceId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var recvMap = recvCounts.ToDictionary(x => x.Key, x => x.Count);
        var todoMap = todoCounts.ToDictionary(x => x.Key, x => x.Count);

        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EEventInstanceListRowVm
        {
            DataId = x.DataId,
            AppCode = x.AppCode,
            EventCode = x.EventCode,
            EventStatus = x.EventStatus,
            ObjectRefDisplay = EventObjectRefHelper.FormatDisplay(x.ObjectType, x.ObjectKey),
            OccurredTime = x.OccurredTime,
            ProcessTime = x.ProcessTime,
            ReceiverCount = recvMap.TryGetValue(x.DataId, out var rc) ? rc : 0,
            TodoCount = todoMap.TryGetValue(x.DataId, out var tc) ? tc : 0
        }).ToList();

        return (pageRows, total, totalPages, page);
    }

    public async Task<EEventInstanceDetailVm?> GetDetailAsync(long id, CancellationToken ct)
    {
        var x = await _db.EEventInstances.AsNoTracking().FirstOrDefaultAsync(i => i.DataId == id, ct);
        if (x == null) return null;

        var cfg = await _db.EEventConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.AppCode == x.AppCode && c.EventCode == x.EventCode, ct);

        string triggerDisplay = "-";
        if (x.TriggerUserId.HasValue)
        {
            var u = await _db.EUsers.AsNoTracking().FirstOrDefaultAsync(e => e.DataId == x.TriggerUserId.Value, ct);
            triggerDisplay = u == null ? $"#{x.TriggerUserId}" : $"{u.LoginId} - {u.RealName}".Trim();
        }

        var receivers = await _db.EEventReceivers.AsNoTracking()
            .Where(r => r.EventInstanceId == id)
            .OrderBy(r => r.DataId)
            .ToListAsync(ct);
        var userIds = receivers.Select(r => r.ReceiverUserId).Distinct().ToList();
        var users = await _db.EUsers.AsNoTracking()
            .Where(u => userIds.Contains(u.DataId))
            .Select(u => new { u.DataId, u.LoginId, u.RealName })
            .ToListAsync(ct);
        var userMap = users.ToDictionary(u => u.DataId, u => $"{u.LoginId} - {u.RealName}".Trim());

        var recvRows = receivers.Select(r => new EEventInstanceReceiverRowVm
        {
            DataId = r.DataId,
            UserDisplay = userMap.TryGetValue(r.ReceiverUserId, out var d) ? d : $"#{r.ReceiverUserId}",
            ResolveType = r.ResolveType,
            ResolveReason = r.ResolveReason
        }).ToList();

        var logs = await _db.EEventLogs.AsNoTracking()
            .Where(l => l.EventInstanceId == id)
            .OrderByDescending(l => l.DataId)
            .Take(20)
            .Select(l => new EEventInstanceLogRowVm
            {
                DataId = l.DataId,
                CreateTime = l.CreateTime,
                ActionType = l.ActionType ?? "",
                HandleResult = l.HandleResult ?? "",
                Remark = l.Remark
            })
            .ToListAsync(ct);

        var todoCount = await _db.ETodoTasks.CountAsync(t => t.EventInstanceId == id, ct);
        var logCount = await _db.EEventLogs.CountAsync(l => l.EventInstanceId == id, ct);

        return new EEventInstanceDetailVm
        {
            DataId = x.DataId,
            AppCode = x.AppCode,
            EventCode = x.EventCode,
            EventName = cfg?.EventName ?? "",
            EventStatus = x.EventStatus,
            ObjectType = x.ObjectType,
            ObjectKey = x.ObjectKey,
            ObjectRefDisplay = EventObjectRefHelper.FormatDisplay(x.ObjectType, x.ObjectKey),
            ObjectCode = x.ObjectCode,
            ObjectTitle = x.ObjectTitle,
            ObjectUrl = x.ObjectUrl,
            PayloadJson = x.PayloadJson,
            IdempotencyKey = x.IdempotencyKey,
            LastError = x.LastError,
            RetryCount = x.RetryCount,
            TriggerUserDisplay = triggerDisplay,
            OccurredTime = x.OccurredTime,
            CreateTime = x.CreateTime,
            ProcessTime = x.ProcessTime,
            ReceiverCount = receivers.Count,
            TodoCount = todoCount,
            LogCount = logCount,
            Receivers = recvRows,
            RecentLogs = logs
        };
    }
}
