using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class EventDemoPublishFormVm
{
    [Required(ErrorMessage = "应用编码不能为空")]
    public string AppCode { get; set; } = "FRAME";

    [Required(ErrorMessage = "事件编码不能为空")]
    public string EventCode { get; set; } = "FRAME.DEMO.SUBMIT";

    [Required(ErrorMessage = "对象类型不能为空")]
    public string ObjectType { get; set; } = "Demo";

    [Required(ErrorMessage = "对象键不能为空")]
    public string ObjectKey { get; set; } = "";

    public string? ObjectTitle { get; set; }
    public string? PayloadJson { get; set; }
    public string? IdempotencyKey { get; set; }
}

public sealed class EventDemoPublishResultVm
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public long? EventInstanceId { get; set; }
    public int ReceiverCount { get; set; }
    public int TodoCreatedCount { get; set; }
    public bool IdempotentHit { get; set; }
}
