using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTree.Models;

/// <summary>E 模块登录日志（Tbl_E_LoginLog）。与 Tbl_UserLoginMsg 的 LoginLog 实体无关。</summary>
[Table("Tbl_E_LoginLog")]
public class ELoginLog
{
    [Key]
    [Column("DataID")]
    public long DataId { get; set; }

    [Column("LoginId")]
    [StringLength(30)]
    public string LoginId { get; set; } = "";

    [Column("UserID")]
    public int? UserId { get; set; }

    [Column("LoginStatus")]
    [StringLength(10)]
    public string LoginStatus { get; set; } = "";

    [Column("FailReason")]
    [StringLength(100)]
    public string? FailReason { get; set; }

    [Column("IPAddress")]
    [StringLength(50)]
    public string? IPAddress { get; set; }

    [Column("LoginTime")]
    public DateTime LoginTime { get; set; }
}
