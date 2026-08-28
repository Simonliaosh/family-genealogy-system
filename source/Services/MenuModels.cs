namespace FamilyTree.Services;

public record MenuGroupModel(string FunctionType, IReadOnlyList<MenuItemModel> Items);

public record MenuItemModel(string Title, string Href, string? Icon);
