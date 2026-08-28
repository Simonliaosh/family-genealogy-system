using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EMemberFormVm
{
    public int DataId { get; set; }

    [Required(ErrorMessage = "请输入人员编号")]
    [StringLength(30, ErrorMessage = "人员编号最多 30 个字符")]
    [Display(Name = "人员编号")]
    public string MemberId { get; set; } = "";

    [Required(ErrorMessage = "请输入姓名")]
    [StringLength(20, ErrorMessage = "姓名最多 20 个字符")]
    [Display(Name = "姓名")]
    public string MemberName { get; set; } = "";

    [StringLength(2)]
    [Display(Name = "性别")]
    public string? Sex { get; set; }

    [StringLength(12)]
    [Display(Name = "民族")]
    public string? Nation { get; set; }

    [StringLength(30)]
    [Display(Name = "籍贯")]
    public string? NativePlace { get; set; }

    [StringLength(20)]
    [Display(Name = "身份证号")]
    public string? IdNo { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "出生日期")]
    public DateTime? Birthday { get; set; }

    [StringLength(12)]
    [Display(Name = "学历")]
    public string? ELevel { get; set; }

    [StringLength(20)]
    [Display(Name = "学位")]
    public string? Degree { get; set; }

    [StringLength(40)]
    [Display(Name = "毕业院校")]
    public string? School { get; set; }

    [StringLength(30)]
    [Display(Name = "专业")]
    public string? Speciality { get; set; }

    [StringLength(30)]
    [Display(Name = "计算机能力")]
    public string? ComputerAbility { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "参加工作日期")]
    public DateTime? WorkDate { get; set; }

    [StringLength(10)]
    [Display(Name = "职级")]
    public string? PerGrade { get; set; }

    [Display(Name = "部门")]
    public int? DeptId { get; set; }

    [Display(Name = "岗位")]
    public int? PosId { get; set; }

    [StringLength(100)]
    [Display(Name = "家庭住址")]
    public string? HomeAddr { get; set; }

    [StringLength(20)]
    [Display(Name = "手机")]
    public string? Mobile { get; set; }

    [StringLength(20)]
    [Display(Name = "住宅电话")]
    public string? HomePhone { get; set; }

    [StringLength(20)]
    [Display(Name = "办公电话")]
    public string? WorkPhone { get; set; }

    [StringLength(30)]
    [Display(Name = "微信号")]
    public string? WxNum { get; set; }

    [StringLength(20)]
    [Display(Name = "QQ")]
    public string? QqNum { get; set; }

    [StringLength(80)]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [Display(Name = "电子邮箱")]
    public string? EMail { get; set; }

    [StringLength(8)]
    [Display(Name = "健康状况")]
    public string? Health { get; set; }

    [Display(Name = "备注")]
    public string? Remark { get; set; }
}

public sealed class EMemberListRowVm
{
    public int DataId { get; set; }
    public string MemberId { get; set; } = "";
    public string MemberName { get; set; } = "";
    public string SexDisplay { get; set; } = "";
    public string DeptName { get; set; } = "";
    public string PosName { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string PerGradeDisplay { get; set; } = "";
    /// <summary>列表脱敏展示，非数据库原值。</summary>
    public string IdNoMasked { get; set; } = "";

    public DateTime CreateDate { get; set; }
}

public sealed class EMemberDetailVm
{
    public int DataId { get; set; }
    public string MemberId { get; set; } = "";
    public string MemberName { get; set; } = "";
    public string? Sex { get; set; }
    public string? Nation { get; set; }
    public string? NativePlace { get; set; }
    public string IdNoMasked { get; set; } = "";
    public DateTime? Birthday { get; set; }
    public string? ELevel { get; set; }
    public string? Degree { get; set; }
    public string? School { get; set; }
    public string? Speciality { get; set; }
    public string? ComputerAbility { get; set; }
    public DateTime? WorkDate { get; set; }
    public string? PerGrade { get; set; }
    public string DeptName { get; set; } = "";
    public string PosName { get; set; } = "";
    public string? HomeAddr { get; set; }
    public string? Mobile { get; set; }
    public string? HomePhone { get; set; }
    public string? WorkPhone { get; set; }
    public string? WxNum { get; set; }
    public string? QqNum { get; set; }
    public string? EMail { get; set; }
    public string? Health { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime AmendDate { get; set; }
    public string? OperatorName { get; set; }
}
