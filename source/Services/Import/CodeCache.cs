using Microsoft.Data.SqlClient;

namespace FamilyTree.Services.Import;

public sealed class CodeCache
{
    private readonly SqlConnection _conn;

    public CodeCache(SqlConnection conn) => _conn = conn;

    public Dictionary<string, int> Depts { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Posts { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Duties { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Users { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Members { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Indicators { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    public async Task ReloadAllAsync(CancellationToken ct = default)
    {
        Depts = await LoadMapAsync("SELECT DeptCode, DataID FROM dbo.Tbl_E_Department WHERE IsDeleted=0", ct);
        Posts = await LoadMapAsync("SELECT PostCode, DataID FROM dbo.Tbl_E_Position WHERE IsDeleted=0", ct);
        Duties = await LoadMapAsync("SELECT DutyCode, DataID FROM dbo.Tbl_E_Duty WHERE IsDeleted=0", ct);
        Users = await LoadMapAsync("SELECT LoginId, DataID FROM dbo.Tbl_E_Users WHERE IsDeleted=0", ct);
        Members = await LoadMapAsync("SELECT MemberID, DataID FROM dbo.Tbl_E_Member WHERE IsDeleted=0", ct);
        if (await TableExistsAsync("Tbl_Dash_Indicator", ct))
            Indicators = await LoadMapAsync("SELECT IndicatorCode, DataID FROM dbo.Tbl_Dash_Indicator WHERE IsDeleted=0", ct);
    }

    public async Task ReloadDeptsAsync(CancellationToken ct = default) =>
        Depts = await LoadMapAsync("SELECT DeptCode, DataID FROM dbo.Tbl_E_Department WHERE IsDeleted=0", ct);

    public async Task ReloadPostsAsync(CancellationToken ct = default) =>
        Posts = await LoadMapAsync("SELECT PostCode, DataID FROM dbo.Tbl_E_Position WHERE IsDeleted=0", ct);

    public async Task ReloadDutiesAsync(CancellationToken ct = default) =>
        Duties = await LoadMapAsync("SELECT DutyCode, DataID FROM dbo.Tbl_E_Duty WHERE IsDeleted=0", ct);

    public async Task ReloadUsersAsync(CancellationToken ct = default) =>
        Users = await LoadMapAsync("SELECT LoginId, DataID FROM dbo.Tbl_E_Users WHERE IsDeleted=0", ct);

    public async Task ReloadMembersAsync(CancellationToken ct = default) =>
        Members = await LoadMapAsync("SELECT MemberID, DataID FROM dbo.Tbl_E_Member WHERE IsDeleted=0", ct);

    public async Task ReloadIndicatorsAsync(CancellationToken ct = default)
    {
        if (await TableExistsAsync("Tbl_Dash_Indicator", ct))
            Indicators = await LoadMapAsync("SELECT IndicatorCode, DataID FROM dbo.Tbl_Dash_Indicator WHERE IsDeleted=0", ct);
    }

    public int RequireDept(string? code, string context)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException($"{context}: 缺少 DeptCode");
        if (!Depts.TryGetValue(code.Trim(), out var id))
            throw new InvalidOperationException($"{context}: 未找到部门 [{code}]");
        return id;
    }

    public int? OptionalDept(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null :
        Depts.TryGetValue(code.Trim(), out var id) ? id :
        throw new InvalidOperationException($"未找到部门 [{code}]");

    public int RequirePost(string? code, string context)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException($"{context}: 缺少 PostCode");
        if (!Posts.TryGetValue(code.Trim(), out var id))
            throw new InvalidOperationException($"{context}: 未找到岗位 [{code}]");
        return id;
    }

    public int RequireDuty(string? code, string context)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException($"{context}: 缺少 DutyCode");
        if (!Duties.TryGetValue(code.Trim(), out var id))
            throw new InvalidOperationException($"{context}: 未找到职责 [{code}]");
        return id;
    }

    public int RequireUser(string? loginId, string context)
    {
        if (string.IsNullOrWhiteSpace(loginId))
            throw new InvalidOperationException($"{context}: 缺少 LoginId");
        if (!Users.TryGetValue(loginId.Trim(), out var id))
            throw new InvalidOperationException($"{context}: 未找到用户 [{loginId}]");
        return id;
    }

    public int? OptionalUser(string? loginId)
    {
        if (string.IsNullOrWhiteSpace(loginId)) return null;
        return Users.TryGetValue(loginId.Trim(), out var id) ? id :
            throw new InvalidOperationException($"未找到用户 [{loginId}]");
    }

    public int? OptionalMember(string? memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId)) return null;
        return Members.TryGetValue(memberId.Trim(), out var id) ? id :
            throw new InvalidOperationException($"未找到人员档案 MemberID=[{memberId}]");
    }

    public int RequireIndicator(string? code, string context)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException($"{context}: 缺少 IndicatorCode");
        if (!Indicators.TryGetValue(code.Trim(), out var id))
            throw new InvalidOperationException($"{context}: 未找到指标 [{code}]");
        return id;
    }

    private async Task<Dictionary<string, int>> LoadMapAsync(string sql, CancellationToken ct)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = new SqlCommand(sql, _conn);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            map[r.GetString(0).Trim()] = r.GetInt32(1);
        return map;
    }

    private async Task<bool> TableExistsAsync(string table, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(
            "SELECT 1 FROM sys.tables WHERE name=@t", _conn);
        cmd.Parameters.AddWithValue("@t", table);
        return (await cmd.ExecuteScalarAsync(ct)) != null;
    }
}
