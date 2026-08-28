using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EManagerSubordinateFormVm
{
    public int DataId { get; set; }

    [Required(ErrorMessage = "请选择上级用户")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择上级用户")]
    public int ManagerUserId { get; set; }

    [Required(ErrorMessage = "请选择下级用户")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择下级用户")]
    public int SubUserId { get; set; }

    /// <summary>0 表示未选部门（存库 NULL，语义：不限部门）。</summary>
    public int DeptId { get; set; }

    /// <summary>0 表示无上级岗位（存库 NULL）。</summary>
    public int ManagerPostId { get; set; }

    /// <summary>0 表示无下级岗位（存库 NULL）。</summary>
    public int SubPostId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "显示顺序必须大于等于0")]
    public int DispSeq { get; set; } = 99;

    [Required(ErrorMessage = "状态不能为空")]
    [StringLength(20)]
    public string BStatus { get; set; } = "启用";

    public string? Remark { get; set; }
}

public sealed class EManagerSubordinateListRowVm
{
    public int DataId { get; set; }
    public string ManagerDisplay { get; set; } = "";
    public string SubDisplay { get; set; } = "";
    public string ManagerPostText { get; set; } = "-";
    public string SubPostText { get; set; } = "-";
    public string DeptText { get; set; } = "-";
    public string BStatusText { get; set; } = "";
}
