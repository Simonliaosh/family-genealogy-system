namespace FamilyTree.Services;

/// <summary>平台字典类型编码（与 docs/EFrame_v2_supplement.sql 模块 A 一致）。</summary>
public static class DictCodes
{
    public const string DataScope = "DATA_SCOPE";
    public const string DutyCategory = "DUTY_CATEGORY";
    public const string UserType = "USER_TYPE";
    public const string ResourceType = "RESOURCE_TYPE";

    public const string ExecType = "EXEC_TYPE";
    public const string HandleMode = "HANDLE_MODE";
    public const string EventType = "EVENT_TYPE";
    public const string TargetResolve = "TARGET_RESOLVE";
    /// <summary>可选：流转动作（未建字典时服务层回退 CREATE_TODO 等内置码）。</summary>
    public const string FlowAction = "FLOW_ACTION";
    public const string TodoStatus = "TODO_STATUS";
    public const string TodoPriority = "TODO_PRIORITY";
    public const string HandoverType = "HANDOVER_TYPE";
    public const string SubType = "SUB_TYPE";
    public const string AppType = "APP_TYPE";
    public const string EventAction = "EVENT_ACTION";

    public const string MemberSex = "MEMBER_SEX";
    public const string MemberEduLevel = "MEMBER_EDU_LEVEL";
    public const string MemberPerGrade = "MEMBER_PER_GRADE";
    public const string MemberHealth = "MEMBER_HEALTH";
}
