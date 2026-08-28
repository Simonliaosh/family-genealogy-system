namespace FamilyTree.Models.ViewModels;

public sealed class HomeMainItemVm
{
    public string Text { get; init; } = "";
    public string? DateHint { get; init; }
    public string Href { get; init; } = "#";
}

public sealed class HomeMainPanelVm
{
    public string Title { get; init; } = "";
    public string ListHref { get; init; } = "#";
    public IReadOnlyList<HomeMainItemVm> Items { get; init; } = Array.Empty<HomeMainItemVm>();
}

public sealed class HomeMainRowVm
{
    public HomeMainPanelVm Left { get; init; } = new();
    public HomeMainPanelVm Right { get; init; } = new();
}
