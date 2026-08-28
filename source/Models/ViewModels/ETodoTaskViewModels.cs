using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class ETodoTaskListRowVm
{
    public int DataId { get; set; }
    public byte Status { get; set; }
    public string TitleDisplay { get; set; } = "";
    public string UserDisplay { get; set; } = "";
    public string StatusText { get; set; } = "";
    public string PriorityText { get; set; } = "";
    public DateTime CreateTime { get; set; }
    public string HandleTimeDisplay { get; set; } = "";
    public string ObjectType { get; set; } = "";
    public string ObjectKey { get; set; } = "";
    public string ObjectRefDisplay { get; set; } = "-";
    public string? EventCode { get; set; }
    public string? BusinessUrl { get; set; }
    public string BusinessLinkLabel { get; set; } = "打开业务单据";
    public long? EventLogId { get; set; }
}

public sealed class ETodoTaskDetailVm
{
    public int DataId { get; set; }
    public long? EventLogId { get; set; }
    public int UserId { get; set; }
    public string UserDisplay { get; set; } = "";
    public string ObjectType { get; set; } = "";
    public string ObjectKey { get; set; } = "";
    public string ObjectRefDisplay { get; set; } = "-";
    public string? ObjectTitle { get; set; }
    public string? EventCode { get; set; }
    public string? BusinessUrl { get; set; }
    public string BusinessLinkLabel { get; set; } = "打开业务单据";
    public byte Status { get; set; }
    public string StatusText { get; set; } = "";
    public string Priority { get; set; } = "NORMAL";
    public string PriorityText { get; set; } = "";
    public DateTime? DueTime { get; set; }
    public string? TodoTitle { get; set; }
    public int? HandlerDeptId { get; set; }
    public string? HandlerDeptDisplay { get; set; }
    public int? HandlerPosId { get; set; }
    public string? HandlerPosDisplay { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? HandleTime { get; set; }
}

public sealed class ETodoTaskHandleFormVm
{
    public int DataId { get; set; }

    /// <summary>目标状态：1 已处理，2 已关闭。</summary>
    [Range(1, 2)]
    public byte NewStatus { get; set; } = 1;

    public int? HandlerDeptId { get; set; }
    public int? HandlerPosId { get; set; }
}
