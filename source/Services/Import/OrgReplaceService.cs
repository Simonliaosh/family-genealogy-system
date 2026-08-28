using Microsoft.Data.SqlClient;

namespace FamilyTree.Services.Import;

/// <summary>
/// 导入前清空原有组织/用户/权限/事件配置，以便 Excel 数据全量覆盖（保留框架 RES.CF.* 等种子菜单）。
/// </summary>
public sealed class OrgReplaceService
{
    private readonly SqlConnection _conn;
    private readonly ImportOptions _opt;

    public OrgReplaceService(SqlConnection conn, ImportOptions opt)
    {
        _conn = conn;
        _opt = opt;
    }

    public async Task<int> ClearExistingOrgAsync(SqlTransaction? tx, CancellationToken ct = default)
    {
        if (_opt.DryRun)
        {
            Console.WriteLine("[DryRun] 将清空原有组织/用户/订阅/事件配置（保留框架 RES.CF.* 菜单资源）。");
            return 0;
        }

        var total = 0;
        // 按外键依赖从叶到根删除；保留 Tbl_E_Resource 中框架种子菜单（RES.CF.* / RES.HR.* / RES.DASH.*）
        var batches = new[]
        {
            // 仪表盘运行时
            "Tbl_Dash_UserOperLog",
            "Tbl_Dash_UserCard",
            "Tbl_Dash_UserSetting",
            "Tbl_Dash_PosTemplate",
            "Tbl_Dash_PosIndicatorPerm",
            "Tbl_Dash_Indicator",
            // 事件运行数据（不清 EventInstance 历史；清待办以便新用户接管）
            "Tbl_E_TodoCandidate",
            "Tbl_E_TodoTaskLog",
            "Tbl_E_TodoTask",
            "Tbl_E_TodoGroup",
            "Tbl_E_EventReceiver",
            "Tbl_E_EventDelivery",
            // 组织与权限
            "Tbl_E_UserHandover",
            "Tbl_E_UserDelegate",
            "Tbl_E_ManagerSubordinate",
            "Tbl_E_UserPosition",
            "Tbl_E_PositionDuty",
            "Tbl_E_Subscription",
            "Tbl_E_ResourcePermission",
            "Tbl_E_EventFlowRule",
            "Tbl_E_EventConfig",
            "Tbl_E_Member",
            "Tbl_E_Users",
            "Tbl_E_DutyResourceAction",
            "Tbl_E_Duty",
            "Tbl_E_Position",
        };

        foreach (var table in batches)
        {
            if (_opt.SkipDash && table.StartsWith("Tbl_Dash_", StringComparison.Ordinal))
                continue;
            total += await DeleteAllAsync(table, tx, ct);
        }

        total += await ClearDepartmentsAsync(tx, ct);

        // 删除 Excel 导入的业务菜单（贸易等），保留框架内置菜单
        total += await ExecAsync(
            @"DELETE FROM dbo.Tbl_E_Resource
              WHERE ResourceID NOT LIKE 'RES.CF.%'
                AND ResourceID NOT LIKE 'RES.HR.%'
                AND ResourceID NOT LIKE 'RES.DASH.%'", tx, ct);

        Console.WriteLine($"已清空原有组织数据，共删除约 {total} 行（含待办/事件接收人）。");
        Console.WriteLine("保留：框架菜单 RES.CF.* / RES.HR.* / RES.DASH.*、字典与应用模块基础数据。");
        return total;
    }

    private async Task<int> DeleteAllAsync(string table, SqlTransaction? tx, CancellationToken ct)
    {
        var sql = $@"
IF OBJECT_ID(N'dbo.{table}', N'U') IS NOT NULL
BEGIN
    DECLARE @n INT;
    DELETE FROM dbo.[{table}];
    SET @n = @@ROWCOUNT;
    SELECT @n;
END
ELSE SELECT 0;";
        await using var cmd = new SqlCommand(sql, _conn, tx);
        var result = await cmd.ExecuteScalarAsync(ct);
        var n = result is int i ? i : Convert.ToInt32(result ?? 0);
        if (n > 0)
            Console.WriteLine($"  清空 {table}: {n} 行");
        return n;
    }

    private async Task<int> ClearDepartmentsAsync(SqlTransaction? tx, CancellationToken ct)
    {
        const string sql = @"
IF OBJECT_ID(N'dbo.Tbl_E_Department', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.Tbl_E_Department SET ParentDeptID = NULL WHERE ParentDeptID IS NOT NULL;
    DELETE FROM dbo.Tbl_E_Department;
    SELECT @@ROWCOUNT;
END
ELSE SELECT 0;";
        await using var cmd = new SqlCommand(sql, _conn, tx);
        var result = await cmd.ExecuteScalarAsync(ct);
        var n = result is int i ? i : Convert.ToInt32(result ?? 0);
        if (n > 0)
            Console.WriteLine($"  清空 Tbl_E_Department: {n} 行");
        return n;
    }

    private async Task<int> ExecAsync(string sql, SqlTransaction? tx, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, _conn, tx);
        return await cmd.ExecuteNonQueryAsync(ct);
    }
}
