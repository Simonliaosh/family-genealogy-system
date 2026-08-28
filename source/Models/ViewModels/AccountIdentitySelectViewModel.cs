using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public class AccountIdentitySelectViewModel
{
    public string LoginId { get; set; } = "";
    public string RealName { get; set; } = "";

    [Required(ErrorMessage = "请选择本次登录身份")]
    public string SelectedIdentityType { get; set; } = "";

    public List<(string Value, string Text)> IdentityOptions { get; set; } = new();
}

