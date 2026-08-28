using FamilyTree.Services;

namespace FamilyTree.Models.ViewModels;

public class MenuSidebarVm
{
    public List<Menu> EcMenus { get; set; } = new();
    public List<MenuGroupModel> MenuGroups { get; set; } = new();
}
