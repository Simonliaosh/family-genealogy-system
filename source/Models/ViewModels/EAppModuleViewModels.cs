using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EAppModuleFormVm
{
    public int DataId { get; set; }

    [Required(ErrorMessage = "应用编码不能为空")]
    [StringLength(50)]
    [RegularExpression(@"^[A-Za-z0-9_.]+$", ErrorMessage = "应用编码仅允许字母、数字、下划线、点")]
    public string AppCode { get; set; } = "";

    [Required(ErrorMessage = "应用名称不能为空")]
    [StringLength(100)]
    public string AppName { get; set; } = "";

    [Required]
    [StringLength(30)]
    public string AppType { get; set; } = "BUSINESS";

    [StringLength(300)]
    public string? BaseUrl { get; set; }

    [StringLength(100)]
    public string? Icon { get; set; }

    [Range(0, int.MaxValue)]
    public int DispSeq { get; set; } = 99;

    [Required]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";

    public string? Remark { get; set; }
}

public sealed class EAppModuleManageListRowVm
{
    public int DataId { get; set; }
    public string AppCode { get; set; } = "";
    public string AppName { get; set; } = "";
    public string AppType { get; set; } = "";
    public string AppTypeText { get; set; } = "";
    public int DispSeq { get; set; }
    public string BStatusText { get; set; } = "";
    public DateTime AmendDate { get; set; }
    public string OperatorName { get; set; } = "";
    public bool IsCoreApp { get; set; }
}
