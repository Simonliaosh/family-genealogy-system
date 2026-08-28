using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>首页工作台：组织统计、本人待办与近期事件摘要。</summary>
public class HomeWorkbenchService
{
    private readonly FrameworkDbContext _db;

    public HomeWorkbenchService(FrameworkDbContext db)
    {
        _db = db;
    }

    public async Task<HomeWorkbenchVm> BuildAsync(int eUserId, CancellationToken ct)
    {
        var active = new[] { "1", "启用" };

        var pendingTodoCount = await _db.ETodoTasks.AsNoTracking()
            .CountAsync(x => x.UserId == eUserId && x.Status == 0, ct);

        var totalUsers = await _db.EUsers.AsNoTracking()
            .CountAsync(x => !x.IsDeleted && active.Contains(x.BStatus ?? ""), ct);

        var totalDepartments = await _db.EDepartments.AsNoTracking()
            .CountAsync(x => !x.IsDeleted && active.Contains(x.BStatus ?? ""), ct);

        var totalDuties = await _db.EEDuties.AsNoTracking()
            .CountAsync(x => !x.IsDeleted && active.Contains(x.BStatus ?? ""), ct);

        var stats = new HomeWorkbenchStatsVm
        {
            PendingTodoCount = pendingTodoCount,
            TotalUsers = totalUsers,
            TotalDepartments = totalDepartments,
            TotalDuties = totalDuties
        };

        var mainRows = await BuildMainRowsAsync(eUserId, ct);
        return new HomeWorkbenchVm { Stats = stats, MainRows = mainRows };
    }

    private async Task<IReadOnlyList<HomeMainRowVm>> BuildMainRowsAsync(int eUserId, CancellationToken ct)
    {
        var pendingTodos = await _db.ETodoTasks.AsNoTracking()
            .Where(x => x.UserId == eUserId && x.Status == 0)
            .OrderByDescending(x => x.CreateTime)
            .Take(5)
            .Select(x => new
            {
                x.DataId,
                x.TodoTitle,
                x.EventCode,
                x.ObjectType,
                x.ObjectKey,
                x.CreateTime
            })
            .ToListAsync(ct);

        var recentInstances = await _db.EEventInstances.AsNoTracking()
            .OrderByDescending(x => x.OccurredTime)
            .Take(5)
            .Select(x => new
            {
                x.DataId,
                x.AppCode,
                x.EventCode,
                x.ObjectType,
                x.ObjectKey,
                x.EventStatus,
                x.OccurredTime
            })
            .ToListAsync(ct);

        var recentLogs = await _db.EEventLogs.AsNoTracking()
            .OrderByDescending(x => x.CreateTime)
            .Take(5)
            .Select(x => new
            {
                x.DataId,
                x.EventCode,
                x.ActionType,
                x.HandleResult,
                x.CreateTime
            })
            .ToListAsync(ct);

        var todoItems = pendingTodos.Count == 0
            ? new[] { new HomeMainItemVm { Text = "暂无待处理待办", Href = "/ETodoTask/Index" } }
            : pendingTodos.Select(t => new HomeMainItemVm
            {
                Text = string.IsNullOrWhiteSpace(t.TodoTitle)
                    ? $"{t.EventCode} · {EventObjectRefHelper.FormatDisplay(t.ObjectType, t.ObjectKey)}"
                    : t.TodoTitle,
                DateHint = t.CreateTime.ToString("MM-dd HH:mm"),
                Href = $"/ETodoTask/Details/{t.DataId}"
            }).ToArray();

        var instanceItems = recentInstances.Count == 0
            ? new[] { new HomeMainItemVm { Text = "尚无事件实例，可在「事件发布 Demo」联调", Href = "/EventDemo/Index" } }
            : recentInstances.Select(i => new HomeMainItemVm
            {
                Text = $"{i.AppCode}.{i.EventCode} · {EventObjectRefHelper.FormatDisplay(i.ObjectType, i.ObjectKey)} ({i.EventStatus})",
                DateHint = i.OccurredTime.ToString("MM-dd HH:mm"),
                Href = $"/EEventInstanceQuery/Details/{i.DataId}"
            }).ToArray();

        var logItems = recentLogs.Count == 0
            ? new[] { new HomeMainItemVm { Text = "暂无事件日志", Href = "/EEventLog/Index" } }
            : recentLogs.Select(l => new HomeMainItemVm
            {
                Text = $"{l.EventCode} · {EEventLogService.ToActionTypeText(l.ActionType)} / {EEventLogService.ToHandleResultText(l.HandleResult)}",
                DateHint = l.CreateTime.ToString("MM-dd HH:mm"),
                Href = $"/EEventLog/Index"
            }).ToArray();

        return new[]
        {
            new HomeMainRowVm
            {
                Left = new HomeMainPanelVm
                {
                    Title = "我的待办",
                    ListHref = "/ETodoTask/Index",
                    Items = todoItems
                },
                Right = new HomeMainPanelVm
                {
                    Title = "近期事件实例",
                    ListHref = "/EEventInstanceQuery/Index",
                    Items = instanceItems
                }
            },
            new HomeMainRowVm
            {
                Left = new HomeMainPanelVm
                {
                    Title = "近期事件日志",
                    ListHref = "/EEventLog/Index",
                    Items = logItems
                },
                Right = new HomeMainPanelVm
                {
                    Title = "联调与配置",
                    ListHref = "/EventDemo/Index",
                    Items = new[]
                    {
                        new HomeMainItemVm { Text = "事件发布 Demo", Href = "/EventDemo/Index" },
                        new HomeMainItemVm { Text = "事件配置", Href = "/EEventConfig/Index" },
                        new HomeMainItemVm { Text = "流转规则", Href = "/EEventFlowRule/Index" },
                        new HomeMainItemVm { Text = "字典查询", Href = "/EDictQuery/Index" },
                        new HomeMainItemVm { Text = "字典维护", Href = "/EDictType/Index" },
                        new HomeMainItemVm { Text = "应用模块维护", Href = "/EAppModule/Index" }
                    }
                }
            }
        };
    }
}
