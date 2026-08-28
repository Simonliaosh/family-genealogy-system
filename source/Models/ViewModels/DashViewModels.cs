using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models.ViewModels;

public sealed class DashIndicatorListRowVm
{
    public int DataId { get; set; }
    public string IndicatorCode { get; set; } = "";
    public string IndicatorName { get; set; } = "";
    public string AppCode { get; set; } = "";
    public byte ChartType { get; set; }
    public string ChartTypeName { get; set; } = "";
    public string DataSource { get; set; } = "";
    public string BStatus { get; set; } = "";
    public int DispSeq { get; set; }
    public DateTime AmendDate { get; set; }
}

public sealed class DashIndicatorFormVm
{
    public int DataId { get; set; }

    [Required(ErrorMessage = "指标编码不能为空")]
    [StringLength(50, ErrorMessage = "指标编码最长50字符")]
    [RegularExpression(@"^[A-Z][A-Z0-9_]{2,49}$", ErrorMessage = "指标编码须以大写字母开头，仅允许大写字母、数字、下划线，长度3~50")]
    public string IndicatorCode { get; set; } = "";

    [Required(ErrorMessage = "指标名称不能为空")]
    [StringLength(100, ErrorMessage = "指标名称最长100字符")]
    public string IndicatorName { get; set; } = "";

    [Required(ErrorMessage = "所属应用不能为空")]
    [StringLength(50, ErrorMessage = "应用编码最长50字符")]
    public string AppCode { get; set; } = "FRAME";

    [Range(1, 9, ErrorMessage = "图表类型须在1~9之间")]
    public byte ChartType { get; set; } = 1;

    [Required(ErrorMessage = "数据来源不能为空")]
    [StringLength(100, ErrorMessage = "数据来源最长100字符")]
    public string DataSource { get; set; } = "";

    [Required(ErrorMessage = "CalcRule不能为空")]
    public string CalcRule { get; set; } = "{}";

    [Range(1, 4, ErrorMessage = "默认时间范围无效")]
    public byte DefaultTimeScope { get; set; } = 3;

    public bool IsLockCalc { get; set; } = true;

    [Range(0, int.MaxValue, ErrorMessage = "显示顺序必须大于等于0")]
    public int DispSeq { get; set; } = 99;

    [StringLength(500, ErrorMessage = "备注最长500字符")]
    public string? Remark { get; set; }

    [Required(ErrorMessage = "状态不能为空")]
    [StringLength(20)]
    public string BStatus { get; set; } = "1";
}

public sealed class DashPosPermIndexVm
{
    public int PosId { get; set; }
    public IReadOnlyList<(int PosId, string PostCName)> PosOptions { get; set; } = [];
    public IReadOnlyList<DashIndicatorListRowVm> Indicators { get; set; } = [];
    public HashSet<int> GrantedIndicatorIds { get; set; } = [];
}

public sealed class DashPosPermSaveVm
{
    [Required(ErrorMessage = "请选择岗位")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择岗位")]
    public int PosId { get; set; }

    public int[] IndicatorIds { get; set; } = [];
}

public sealed class DashPosTemplateCardVm
{
    public int DataId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "请选择业务指标")]
    public int IndicatorId { get; set; }

    public string IndicatorCode { get; set; } = "";

    public byte ChartType { get; set; }

    public string ChartTypeName { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "布局行号无效")]
    public int LayoutRow { get; set; }

    [Range(1, 4, ErrorMessage = "布局列号须在1~4之间")]
    public int LayoutCol { get; set; }

    [Range(1, 4, ErrorMessage = "列跨度须在1~4之间")]
    public byte ColSpan { get; set; } = 2;

    [Required(ErrorMessage = "卡片标题不能为空")]
    [StringLength(100, ErrorMessage = "卡片标题最长100字符")]
    public string CardTitle { get; set; } = "";

    public bool IsLock { get; set; }

    public string? DefaultFilterJson { get; set; }
}

public sealed class DashPosTemplateSaveBatchVm
{
    [Required(ErrorMessage = "请选择岗位")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择岗位")]
    public int PosId { get; set; }

    public List<DashPosTemplateCardVm> Cards { get; set; } = [];
}

public sealed class DashPosTemplateIndexVm
{
    public int PosId { get; set; }
    public IReadOnlyList<(int PosId, string PostCName)> PosOptions { get; set; } = [];
    public IReadOnlyList<DashPosTemplateCardVm> Cards { get; set; } = [];
    public IReadOnlyList<DashIndicatorListRowVm> AvailableIndicators { get; set; } = [];
}

public sealed class DashLayoutVm
{
    public IReadOnlyList<DashUserPosItemVm> PosList { get; set; } = [];
    public int CurrentUserPosId { get; set; }
    public DashGlobalFilterVm GlobalFilter { get; set; } = new();
    public IReadOnlyList<DashCardLayoutVm> Cards { get; set; } = [];
}

public sealed class DashUserPosItemVm
{
    public int UserPosId { get; set; }
    public int PosId { get; set; }
    public string PosName { get; set; } = "";
    public int DeptId { get; set; }
    public string DeptName { get; set; } = "";
    public bool IsPrimary { get; set; }
    public bool IsCurrent { get; set; }
}

public sealed class DashCardLayoutVm
{
    public int CardId { get; set; }
    public int IndicatorId { get; set; }
    public string IndicatorCode { get; set; } = "";
    public string CardTitle { get; set; } = "";
    public byte ChartType { get; set; }
    public int LayoutRow { get; set; }
    public int LayoutCol { get; set; }
    public byte ColSpan { get; set; }
    public bool IsHide { get; set; }
    public bool IsLock { get; set; }
    public string? Remark { get; set; }
    public string? UserFilterJson { get; set; }
    public string? CardGlobalRoute { get; set; }
    public byte LinkType { get; set; } = 1;
}

public sealed class DashGetIndicatorDataRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "指标ID无效")]
    public int IndicatorId { get; set; }

    public int? CardId { get; set; }

    public DashGlobalFilterVm? GlobalFilter { get; set; }

    public string? CardFilterJson { get; set; }

    public Dictionary<string, object>? DynamicParam { get; set; }
}

public sealed class DashSaveLayoutRequest
{
    public List<DashSaveLayoutCardItem> Cards { get; set; } = [];
}

public sealed class DashSaveLayoutCardItem
{
    [Range(1, int.MaxValue, ErrorMessage = "卡片ID无效")]
    public int CardId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "布局行号无效")]
    public int LayoutRow { get; set; }

    [Range(1, 4, ErrorMessage = "布局列号须在1~4之间")]
    public int LayoutCol { get; set; }

    [Range(1, 4, ErrorMessage = "列跨度须在1~4之间")]
    public byte ColSpan { get; set; }
}

public sealed class DashAddCardRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "请选择业务指标")]
    public int IndicatorId { get; set; }

    [Required(ErrorMessage = "卡片标题不能为空")]
    [StringLength(100, ErrorMessage = "卡片标题最长100字符")]
    public string CardTitle { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "布局行号无效")]
    public int LayoutRow { get; set; }

    [Range(1, 4, ErrorMessage = "布局列号须在1~4之间")]
    public int LayoutCol { get; set; }

    [Range(1, 4, ErrorMessage = "列跨度须在1~4之间")]
    public byte ColSpan { get; set; } = 2;
}

public sealed class DashApiResult<T>
{
    public int Code { get; set; }
    public string Msg { get; set; } = "";
    public T? Data { get; set; }
}

public sealed class DashGlobalFilterVm
{
    [Range(1, 4, ErrorMessage = "时间范围无效")]
    public byte TimeType { get; set; } = 3;

    public int DeptId { get; set; }
}
