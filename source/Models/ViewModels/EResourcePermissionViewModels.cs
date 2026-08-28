using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EResourcePermissionFormVm
{
    public int DataId { get; set; }

    [Required(ErrorMessage = "请选择职责")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择职责")]
    public int DutyId { get; set; }

    [Required(ErrorMessage = "请选择资源")]
    [StringLength(50)]
    public string ResourceId { get; set; } = "";

    public bool CanQuery { get; set; } = true;

    public bool CanCreate { get; set; }

    public bool CanUpdate { get; set; }

    public bool CanDelete { get; set; }

    [Required(ErrorMessage = "状态不能为空")]
    [StringLength(10)]
    public string BStatus { get; set; } = "启用";
}

public sealed class EResourcePermissionListRowVm
{
    public int DataId { get; set; }
    public string DutyDisplay { get; set; } = "";
    public string ResourceDisplay { get; set; } = "";
    public string CanQueryText { get; set; } = "";
    public string CanCreateText { get; set; } = "";
    public string CanUpdateText { get; set; } = "";
    public string CanDeleteText { get; set; } = "";
    public string BStatusText { get; set; } = "";
}
