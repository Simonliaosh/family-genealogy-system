using FamilyTree.Services;
using Microsoft.Data.SqlClient;
using System.Text;

namespace FamilyTree.Services.Import;

public sealed class DataImportService
{
    private readonly ImportOptions _opt;
    private readonly SqlConnection _conn;
    private readonly CodeCache _cache;
    private readonly List<(string LoginId, string Password)> _issuedAccounts = new();

    private static readonly (string File, string[] Keys, bool Optional)[] ImportSteps =
    {
        ("01-Tbl_E_AppModule.xlsx", new[] { "AppCode" }, false),
        ("02-Tbl_E_MenuGroup.xlsx", new[] { "MenuGroupCode" }, false),
        ("03-Tbl_E_Department.xlsx", new[] { "DeptCode" }, false),
        ("04-Tbl_E_Position.xlsx", new[] { "PostCode" }, false),
        ("05-Tbl_E_Duty.xlsx", new[] { "DutyCode" }, false),
        ("06-Tbl_E_PositionDuty.xlsx", new[] { "PostCode", "DutyCode" }, false),
        ("07-Tbl_E_Users.xlsx", new[] { "LoginId" }, false),
        ("08-Tbl_E_Member.xlsx", new[] { "MemberID" }, true),
        ("09-Tbl_E_UserPosition.xlsx", new[] { "LoginId", "DeptCode", "PostCode" }, false),
        ("10-Tbl_E_Resource.xlsx", new[] { "ResourceID" }, true),
        ("11-Tbl_E_Subscription.xlsx", new[] { "DutyCode", "SubType" }, false),
        ("12-Tbl_E_ResourcePermission.xlsx", new[] { "DutyCode", "ResourceID" }, true),
        ("13-Tbl_E_EventConfig.xlsx", new[] { "EventCode" }, true),
        ("14-Tbl_E_EventFlowRule.xlsx", new[] { "RuleCode" }, true),
        ("15-Tbl_Dash_Indicator.xlsx", new[] { "IndicatorCode" }, true),
        ("16-Tbl_Dash_PosIndicatorPerm.xlsx", new[] { "PostCode", "IndicatorCode" }, true),
        ("17-Tbl_Dash_PosTemplate.xlsx", new[] { "PostCode", "IndicatorCode" }, true),
    };

    public DataImportService(ImportOptions opt, SqlConnection conn, CodeCache cache)
    {
        _opt = opt;
        _conn = conn;
        _cache = cache;
    }

    public async Task<IReadOnlyList<ImportFileResult>> RunAsync(CancellationToken ct = default)
    {
        var results = new List<ImportFileResult>();
        await _cache.ReloadAllAsync(ct);

        SqlTransaction? tx = null;
        if (!_opt.DryRun)
            tx = (SqlTransaction)await _conn.BeginTransactionAsync(ct);

        try
        {
            if (_opt.ReplaceExistingOrg)
            {
                var cleaner = new OrgReplaceService(_conn, _opt);
                await cleaner.ClearExistingOrgAsync(tx, ct);
                await _cache.ReloadAllAsync(ct);
            }

            foreach (var (file, keys, optional) in ImportSteps)
            {
                if (_opt.SkipDash && file.StartsWith("15-", StringComparison.Ordinal))
                    continue;

                var path = Path.Combine(_opt.DataDir, file);
                if (!File.Exists(path))
                {
                    if (optional)
                    {
                        results.Add(new ImportFileResult { FileName = file, Skipped = 1 });
                        continue;
                    }
                    throw new FileNotFoundException($"缺少导入文件: {path}");
                }

                var fr = await ImportFileAsync(path, file, tx, ct);
                results.Add(fr);
                if (fr.Errors.Count > 0)
                    throw new InvalidOperationException($"导入 {file} 失败，见错误列表。");
            }

            if (_opt.DryRun)
                Console.WriteLine("[DryRun] 未提交事务。");
            else
                await tx!.CommitAsync(ct);
        }
        catch
        {
            if (tx != null && !_opt.DryRun)
                await tx.RollbackAsync(ct);
            throw;
        }

        return results;
    }

    public IReadOnlyList<(string LoginId, string Password)> IssuedAccounts => _issuedAccounts;

    private async Task<ImportFileResult> ImportFileAsync(string path, string fileName, SqlTransaction? tx, CancellationToken ct)
    {
        var result = new ImportFileResult { FileName = fileName };
        using var reader = new ExcelSheetReader(path);

        Func<ExcelSheetReader, ImportFileResult, SqlTransaction?, CancellationToken, Task> importFn = fileName switch
        {
            var f when f.StartsWith("01-") => ImportAppModuleAsync,
            var f when f.StartsWith("02-") => ImportMenuGroupAsync,
            var f when f.StartsWith("03-") => ImportDepartmentAsync,
            var f when f.StartsWith("04-") => ImportPositionAsync,
            var f when f.StartsWith("05-") => ImportDutyAsync,
            var f when f.StartsWith("06-") => ImportPositionDutyAsync,
            var f when f.StartsWith("07-") => ImportUsersAsync,
            var f when f.StartsWith("08-") => ImportMemberAsync,
            var f when f.StartsWith("09-") => ImportUserPositionAsync,
            var f when f.StartsWith("10-") => ImportResourceAsync,
            var f when f.StartsWith("11-") => ImportSubscriptionAsync,
            var f when f.StartsWith("12-") => ImportResourcePermissionAsync,
            var f when f.StartsWith("13-") => ImportEventConfigAsync,
            var f when f.StartsWith("14-") => ImportEventFlowRuleAsync,
            var f when f.StartsWith("15-") => ImportDashIndicatorAsync,
            var f when f.StartsWith("16-") => ImportDashPosPermAsync,
            var f when f.StartsWith("17-") => ImportDashPosTemplateAsync,
            _ => throw new NotSupportedException(fileName)
        };

        await importFn(reader, result, tx, ct);
        return result;
    }

    private async Task ExecAsync(string sql, SqlTransaction? tx, Action<SqlCommand>? bind, CancellationToken ct)
    {
        if (_opt.DryRun) return;
        await using var cmd = new SqlCommand(sql, _conn, tx);
        bind?.Invoke(cmd);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<bool> ExistsAsync(string sql, SqlTransaction? tx, Action<SqlCommand> bind, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, _conn, tx);
        bind(cmd);
        return (await cmd.ExecuteScalarAsync(ct)) != null;
    }

    private async Task ImportAppModuleAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "AppCode")) { r.Skipped++; continue; }
            var code = x.GetString(row, "AppCode");
            var ctx = $"01 行{row} AppCode={code}";
            var exists = await ExistsAsync("SELECT 1 FROM dbo.Tbl_E_AppModule WHERE AppCode=@c", tx,
                c => c.Parameters.AddWithValue("@c", code), ct);
            var sql = exists
                ? @"UPDATE dbo.Tbl_E_AppModule SET AppName=@AppName, AppType=@AppType, BaseUrl=@BaseUrl, Icon=@Icon,
                    DispSeq=@DispSeq, Remark=@Remark, BStatus=@BStatus, IsDeleted=0, AmendDate=GETDATE(), Operator=@Op
                    WHERE AppCode=@AppCode"
                : @"INSERT INTO dbo.Tbl_E_AppModule (AppCode, AppName, AppType, BaseUrl, Icon, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@AppCode, @AppName, @AppType, @BaseUrl, @Icon, @DispSeq, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";
            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@AppCode", code);
                cmd.Parameters.AddWithValue("@AppName", ImportParse.Or(x.GetString(row, "AppName"), code));
                cmd.Parameters.AddWithValue("@AppType", ImportParse.Or(x.GetString(row, "AppType"), "BUSINESS"));
                cmd.Parameters.AddWithValue("@BaseUrl", (object?)NullIfEmpty(x.GetString(row, "BaseUrl")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Icon", (object?)NullIfEmpty(x.GetString(row, "Icon")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DispSeq", ImportParse.Int(x.GetString(row, "DispSeq"), 99));
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, code, exists ? "UPDATE" : "INSERT");
        }
    }

    private async Task ImportMenuGroupAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "MenuGroupCode")) { r.Skipped++; continue; }
            var code = x.GetString(row, "MenuGroupCode");
            var exists = await ExistsAsync("SELECT 1 FROM dbo.Tbl_E_MenuGroup WHERE MenuGroupCode=@c", tx,
                c => c.Parameters.AddWithValue("@c", code), ct);
            var sql = exists
                ? @"UPDATE dbo.Tbl_E_MenuGroup SET AppCode=@AppCode, MenuGroupName=@Name, Icon=@Icon, DispSeq=@DispSeq,
                    Remark=@Remark, BStatus=@BStatus, IsDeleted=0, AmendDate=GETDATE(), Operator=@Op WHERE MenuGroupCode=@Code"
                : @"INSERT INTO dbo.Tbl_E_MenuGroup (MenuGroupCode, AppCode, MenuGroupName, Icon, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@Code, @AppCode, @Name, @Icon, @DispSeq, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";
            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@AppCode", ImportParse.Or(x.GetString(row, "AppCode"), "FRAME"));
                cmd.Parameters.AddWithValue("@Name", ImportParse.Or(x.GetString(row, "MenuGroupName"), code));
                cmd.Parameters.AddWithValue("@Icon", (object?)NullIfEmpty(x.GetString(row, "Icon")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DispSeq", ImportParse.Int(x.GetString(row, "DispSeq"), 99));
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, code, exists ? "UPDATE" : "INSERT");
        }
    }

    private async Task ImportDepartmentAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        var rows = x.DataRows()
            .Select(row => (row, level: ImportParse.Int(x.GetString(row, "DeptLevel"), 1)))
            .Where(t => !x.IsRowEmpty(t.row, "DeptCode"))
            .OrderBy(t => t.level)
            .ThenBy(t => x.GetString(t.row, "DeptCode"))
            .ToList();

        foreach (var (row, _) in rows)
        {
            var code = x.GetString(row, "DeptCode");
            var parentCode = x.GetString(row, "ParentDeptCode");
            int? parentId = null;
            if (!string.IsNullOrWhiteSpace(parentCode))
                parentId = _cache.RequireDept(parentCode, $"03 行{row}");

            var exists = await ExistsAsync("SELECT 1 FROM dbo.Tbl_E_Department WHERE DeptCode=@c AND IsDeleted=0", tx,
                c => c.Parameters.AddWithValue("@c", code), ct);

            var sql = exists
                ? @"UPDATE dbo.Tbl_E_Department SET DeptCName=@CName, DeptEName=@EName, ParentDeptID=@ParentId, DeptLevel=@Level,
                    DeptPath=@Path, DeptType=@Type, DispSeq=@DispSeq, DDescription=@Desc, Remark=@Remark, BStatus=@BStatus,
                    IsDeleted=0, AmendDate=GETDATE(), Operator=@Op WHERE DeptCode=@Code"
                : @"INSERT INTO dbo.Tbl_E_Department (DeptCode, DeptCName, DeptEName, ParentDeptID, DeptLevel, DeptPath, DeptType, DispSeq, DDescription, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@Code, @CName, @EName, @ParentId, @Level, @Path, @Type, @DispSeq, @Desc, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";

            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@CName", ImportParse.Or(x.GetString(row, "DeptCName"), code));
                cmd.Parameters.AddWithValue("@EName", (object?)NullIfEmpty(x.GetString(row, "DeptEName")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ParentId", (object?)parentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Level", ImportParse.Int(x.GetString(row, "DeptLevel"), 1));
                cmd.Parameters.AddWithValue("@Path", (object?)NullIfEmpty(x.GetString(row, "DeptPath")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Type", (object?)NullIfEmpty(x.GetString(row, "DeptType")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DispSeq", ImportParse.Int(x.GetString(row, "DispSeq"), 99));
                cmd.Parameters.AddWithValue("@Desc", (object?)NullIfEmpty(x.GetString(row, "DDescription")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, code, exists ? "UPDATE" : "INSERT");
        }
        await _cache.ReloadDeptsAsync(ct);

        // 第二遍：LeaderLoginId
        foreach (var (row, _) in rows)
        {
            var leader = x.GetString(row, "LeaderLoginId");
            if (string.IsNullOrWhiteSpace(leader)) continue;
            var code = x.GetString(row, "DeptCode");
            var leaderId = _cache.RequireUser(leader, $"03 行{row} LeaderLoginId");
            await ExecAsync(@"UPDATE dbo.Tbl_E_Department SET LeaderUserID=@Uid, AmendDate=GETDATE(), Operator=@Op WHERE DeptCode=@Code", tx,
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Uid", leaderId);
                    cmd.Parameters.AddWithValue("@Code", code);
                    cmd.Parameters.AddWithValue("@Op", _opt.Operator);
                }, ct);
        }
    }

    private async Task ImportPositionAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "PostCode")) { r.Skipped++; continue; }
            var code = x.GetString(row, "PostCode");
            var exists = await ExistsAsync("SELECT 1 FROM dbo.Tbl_E_Position WHERE PostCode=@c", tx,
                c => c.Parameters.AddWithValue("@c", code), ct);
            var sql = exists
                ? @"UPDATE dbo.Tbl_E_Position SET PostCName=@CName, PostEName=@EName, PositionType=@Type, DataScope=@Scope,
                    DispSeq=@DispSeq, DDescription=@Desc, Remark=@Remark, BStatus=@BStatus, IsDeleted=0, AmendDate=GETDATE(), Operator=@Op WHERE PostCode=@Code"
                : @"INSERT INTO dbo.Tbl_E_Position (PostCode, PostCName, PostEName, PositionType, DataScope, DispSeq, DDescription, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@Code, @CName, @EName, @Type, @Scope, @DispSeq, @Desc, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";
            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@CName", ImportParse.Or(x.GetString(row, "PostCName"), code));
                cmd.Parameters.AddWithValue("@EName", (object?)NullIfEmpty(x.GetString(row, "PostEName")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Type", (object?)NullIfEmpty(x.GetString(row, "PositionType")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Scope", ImportParse.Or(x.GetString(row, "DataScope"), "DEPT"));
                cmd.Parameters.AddWithValue("@DispSeq", ImportParse.Int(x.GetString(row, "DispSeq"), 99));
                cmd.Parameters.AddWithValue("@Desc", (object?)NullIfEmpty(x.GetString(row, "DDescription")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, code, exists ? "UPDATE" : "INSERT");
        }
        await _cache.ReloadPostsAsync(ct);
    }

    private async Task ImportDutyAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "DutyCode")) { r.Skipped++; continue; }
            var code = x.GetString(row, "DutyCode");
            var exists = await ExistsAsync("SELECT 1 FROM dbo.Tbl_E_Duty WHERE DutyCode=@c", tx,
                c => c.Parameters.AddWithValue("@c", code), ct);
            var sql = exists
                ? @"UPDATE dbo.Tbl_E_Duty SET DutyCName=@CName, DutyEName=@EName, DutyCategory=@Cat, DutyDispSeq=@DispSeq,
                    DDescription=@Desc, DutyFlow=@Flow, Remark=@Remark, BStatus=@BStatus, IsDeleted=0, AmendDate=GETDATE(), Operator=@Op WHERE DutyCode=@Code"
                : @"INSERT INTO dbo.Tbl_E_Duty (DutyCode, DutyCName, DutyEName, DutyCategory, DutyDispSeq, DDescription, DutyFlow, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@Code, @CName, @EName, @Cat, @DispSeq, @Desc, @Flow, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";
            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@CName", ImportParse.Or(x.GetString(row, "DutyCName"), code));
                cmd.Parameters.AddWithValue("@EName", (object?)NullIfEmpty(x.GetString(row, "DutyEName")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Cat", (object?)NullIfEmpty(x.GetString(row, "DutyCategory")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DispSeq", ImportParse.Int(x.GetString(row, "DutyDispSeq"), 99));
                cmd.Parameters.AddWithValue("@Desc", (object?)NullIfEmpty(x.GetString(row, "DDescription")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Flow", (object?)NullIfEmpty(x.GetString(row, "DutyFlow")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, code, exists ? "UPDATE" : "INSERT");
        }
        await _cache.ReloadDutiesAsync(ct);
    }

    private async Task ImportPositionDutyAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "PostCode", "DutyCode")) { r.Skipped++; continue; }
            var post = x.GetString(row, "PostCode");
            var duty = x.GetString(row, "DutyCode");
            var posId = _cache.RequirePost(post, $"06 行{row}");
            var dutyId = _cache.RequireDuty(duty, $"06 行{row}");
            var deptId = _cache.OptionalDept(x.GetString(row, "DeptCode"));

            var exists = await ExistsAsync(
                @"SELECT 1 FROM dbo.Tbl_E_PositionDuty WHERE PosID=@P AND DutyID=@D AND IsDeleted=0
                  AND ((@Dept IS NULL AND DeptID IS NULL) OR DeptID=@Dept)", tx, cmd =>
                {
                    cmd.Parameters.AddWithValue("@P", posId);
                    cmd.Parameters.AddWithValue("@D", dutyId);
                    cmd.Parameters.AddWithValue("@Dept", (object?)deptId ?? DBNull.Value);
                }, ct);

            var sql = exists
                ? @"UPDATE dbo.Tbl_E_PositionDuty SET BusinessLimit=@BL, DispSeq=@DispSeq, Remark=@Remark, BStatus=@BStatus, IsDeleted=0, AmendDate=GETDATE(), Operator=@Op
                    WHERE PosID=@P AND DutyID=@D AND ((@Dept IS NULL AND DeptID IS NULL) OR DeptID=@Dept)"
                : @"INSERT INTO dbo.Tbl_E_PositionDuty (PosID, DutyID, DeptID, BusinessLimit, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@P, @D, @Dept, @BL, @DispSeq, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";

            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@P", posId);
                cmd.Parameters.AddWithValue("@D", dutyId);
                cmd.Parameters.AddWithValue("@Dept", (object?)deptId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BL", ImportParse.Or(x.GetString(row, "BusinessLimit"), "111111"));
                cmd.Parameters.AddWithValue("@DispSeq", ImportParse.Int(x.GetString(row, "DispSeq"), 1));
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, $"{post}+{duty}", exists ? "UPDATE" : "INSERT");
        }
    }

    private async Task ImportUsersAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "LoginId")) { r.Skipped++; continue; }
            var login = x.GetString(row, "LoginId");
            var plainPwd = ImportParse.Or(x.GetString(row, "InitialPassword"), "123456");
            var algoIn = x.GetString(row, "PasswordAlgo");
            string hash;
            string algo;
            int ver;
            if (string.Equals(algoIn, PasswordHasher.AlgoMd5_16, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(algoIn))
            {
                hash = PasswordHasher.HashMd5_16(plainPwd);
                algo = PasswordHasher.AlgoMd5_16;
                ver = 1;
            }
            else
            {
                (hash, algo, ver) = PasswordHasher.HashForStore(plainPwd);
            }

            var exists = await ExistsAsync("SELECT 1 FROM dbo.Tbl_E_Users WHERE LoginId=@l", tx,
                c => c.Parameters.AddWithValue("@l", login), ct);

            var sql = exists
                ? @"UPDATE dbo.Tbl_E_Users SET RealName=@Name, PwdHash=@Hash, PasswordAlgo=@Algo, PasswordVersion=@Ver,
                    UserType=@UType, MaxLoginCount=@MaxLogin, IsEnabled=@Enabled, Remark=@Remark, BStatus=@BStatus, IsDeleted=0,
                    PwdErrorCount=0, IsLocked=0, AmendDate=GETDATE(), Operator=@Op WHERE LoginId=@Login"
                : @"INSERT INTO dbo.Tbl_E_Users (LoginId, RealName, PwdHash, PasswordAlgo, PasswordVersion, UserType, LoginCount, MaxLoginCount,
                    PwdErrorCount, MaxPwdErrorCount, IsLocked, IsEnabled, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@Login, @Name, @Hash, @Algo, @Ver, @UType, 0, @MaxLogin, 0, 5, 0, @Enabled, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";

            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@Login", login);
                cmd.Parameters.AddWithValue("@Name", ImportParse.Or(x.GetString(row, "RealName"), login));
                cmd.Parameters.AddWithValue("@Hash", hash);
                cmd.Parameters.AddWithValue("@Algo", algo);
                cmd.Parameters.AddWithValue("@Ver", ver);
                cmd.Parameters.AddWithValue("@UType", ImportParse.Or(x.GetString(row, "UserType"), "EMPLOYEE"));
                cmd.Parameters.AddWithValue("@MaxLogin", ImportParse.Int(x.GetString(row, "MaxLoginCount"), 99999));
                cmd.Parameters.AddWithValue("@Enabled", ImportParse.Bit(x.GetString(row, "IsEnabled"), true));
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            _issuedAccounts.Add((login, plainPwd));
            Track(r, row, login, exists ? "UPDATE" : "INSERT");
        }
        await _cache.ReloadUsersAsync(ct);
    }

    private async Task ImportMemberAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "MemberID")) { r.Skipped++; continue; }
            var mid = x.GetString(row, "MemberID");
            var userId = _cache.OptionalUser(x.GetString(row, "LoginId"));
            var deptId = _cache.OptionalDept(x.GetString(row, "DefaultDeptCode"));
            var posId = string.IsNullOrWhiteSpace(x.GetString(row, "DefaultPostCode")) ? null :
                (int?)_cache.RequirePost(x.GetString(row, "DefaultPostCode"), $"08 行{row}");

            var exists = await ExistsAsync("SELECT 1 FROM dbo.Tbl_E_Member WHERE MemberID=@m", tx,
                c => c.Parameters.AddWithValue("@m", mid), ct);

            var sql = exists
                ? @"UPDATE dbo.Tbl_E_Member SET MemberName=@Name, UserID=@Uid, Sex=@Sex, Mobile=@Mobile, EMail=@Email,
                    DefaultDeptID=@Dept, DefaultPosID=@Pos, Remark=@Remark, BStatus=@BStatus, IsDeleted=0, AmendDate=GETDATE(), Operator=@Op WHERE MemberID=@Mid"
                : @"INSERT INTO dbo.Tbl_E_Member (MemberID, MemberName, UserID, Sex, Mobile, EMail, DefaultDeptID, DefaultPosID, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@Mid, @Name, @Uid, @Sex, @Mobile, @Email, @Dept, @Pos, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";

            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@Mid", mid);
                cmd.Parameters.AddWithValue("@Name", ImportParse.Or(x.GetString(row, "MemberName"), mid));
                cmd.Parameters.AddWithValue("@Uid", (object?)userId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Sex", (object?)NullIfEmpty(x.GetString(row, "Sex")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Mobile", (object?)NullIfEmpty(x.GetString(row, "Mobile")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", (object?)NullIfEmpty(x.GetString(row, "EMail")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Dept", (object?)deptId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Pos", (object?)posId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, mid, exists ? "UPDATE" : "INSERT");
        }
        await _cache.ReloadMembersAsync(ct);
    }

    private async Task ImportUserPositionAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "LoginId", "DeptCode", "PostCode")) { r.Skipped++; continue; }
            var login = x.GetString(row, "LoginId");
            var userId = _cache.RequireUser(login, $"09 行{row}");
            var deptId = _cache.RequireDept(x.GetString(row, "DeptCode"), $"09 行{row}");
            var posId = _cache.RequirePost(x.GetString(row, "PostCode"), $"09 行{row}");
            var memberId = _cache.OptionalMember(x.GetString(row, "MemberID"));

            var exists = await ExistsAsync(
                @"SELECT 1 FROM dbo.Tbl_E_UserPosition WHERE UserID=@U AND DeptID=@D AND PosID=@P AND IsDeleted=0", tx, cmd =>
                {
                    cmd.Parameters.AddWithValue("@U", userId);
                    cmd.Parameters.AddWithValue("@D", deptId);
                    cmd.Parameters.AddWithValue("@P", posId);
                }, ct);

            var sql = exists
                ? @"UPDATE dbo.Tbl_E_UserPosition SET MemberID=@M, IsPrimary=@Primary, BeginDate=@Begin, EndDate=@End, Remark=@Remark,
                    BStatus=@BStatus, IsDeleted=0, AmendDate=GETDATE(), Operator=@Op WHERE UserID=@U AND DeptID=@D AND PosID=@P AND IsDeleted=0"
                : @"INSERT INTO dbo.Tbl_E_UserPosition (UserID, MemberID, DeptID, PosID, IsPrimary, BeginDate, EndDate, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@U, @M, @D, @P, @Primary, @Begin, @End, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";

            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@U", userId);
                cmd.Parameters.AddWithValue("@M", (object?)memberId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@D", deptId);
                cmd.Parameters.AddWithValue("@P", posId);
                cmd.Parameters.AddWithValue("@Primary", ImportParse.Bit(x.GetString(row, "IsPrimary"), false));
                cmd.Parameters.AddWithValue("@Begin", (object?)ImportParse.Date(x.GetString(row, "BeginDate")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@End", (object?)ImportParse.Date(x.GetString(row, "EndDate")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, $"{login}@{x.GetString(row, "DeptCode")}/{x.GetString(row, "PostCode")}", exists ? "UPDATE" : "INSERT");
        }
    }

    private async Task ImportResourceAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "ResourceID")) { r.Skipped++; continue; }
            var rid = x.GetString(row, "ResourceID");
            var exists = await ExistsAsync("SELECT 1 FROM dbo.Tbl_E_Resource WHERE ResourceID=@r", tx,
                c => c.Parameters.AddWithValue("@r", rid), ct);
            var sql = exists
                ? @"UPDATE dbo.Tbl_E_Resource SET AppCode=@App, ResourceName=@Name, ResourceType=@Type, MenuPath=@Path,
                    MenuGroupCode=@Grp, ParentResourceID=@Parent, Icon=@Icon, DispSeq=@DispSeq, Remark=@Remark, BStatus=@BStatus,
                    IsDeleted=0, AmendDate=GETDATE(), Operator=@Op WHERE ResourceID=@Rid"
                : @"INSERT INTO dbo.Tbl_E_Resource (AppCode, ResourceID, ResourceName, ResourceType, MenuPath, MenuGroupCode, ParentResourceID, Icon, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@App, @Rid, @Name, @Type, @Path, @Grp, @Parent, @Icon, @DispSeq, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";
            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@Rid", rid);
                cmd.Parameters.AddWithValue("@App", ImportParse.Or(x.GetString(row, "AppCode"), "FRAME"));
                cmd.Parameters.AddWithValue("@Name", ImportParse.Or(x.GetString(row, "ResourceName"), rid));
                cmd.Parameters.AddWithValue("@Type", ImportParse.Or(x.GetString(row, "ResourceType"), "MENU"));
                cmd.Parameters.AddWithValue("@Path", (object?)NullIfEmpty(x.GetString(row, "MenuPath")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Grp", (object?)NullIfEmpty(x.GetString(row, "MenuGroupCode")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Parent", (object?)NullIfEmpty(x.GetString(row, "ParentResourceID")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Icon", (object?)NullIfEmpty(x.GetString(row, "Icon")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DispSeq", ImportParse.Int(x.GetString(row, "DispSeq"), 99));
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, rid, exists ? "UPDATE" : "INSERT");
        }
    }

    private async Task ImportSubscriptionAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "DutyCode", "SubType")) { r.Skipped++; continue; }
            var duty = x.GetString(row, "DutyCode");
            var subType = ImportParse.Or(x.GetString(row, "SubType"), "RESOURCE").ToUpperInvariant();
            var dutyId = _cache.RequireDuty(duty, $"11 行{row}");
            var resourceId = NullIfEmpty(x.GetString(row, "ResourceID"));
            var eventCode = NullIfEmpty(x.GetString(row, "EventCode"));
            var deptId = _cache.OptionalDept(x.GetString(row, "DeptCode"));

            if (subType is "RESOURCE" or "MIXED" && string.IsNullOrEmpty(resourceId))
                throw new InvalidOperationException($"11 行{row}: SubType={subType} 需要 ResourceID");
            if (subType is "EVENT" or "MIXED" && string.IsNullOrEmpty(eventCode) && subType == "EVENT")
                throw new InvalidOperationException($"11 行{row}: SubType=EVENT 需要 EventCode");

            var exists = await ExistsAsync(
                @"SELECT 1 FROM dbo.Tbl_E_Subscription WHERE DutyID=@D AND SubType=@S AND IsDeleted=0
                  AND ISNULL(ResourceID,'')=ISNULL(@R,'') AND ISNULL(EventCode,'')=ISNULL(@E,'')", tx, cmd =>
                {
                    cmd.Parameters.AddWithValue("@D", dutyId);
                    cmd.Parameters.AddWithValue("@S", subType);
                    cmd.Parameters.AddWithValue("@R", (object?)resourceId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@E", (object?)eventCode ?? DBNull.Value);
                }, ct);

            var sql = exists
                ? @"UPDATE dbo.Tbl_E_Subscription SET AppCode=@App, DeptID=@Dept, IsPrimary=@Primary, FunctionLimit=@FL,
                    ConditionExpr=@Cond, NotifyMode=@Notify, DispSeq=@DispSeq, Remark=@Remark, BStatus=@BStatus, IsDeleted=0, AmendDate=GETDATE(), Operator=@Op
                    WHERE DutyID=@D AND SubType=@S AND ISNULL(ResourceID,'')=ISNULL(@R,'') AND ISNULL(EventCode,'')=ISNULL(@E,'') AND IsDeleted=0"
                : @"INSERT INTO dbo.Tbl_E_Subscription (DutyID, AppCode, SubType, EventCode, ResourceID, DeptID, IsPrimary, FunctionLimit, ConditionExpr, NotifyMode, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@D, @App, @S, @E, @R, @Dept, @Primary, @FL, @Cond, @Notify, @DispSeq, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";

            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@D", dutyId);
                cmd.Parameters.AddWithValue("@App", (object?)NullIfEmpty(x.GetString(row, "AppCode")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@S", subType);
                cmd.Parameters.AddWithValue("@E", (object?)eventCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@R", (object?)resourceId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Dept", (object?)deptId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Primary", ImportParse.Bit(x.GetString(row, "IsPrimary"), true));
                cmd.Parameters.AddWithValue("@FL", (object?)NullIfEmpty(x.GetString(row, "FunctionLimit")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Cond", (object?)NullIfEmpty(x.GetString(row, "ConditionExpr")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Notify", (object?)NullIfEmpty(x.GetString(row, "NotifyMode")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DispSeq", ImportParse.Int(x.GetString(row, "DispSeq"), 50));
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, $"{duty}/{subType}", exists ? "UPDATE" : "INSERT");
        }
    }

    private async Task ImportResourcePermissionAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "DutyCode", "ResourceID")) { r.Skipped++; continue; }
            var dutyId = _cache.RequireDuty(x.GetString(row, "DutyCode"), $"12 行{row}");
            var rid = x.GetString(row, "ResourceID");
            var exists = await ExistsAsync(
                "SELECT 1 FROM dbo.Tbl_E_ResourcePermission WHERE DutyID=@D AND ResourceID=@R AND IsDeleted=0", tx, cmd =>
                {
                    cmd.Parameters.AddWithValue("@D", dutyId);
                    cmd.Parameters.AddWithValue("@R", rid);
                }, ct);

            var sql = exists
                ? @"UPDATE dbo.Tbl_E_ResourcePermission SET CanQuery=@Q, CanCreate=@C, CanUpdate=@U, CanDelete=@Del, CanExport=@Ex, CanImport=@Im,
                    Remark=@Remark, BStatus=@BStatus, IsDeleted=0, AmendDate=GETDATE(), Operator=@Op WHERE DutyID=@D AND ResourceID=@R AND IsDeleted=0"
                : @"INSERT INTO dbo.Tbl_E_ResourcePermission (DutyID, ResourceID, CanCreate, CanUpdate, CanDelete, CanQuery, CanExport, CanImport, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@D, @R, @C, @U, @Del, @Q, @Ex, @Im, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";

            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@D", dutyId);
                cmd.Parameters.AddWithValue("@R", rid);
                cmd.Parameters.AddWithValue("@Q", ImportParse.Bit(x.GetString(row, "CanQuery"), true));
                cmd.Parameters.AddWithValue("@C", ImportParse.Bit(x.GetString(row, "CanCreate"), false));
                cmd.Parameters.AddWithValue("@U", ImportParse.Bit(x.GetString(row, "CanUpdate"), false));
                cmd.Parameters.AddWithValue("@Del", ImportParse.Bit(x.GetString(row, "CanDelete"), false));
                cmd.Parameters.AddWithValue("@Ex", ImportParse.Bit(x.GetString(row, "CanExport"), false));
                cmd.Parameters.AddWithValue("@Im", ImportParse.Bit(x.GetString(row, "CanImport"), false));
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, $"{x.GetString(row, "DutyCode")}+{rid}", exists ? "UPDATE" : "INSERT");
        }
    }

    private async Task ImportEventConfigAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "EventCode")) { r.Skipped++; continue; }
            var app = ImportParse.Or(x.GetString(row, "AppCode"), "FRAME");
            var ev = x.GetString(row, "EventCode");
            var exists = await ExistsAsync(
                "SELECT 1 FROM dbo.Tbl_E_EventConfig WHERE AppCode=@A AND EventCode=@E AND IsDeleted=0", tx, cmd =>
                {
                    cmd.Parameters.AddWithValue("@A", app);
                    cmd.Parameters.AddWithValue("@E", ev);
                }, ct);

            var sql = exists
                ? @"UPDATE dbo.Tbl_E_EventConfig SET EventName=@Name, EventType=@Type, PageUrl=@Url, MenuGroupCode=@Grp, ExecType=@Exec,
                    IsGenerateTodo=@Todo, TodoTitle=@Title, HandleMode=@Mode, DefaultDueMinutes=@Due, DispSeq=@DispSeq, Remark=@Remark,
                    BStatus=@BStatus, IsDeleted=0, AmendDate=GETDATE(), Operator=@Op WHERE AppCode=@App AND EventCode=@Ev"
                : @"INSERT INTO dbo.Tbl_E_EventConfig (AppCode, EventCode, EventName, EventType, PageUrl, MenuGroupCode, ExecType, IsGenerateTodo, TodoTitle, HandleMode, DefaultDueMinutes, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@App, @Ev, @Name, @Type, @Url, @Grp, @Exec, @Todo, @Title, @Mode, @Due, @DispSeq, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";

            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@App", app);
                cmd.Parameters.AddWithValue("@Ev", ev);
                cmd.Parameters.AddWithValue("@Name", ImportParse.Or(x.GetString(row, "EventName"), ev));
                cmd.Parameters.AddWithValue("@Type", (object?)NullIfEmpty(x.GetString(row, "EventType")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Url", (object?)NullIfEmpty(x.GetString(row, "PageUrl")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Grp", (object?)NullIfEmpty(x.GetString(row, "MenuGroupCode")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Exec", ImportParse.Or(x.GetString(row, "ExecType"), "ASYNC"));
                cmd.Parameters.AddWithValue("@Todo", ImportParse.Bit(x.GetString(row, "IsGenerateTodo"), true));
                cmd.Parameters.AddWithValue("@Title", (object?)NullIfEmpty(x.GetString(row, "TodoTitle")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Mode", ImportParse.Or(x.GetString(row, "HandleMode"), "SINGLE"));
                var due = ImportParse.Int(x.GetString(row, "DefaultDueMinutes"), 0);
                cmd.Parameters.AddWithValue("@Due", due > 0 ? due : DBNull.Value);
                cmd.Parameters.AddWithValue("@DispSeq", ImportParse.Int(x.GetString(row, "DispSeq"), 10));
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, ev, exists ? "UPDATE" : "INSERT");
        }
    }

    private async Task ImportEventFlowRuleAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "RuleCode")) { r.Skipped++; continue; }
            var rule = x.GetString(row, "RuleCode");
            var exists = await ExistsAsync("SELECT 1 FROM dbo.Tbl_E_EventFlowRule WHERE RuleCode=@R AND IsDeleted=0", tx,
                c => c.Parameters.AddWithValue("@R", rule), ct);

            int? dutyId = null;
            int? posId = null;
            int? deptId = null;
            int? userId = null;
            var tDuty = x.GetString(row, "TargetDutyCode");
            var tPost = x.GetString(row, "TargetPostCode");
            var tDept = x.GetString(row, "TargetDeptCode");
            var tLogin = x.GetString(row, "TargetLoginId");
            if (!string.IsNullOrWhiteSpace(tDuty)) dutyId = _cache.RequireDuty(tDuty, $"14 行{row}");
            if (!string.IsNullOrWhiteSpace(tPost)) posId = _cache.RequirePost(tPost, $"14 行{row}");
            if (!string.IsNullOrWhiteSpace(tDept)) deptId = _cache.RequireDept(tDept, $"14 行{row}");
            if (!string.IsNullOrWhiteSpace(tLogin)) userId = _cache.RequireUser(tLogin, $"14 行{row}");

            var sql = exists
                ? @"UPDATE dbo.Tbl_E_EventFlowRule SET RuleName=@Name, AppCode=@App, CurrentEvent=@Cur, NextEvent=@Next, ConditionExpr=@Cond,
                    ActionType=@Act, TargetResolveType=@TType, TargetDutyID=@TDuty, TargetPosID=@TPos, TargetDeptID=@TDept, TargetUserID=@TUser,
                    HandleMode=@Mode, TimeoutMinutes=@Timeout, DispSeq=@DispSeq, Remark=@Remark, BStatus=@BStatus, IsDeleted=0, AmendDate=GETDATE(), Operator=@Op
                    WHERE RuleCode=@Rule"
                : @"INSERT INTO dbo.Tbl_E_EventFlowRule (RuleCode, RuleName, AppCode, CurrentEvent, NextEvent, ConditionExpr, ActionType, TargetResolveType,
                    TargetDutyID, TargetPosID, TargetDeptID, TargetUserID, HandleMode, TimeoutMinutes, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@Rule, @Name, @App, @Cur, @Next, @Cond, @Act, @TType, @TDuty, @TPos, @TDept, @TUser, @Mode, @Timeout, @DispSeq, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";

            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@Rule", rule);
                cmd.Parameters.AddWithValue("@Name", ImportParse.Or(x.GetString(row, "RuleName"), rule));
                cmd.Parameters.AddWithValue("@App", ImportParse.Or(x.GetString(row, "AppCode"), "FRAME"));
                cmd.Parameters.AddWithValue("@Cur", ImportParse.Or(x.GetString(row, "CurrentEvent"), ""));
                cmd.Parameters.AddWithValue("@Next", (object?)NullIfEmpty(x.GetString(row, "NextEvent")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Cond", (object?)NullIfEmpty(x.GetString(row, "ConditionExpr")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Act", ImportParse.Or(x.GetString(row, "ActionType"), "CREATE_TODO"));
                cmd.Parameters.AddWithValue("@TType", (object?)NullIfEmpty(x.GetString(row, "TargetResolveType")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TDuty", (object?)dutyId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TPos", (object?)posId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TDept", (object?)deptId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TUser", (object?)userId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Mode", ImportParse.Or(x.GetString(row, "HandleMode"), "SINGLE"));
                var timeout = ImportParse.Int(x.GetString(row, "TimeoutMinutes"), 0);
                cmd.Parameters.AddWithValue("@Timeout", timeout > 0 ? timeout : DBNull.Value);
                cmd.Parameters.AddWithValue("@DispSeq", ImportParse.Int(x.GetString(row, "DispSeq"), 10));
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, rule, exists ? "UPDATE" : "INSERT");
        }
    }

    private async Task ImportDashIndicatorAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "IndicatorCode")) { r.Skipped++; continue; }
            var code = x.GetString(row, "IndicatorCode");
            var exists = await ExistsAsync("SELECT 1 FROM dbo.Tbl_Dash_Indicator WHERE IndicatorCode=@c AND IsDeleted=0", tx,
                c => c.Parameters.AddWithValue("@c", code), ct);
            var sql = exists
                ? @"UPDATE dbo.Tbl_Dash_Indicator SET IndicatorName=@Name, AppCode=@App, ChartType=@Chart, DataSource=@Src, CalcRule=@Calc,
                    DefaultTimeScope=@Scope, IsLockCalc=@Lock, DispSeq=@DispSeq, Remark=@Remark, BStatus=@BStatus, IsDeleted=0, AmendDate=GETDATE(), Operator=@Op WHERE IndicatorCode=@Code"
                : @"INSERT INTO dbo.Tbl_Dash_Indicator (IndicatorCode, IndicatorName, AppCode, ChartType, DataSource, CalcRule, DefaultTimeScope, IsLockCalc, DispSeq, Remark, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@Code, @Name, @App, @Chart, @Src, @Calc, @Scope, @Lock, @DispSeq, @Remark, @BStatus, 0, GETDATE(), GETDATE(), @Op)";
            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@Name", ImportParse.Or(x.GetString(row, "IndicatorName"), code));
                cmd.Parameters.AddWithValue("@App", ImportParse.Or(x.GetString(row, "AppCode"), "FRAME"));
                cmd.Parameters.AddWithValue("@Chart", (byte)ImportParse.Int(x.GetString(row, "ChartType"), 1));
                cmd.Parameters.AddWithValue("@Src", ImportParse.Or(x.GetString(row, "DataSource"), "CUSTOM"));
                cmd.Parameters.AddWithValue("@Calc", ImportParse.Or(x.GetString(row, "CalcRule"), "{}"));
                cmd.Parameters.AddWithValue("@Scope", (byte)ImportParse.Int(x.GetString(row, "DefaultTimeScope"), 3));
                cmd.Parameters.AddWithValue("@Lock", ImportParse.Bit(x.GetString(row, "IsLockCalc"), true));
                cmd.Parameters.AddWithValue("@DispSeq", ImportParse.Int(x.GetString(row, "DispSeq"), 99));
                cmd.Parameters.AddWithValue("@Remark", (object?)NullIfEmpty(x.GetString(row, "Remark")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, code, exists ? "UPDATE" : "INSERT");
        }
        await _cache.ReloadIndicatorsAsync(ct);
    }

    private async Task ImportDashPosPermAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "PostCode", "IndicatorCode")) { r.Skipped++; continue; }
            var posId = _cache.RequirePost(x.GetString(row, "PostCode"), $"16 行{row}");
            var indId = _cache.RequireIndicator(x.GetString(row, "IndicatorCode"), $"16 行{row}");
            var exists = await ExistsAsync("SELECT 1 FROM dbo.Tbl_Dash_PosIndicatorPerm WHERE PosID=@P AND IndicatorID=@I", tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@P", posId);
                cmd.Parameters.AddWithValue("@I", indId);
            }, ct);
            var sql = exists
                ? @"UPDATE dbo.Tbl_Dash_PosIndicatorPerm SET BStatus=@BStatus, AmendDate=GETDATE(), Operator=@Op WHERE PosID=@P AND IndicatorID=@I"
                : @"INSERT INTO dbo.Tbl_Dash_PosIndicatorPerm (PosID, IndicatorID, BStatus, CreateDate, AmendDate, Operator) VALUES (@P, @I, @BStatus, GETDATE(), GETDATE(), @Op)";
            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@P", posId);
                cmd.Parameters.AddWithValue("@I", indId);
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, $"{x.GetString(row, "PostCode")}+{x.GetString(row, "IndicatorCode")}", exists ? "UPDATE" : "INSERT");
        }
    }

    private async Task ImportDashPosTemplateAsync(ExcelSheetReader x, ImportFileResult r, SqlTransaction? tx, CancellationToken ct)
    {
        foreach (var row in x.DataRows())
        {
            if (x.IsRowEmpty(row, "PostCode", "IndicatorCode")) { r.Skipped++; continue; }
            var posId = _cache.RequirePost(x.GetString(row, "PostCode"), $"17 行{row}");
            var indId = _cache.RequireIndicator(x.GetString(row, "IndicatorCode"), $"17 行{row}");
            var exists = await ExistsAsync(
                "SELECT 1 FROM dbo.Tbl_Dash_PosTemplate WHERE PosID=@P AND IndicatorID=@I AND IsDeleted=0", tx, cmd =>
                {
                    cmd.Parameters.AddWithValue("@P", posId);
                    cmd.Parameters.AddWithValue("@I", indId);
                }, ct);
            var sql = exists
                ? @"UPDATE dbo.Tbl_Dash_PosTemplate SET LayoutRow=@Row, LayoutCol=@Col, ColSpan=@Span, IsLock=@Lock, DefaultFilterJson=@Filter,
                    CardTitle=@Title, DispSeq=@DispSeq, BStatus=@BStatus, IsDeleted=0, AmendDate=GETDATE(), Operator=@Op WHERE PosID=@P AND IndicatorID=@I AND IsDeleted=0"
                : @"INSERT INTO dbo.Tbl_Dash_PosTemplate (PosID, IndicatorID, LayoutRow, LayoutCol, ColSpan, IsLock, DefaultFilterJson, CardTitle, DispSeq, BStatus, IsDeleted, CreateDate, AmendDate, Operator)
                    VALUES (@P, @I, @Row, @Col, @Span, @Lock, @Filter, @Title, @DispSeq, @BStatus, 0, GETDATE(), GETDATE(), @Op)";
            await ExecAsync(sql, tx, cmd =>
            {
                cmd.Parameters.AddWithValue("@P", posId);
                cmd.Parameters.AddWithValue("@I", indId);
                cmd.Parameters.AddWithValue("@Row", ImportParse.Int(x.GetString(row, "LayoutRow"), 1));
                cmd.Parameters.AddWithValue("@Col", ImportParse.Int(x.GetString(row, "LayoutCol"), 1));
                cmd.Parameters.AddWithValue("@Span", (byte)ImportParse.Int(x.GetString(row, "ColSpan"), 2));
                cmd.Parameters.AddWithValue("@Lock", ImportParse.Bit(x.GetString(row, "IsLock"), false));
                cmd.Parameters.AddWithValue("@Filter", (object?)NullIfEmpty(x.GetString(row, "DefaultFilterJson")) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Title", ImportParse.Or(x.GetString(row, "CardTitle"), x.GetString(row, "IndicatorCode")));
                cmd.Parameters.AddWithValue("@DispSeq", ImportParse.Int(x.GetString(row, "DispSeq"), 99));
                cmd.Parameters.AddWithValue("@BStatus", ImportParse.Or(x.GetString(row, "BStatus"), "1"));
                cmd.Parameters.AddWithValue("@Op", _opt.Operator);
            }, ct);
            Track(r, row, $"{x.GetString(row, "PostCode")}+{x.GetString(row, "IndicatorCode")}", exists ? "UPDATE" : "INSERT");
        }
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static void Track(ImportFileResult r, int row, string key, string action)
    {
        if (action == "INSERT") r.Inserted++;
        else if (action == "UPDATE") r.Updated++;
        r.Rows.Add(new ImportRowResult { RowNumber = row, Key = key, Action = action });
    }

    public static string FormatReport(IReadOnlyList<ImportFileResult> results, IReadOnlyList<(string LoginId, string Password)> accounts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("========== EFrame 数据导入报告 ==========");
        sb.AppendLine("【重要】导入已覆盖原有组织与用户，请务必：");
        sb.AppendLine("  1. 通知所有在线用户 退出登录");
        sb.AppendLine("  2. 使用下方 Excel 07 表中的 LoginId + 初始密码 重新登录");
        sb.AppendLine("  3. 若有多任岗，登录后在首页切换任岗");
        sb.AppendLine();
        foreach (var fr in results)
        {
            sb.AppendLine($"{fr.FileName}: 新增 {fr.Inserted}, 更新 {fr.Updated}, 跳过 {fr.Skipped}");
            foreach (var err in fr.Errors)
                sb.AppendLine("  错误: " + err);
        }
        if (accounts.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("--- 账号发放清单（07 表 InitialPassword）---");
            foreach (var (login, pwd) in accounts)
                sb.AppendLine($"  {login}\t{pwd}");
        }
        sb.AppendLine("=========================================");
        return sb.ToString();
    }
}
