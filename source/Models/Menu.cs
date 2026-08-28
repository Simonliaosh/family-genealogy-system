using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

[Table("Tbl_EC_Menu")]
public class Menu
{
    [Key]
    [Column("EC_MenuId")]
    public int MenuId { get; set; }

    [Required]
    [Column("EC_MenuCode")]
    [StringLength(50)]
    [Display(Name = "菜单代码")]
    public string MenuCode { get; set; } = string.Empty;

    [Required]
    [Column("EC_MenuName")]
    [StringLength(100)]
    [Display(Name = "菜单名称")]
    public string MenuName { get; set; } = string.Empty;

    [Column("EC_ParentMenuId")]
    public int? ParentMenuId { get; set; }

    [Column("EC_MenuUrl")]
    [StringLength(200)]
    [Display(Name = "菜单URL")]
    public string? MenuUrl { get; set; }

    [Column("EC_MenuIcon")]
    [StringLength(50)]
    [Display(Name = "菜单图标")]
    public string? MenuIcon { get; set; }

    [Column("EC_SortOrder")]
    [Display(Name = "排序")]
    public int SortOrder { get; set; }

    [Column("EC_ResponsibilityId")]
    public int? ResponsibilityId { get; set; }

    [Column("EC_IsActive")]
    [Display(Name = "是否启用")]
    public bool IsActive { get; set; } = true;

    [Column("EC_CreateTime")]
    [Display(Name = "创建时间")]
    public DateTime CreateTime { get; set; } = DateTime.Now;

}
