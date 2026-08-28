using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public class EUsersFormVm
{
    public int DataId { get; set; }

    [Display(Name = "账号")]
    [Required(ErrorMessage = "账号不能为空")]
    [StringLength(30, ErrorMessage = "账号最多30个字符")]
    public string LoginId { get; set; } = "";

    [Display(Name = "姓名")]
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(20, ErrorMessage = "姓名最多20个字符")]
    public string RealName { get; set; } = "";

    [Display(Name = "用户类型")]
    [Required(ErrorMessage = "用户类型不能为空")]
    [StringLength(12, ErrorMessage = "用户类型最多12个字符")]
    public string UserType { get; set; } = "";

    [Display(Name = "登录次数")]
    public int LoginCount { get; set; }

    [Display(Name = "最大登录次数")]
    [Range(1, int.MaxValue, ErrorMessage = "最大登录次数必须大于等于1")]
    public int MaxLoginCount { get; set; } = 99999;

    [Display(Name = "密码错误次数")]
    public int PwdErrorCount { get; set; }

    [Display(Name = "最大密码错误次数")]
    [Range(1, int.MaxValue, ErrorMessage = "最大密码错误次数必须大于等于1")]
    public int MaxPwdErrorCount { get; set; } = 5;

    [Display(Name = "锁定状态")]
    [Required(ErrorMessage = "锁定状态不能为空")]
    [StringLength(10)]
    public string IsLocked { get; set; } = "0";

    [Display(Name = "启用状态")]
    [Required(ErrorMessage = "启用状态不能为空")]
    [StringLength(10)]
    public string IsEnabled { get; set; } = "0";

    [Display(Name = "最后登录时间")]
    public DateTime? LastLoginTime { get; set; }

    [Display(Name = "最后密码错误时间")]
    public DateTime? LastPwdErrorTime { get; set; }

    [Display(Name = "业务状态")]
    [Required(ErrorMessage = "业务状态不能为空")]
    [StringLength(10, ErrorMessage = "业务状态最多10个字符")]
    public string BStatus { get; set; } = "0";

    [Display(Name = "初始密码")]
    public string? InitialPassword { get; set; }

    [Display(Name = "重置密码")]
    public string? ResetPassword { get; set; }

    [Display(Name = "确认重置密码")]
    public string? ConfirmResetPassword { get; set; }

    [Display(Name = "客户代码")]
    public int? CustomerId { get; set; }

    [Display(Name = "合作伙伴代码")]
    public int? PartnerId { get; set; }

    [Display(Name = "供应商代码")]
    public int? SupplierId { get; set; }

    [Display(Name = "股东代码")]
    [StringLength(30, ErrorMessage = "股东代码最多30个字符")]
    public string? ShareHolderCode { get; set; }
}

