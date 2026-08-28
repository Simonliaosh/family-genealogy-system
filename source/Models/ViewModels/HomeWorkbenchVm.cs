namespace FamilyTree.Models.ViewModels;

public sealed class HomeWorkbenchStatsVm
{
    public int PendingTodoCount { get; set; }
    public int TotalUsers { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalDuties { get; set; }
}

public sealed class HomeWorkbenchVm
{
    public HomeWorkbenchStatsVm Stats { get; init; } = new();
    public IReadOnlyList<HomeMainRowVm> MainRows { get; init; } = Array.Empty<HomeMainRowVm>();
}
