using System.Text.RegularExpressions;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>
/// 人员档案（Tbl_E_Member）。性别/学历/职级/健康从字典读取（MEMBER_*）；无字典时回退内置选项。
/// 身份证：列表与详情仅脱敏展示；搜索白名单允许按证件号模糊匹配。
/// </summary>
public class EMemberService
{
    private readonly FrameworkDbContext _db;
    private readonly DictService _dict;

    public EMemberService(FrameworkDbContext db, DictService dict)
    {
        _db = db;
        _dict = dict;
    }

    public string Normalize(string? value, int maxLen, string fallback = "")
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) v = fallback;
        return v.Length <= maxLen ? v : v[..maxLen];
    }

    public static string MaskIdNo(string? raw)
    {
        var s = (raw ?? "").Trim();
        if (s.Length == 0) return "-";
        if (s.Length <= 8) return "********";
        var head = Math.Min(4, s.Length);
        var tail = Math.Min(4, s.Length - head);
        if (tail <= 0) return new string('*', s.Length);
        var midLen = s.Length - head - tail;
        return s[..head] + new string('*', midLen) + s[^tail..];
    }

    public Task<List<(string Value, string Text)>> GetSexFilterOptionsAsync(CancellationToken ct) =>
        LoadSelectOptionsAsync(DictCodes.MemberSex, forFilter: true, formEmptyLabel: null, FallbackSexFilterOptions(), ct);

    public Task<List<(string Value, string Text)>> GetSexFormOptionsAsync(CancellationToken ct) =>
        LoadSelectOptionsAsync(DictCodes.MemberSex, forFilter: false, formEmptyLabel: "（未填）", FallbackSexFormOptions(), ct);

    public Task<List<(string Value, string Text)>> GetPerGradeFilterOptionsAsync(CancellationToken ct) =>
        LoadSelectOptionsAsync(DictCodes.MemberPerGrade, forFilter: true, formEmptyLabel: null, FallbackPerGradeFilterOptions(), ct);

    public Task<List<(string Value, string Text)>> GetPerGradeFormOptionsAsync(CancellationToken ct) =>
        LoadSelectOptionsAsync(DictCodes.MemberPerGrade, forFilter: false, formEmptyLabel: "（未填）", FallbackPerGradeFormOptions(), ct);

    public Task<List<(string Value, string Text)>> GetHealthFormOptionsAsync(CancellationToken ct) =>
        LoadSelectOptionsAsync(DictCodes.MemberHealth, forFilter: false, formEmptyLabel: "（未填）", FallbackHealthFormOptions(), ct);

    public Task<List<(string Value, string Text)>> GetELevelFormOptionsAsync(CancellationToken ct) =>
        LoadSelectOptionsAsync(DictCodes.MemberEduLevel, forFilter: false, formEmptyLabel: "（未填）", FallbackELevelFormOptions(), ct);

    private async Task<List<(string Value, string Text)>> LoadSelectOptionsAsync(
        string dictTypeCode,
        bool forFilter,
        string? formEmptyLabel,
        List<(string Value, string Text)> fallback,
        CancellationToken ct)
    {
        var opts = await _dict.GetSelectOptionsAsync(dictTypeCode, forFilter, formEmptyLabel, ct: ct);
        return opts.Count > (forFilter ? 1 : 0) ? opts : fallback;
    }

    private static string DictDisplay(string? code, IReadOnlyDictionary<string, string> map) =>
        string.IsNullOrWhiteSpace(code)
            ? "-"
            : map.TryGetValue(code.Trim(), out var name) ? name : code.Trim();

    private static List<(string Value, string Text)> FallbackSexFilterOptions() =>
    [
        ("", "全部"), ("男", "男"), ("女", "女")
    ];

    private static List<(string Value, string Text)> FallbackSexFormOptions() =>
    [
        ("", "（未填）"), ("男", "男"), ("女", "女")
    ];

    private static List<(string Value, string Text)> FallbackPerGradeFilterOptions()
    {
        var grades = new[] { "员级", "主办", "主管", "高级主管", "经理", "其它" };
        var list = new List<(string, string)> { ("", "全部") };
        list.AddRange(grades.Select(g => (g, g)));
        return list;
    }

    private static List<(string Value, string Text)> FallbackPerGradeFormOptions()
    {
        var list = new List<(string, string)> { ("", "（未填）") };
        list.AddRange(FallbackPerGradeFilterOptions().Where(x => x.Value != ""));
        return list;
    }

    private static List<(string Value, string Text)> FallbackHealthFormOptions() =>
    [
        ("", "（未填）"), ("健康", "健康"), ("良好", "良好"), ("一般", "一般"),
        ("慢性病", "慢性病"), ("其它", "其它")
    ];

    private static List<(string Value, string Text)> FallbackELevelFormOptions() =>
    [
        ("", "（未填）"), ("小学", "小学"), ("初中", "初中"), ("高中", "高中"),
        ("中专", "中专"), ("大专", "大专"), ("本科", "本科"), ("硕士", "硕士"),
        ("博士", "博士"), ("其它", "其它")
    ];

    public async Task<List<(string Value, string Text)>> GetDeptOptionsForFormAsync(CancellationToken ct)
    {
        var rows = await _db.EDepartments.AsNoTracking()
            .Where(x => x.BStatus == "启用" || x.BStatus == "1")
            .OrderBy(x => x.DeptCode)
            .Select(x => new { x.DataId, x.DeptCode, x.DeptCName })
            .ToListAsync(ct);
        var list = new List<(string, string)> { ("", "（未选）") };
        list.AddRange(rows.Select(x => (x.DataId.ToString(), $"{x.DeptCode} - {x.DeptCName}")));
        return list;
    }

    public async Task<List<(string Value, string Text)>> GetPosOptionsForFormAsync(CancellationToken ct)
    {
        var rows = await _db.EPositions.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.BStatus == "启用" || x.BStatus == "1"))
            .OrderBy(x => x.PostCode)
            .Select(x => new { x.DataId, x.PostCode, x.PostCName })
            .ToListAsync(ct);
        var list = new List<(string, string)> { ("", "（未选）") };
        list.AddRange(rows.Select(x => (x.DataId.ToString(), $"{x.PostCode} - {x.PostCName}")));
        return list;
    }

    public async Task<List<(string Value, string Text)>> GetDeptFilterOptionsAsync(CancellationToken ct)
    {
        var rows = await _db.EDepartments.AsNoTracking()
            .Where(x => x.BStatus == "启用" || x.BStatus == "1")
            .OrderBy(x => x.DeptCode)
            .Select(x => new { x.DataId, x.DeptCode, x.DeptCName })
            .ToListAsync(ct);
        var list = new List<(string, string)> { ("", "全部") };
        list.AddRange(rows.Select(x => (x.DataId.ToString(), $"{x.DeptCode} - {x.DeptCName}")));
        return list;
    }

    public async Task<List<(string Value, string Text)>> GetPosFilterOptionsAsync(CancellationToken ct)
    {
        var rows = await _db.EPositions.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.BStatus == "启用" || x.BStatus == "1"))
            .OrderBy(x => x.PostCode)
            .Select(x => new { x.DataId, x.PostCode, x.PostCName })
            .ToListAsync(ct);
        var list = new List<(string, string)> { ("", "全部") };
        list.AddRange(rows.Select(x => (x.DataId.ToString(), $"{x.PostCode} - {x.PostCName}")));
        return list;
    }

    public EMemberFormVm ToForm(EMember row) => new()
    {
        DataId = row.DataId,
        MemberId = row.MemberId ?? "",
        MemberName = row.MemberName ?? "",
        Sex = row.Sex,
        Nation = row.Nation,
        NativePlace = row.NativePlace,
        IdNo = row.IdNo,
        Birthday = row.Birthday,
        ELevel = row.ELevel,
        Degree = row.Degree,
        School = row.School,
        Speciality = row.Speciality,
        ComputerAbility = row.ComputerAbility,
        WorkDate = row.WorkDate,
        PerGrade = row.PerGrade,
        DeptId = row.DeptId,
        PosId = row.PosId,
        HomeAddr = row.HomeAddr,
        Mobile = row.Mobile,
        HomePhone = row.HomePhone,
        WorkPhone = row.WorkPhone,
        WxNum = row.WxNum,
        QqNum = row.QqNum,
        EMail = row.EMail,
        Health = row.Health,
        Remark = row.Remark
    };

    public void NormalizeFormForSave(EMemberFormVm model, bool isEdit, string? lockedMemberId)
    {
        if (!isEdit)
            model.MemberId = Normalize(model.MemberId, 30);
        else if (!string.IsNullOrEmpty(lockedMemberId))
            model.MemberId = lockedMemberId;

        model.MemberName = Normalize(model.MemberName, 20);
        model.Sex = NullIfEmpty(Normalize(model.Sex, 2));
        model.Nation = NullIfEmpty(Normalize(model.Nation, 12));
        model.NativePlace = NullIfEmpty(Normalize(model.NativePlace, 30));
        model.IdNo = NullIfEmpty(Normalize(model.IdNo, 20));
        model.ELevel = NullIfEmpty(Normalize(model.ELevel, 12));
        model.Degree = NullIfEmpty(Normalize(model.Degree, 20));
        model.School = NullIfEmpty(Normalize(model.School, 40));
        model.Speciality = NullIfEmpty(Normalize(model.Speciality, 30));
        model.ComputerAbility = NullIfEmpty(Normalize(model.ComputerAbility, 30));
        model.PerGrade = NullIfEmpty(Normalize(model.PerGrade, 10));
        model.HomeAddr = NullIfEmpty(Normalize(model.HomeAddr, 100));
        model.Mobile = NullIfEmpty(Normalize(model.Mobile, 20));
        model.HomePhone = NullIfEmpty(Normalize(model.HomePhone, 20));
        model.WorkPhone = NullIfEmpty(Normalize(model.WorkPhone, 20));
        model.WxNum = NullIfEmpty(Normalize(model.WxNum, 30));
        model.QqNum = NullIfEmpty(Normalize(model.QqNum, 20));
        var em = (model.EMail ?? "").Trim();
        model.EMail = em.Length == 0 ? null : (em.Length <= 80 ? em : em[..80]);
        model.Health = NullIfEmpty(Normalize(model.Health, 8));
        var r = (model.Remark ?? "").Trim();
        model.Remark = r.Length == 0 ? null : r;
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;

    public async Task<IReadOnlyList<(string Key, string Message)>> GetSaveValidationErrorsAsync(
        EMemberFormVm model,
        bool isEdit,
        CancellationToken ct)
    {
        var errors = new List<(string Key, string Message)>();
        var dup = await _db.EMembers.AsNoTracking()
            .AnyAsync(x => x.MemberId == model.MemberId && (!isEdit || x.DataId != model.DataId), ct);
        if (dup)
            errors.Add((nameof(EMemberFormVm.MemberId), "人员编号已存在，请换一个。"));

        if (model.DeptId.HasValue &&
            !await _db.EDepartments.AsNoTracking().AnyAsync(x => x.DataId == model.DeptId.Value, ct))
            errors.Add((nameof(EMemberFormVm.DeptId), "所选部门不存在。"));

        if (model.PosId.HasValue &&
            !await _db.EPositions.AsNoTracking().AnyAsync(x => x.DataId == model.PosId.Value, ct))
            errors.Add((nameof(EMemberFormVm.PosId), "所选岗位不存在。"));

        if (!string.IsNullOrEmpty(model.EMail) && !IsLooseEmail(model.EMail))
            errors.Add((nameof(EMemberFormVm.EMail), "邮箱格式不正确。"));

        return errors;
    }

    private static bool IsLooseEmail(string email) =>
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));

    public async Task CreateAsync(EMemberFormVm model, string operatorId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var entity = new EMember
        {
            MemberId = model.MemberId,
            MemberName = model.MemberName,
            Sex = model.Sex,
            Nation = model.Nation,
            NativePlace = model.NativePlace,
            IdNo = model.IdNo,
            Birthday = model.Birthday,
            ELevel = model.ELevel,
            Degree = model.Degree,
            School = model.School,
            Speciality = model.Speciality,
            ComputerAbility = model.ComputerAbility,
            WorkDate = model.WorkDate,
            PerGrade = model.PerGrade,
            DeptId = model.DeptId,
            PosId = model.PosId,
            HomeAddr = model.HomeAddr,
            Mobile = model.Mobile,
            HomePhone = model.HomePhone,
            WorkPhone = model.WorkPhone,
            WxNum = model.WxNum,
            QqNum = model.QqNum,
            EMail = model.EMail,
            Health = model.Health,
            Remark = model.Remark,
            CreateDate = now,
            AmendDate = now,
            OperatorName = operatorId
        };
        _db.EMembers.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("人员编号已存在，请换一个。", ex);
        }
    }

    public async Task<bool> TryUpdateAsync(int id, EMemberFormVm model, string operatorId, CancellationToken ct)
    {
        var row = await _db.EMembers.FirstOrDefaultAsync(x => x.DataId == id, ct);
        if (row == null) return false;

        var changed = false;
        if (!string.Equals(row.MemberName, model.MemberName, StringComparison.Ordinal)) { row.MemberName = model.MemberName; changed = true; }
        if (!string.Equals((row.Sex ?? "").Trim(), (model.Sex ?? "").Trim(), StringComparison.Ordinal)) { row.Sex = model.Sex; changed = true; }
        if (!string.Equals((row.Nation ?? "").Trim(), (model.Nation ?? "").Trim(), StringComparison.Ordinal)) { row.Nation = model.Nation; changed = true; }
        if (!string.Equals((row.NativePlace ?? "").Trim(), (model.NativePlace ?? "").Trim(), StringComparison.Ordinal)) { row.NativePlace = model.NativePlace; changed = true; }
        if (!string.Equals((row.IdNo ?? "").Trim(), (model.IdNo ?? "").Trim(), StringComparison.Ordinal)) { row.IdNo = model.IdNo; changed = true; }
        if (row.Birthday != model.Birthday) { row.Birthday = model.Birthday; changed = true; }
        if (!string.Equals((row.ELevel ?? "").Trim(), (model.ELevel ?? "").Trim(), StringComparison.Ordinal)) { row.ELevel = model.ELevel; changed = true; }
        if (!string.Equals((row.Degree ?? "").Trim(), (model.Degree ?? "").Trim(), StringComparison.Ordinal)) { row.Degree = model.Degree; changed = true; }
        if (!string.Equals((row.School ?? "").Trim(), (model.School ?? "").Trim(), StringComparison.Ordinal)) { row.School = model.School; changed = true; }
        if (!string.Equals((row.Speciality ?? "").Trim(), (model.Speciality ?? "").Trim(), StringComparison.Ordinal)) { row.Speciality = model.Speciality; changed = true; }
        if (!string.Equals((row.ComputerAbility ?? "").Trim(), (model.ComputerAbility ?? "").Trim(), StringComparison.Ordinal)) { row.ComputerAbility = model.ComputerAbility; changed = true; }
        if (row.WorkDate != model.WorkDate) { row.WorkDate = model.WorkDate; changed = true; }
        if (!string.Equals((row.PerGrade ?? "").Trim(), (model.PerGrade ?? "").Trim(), StringComparison.Ordinal)) { row.PerGrade = model.PerGrade; changed = true; }
        if (row.DeptId != model.DeptId) { row.DeptId = model.DeptId; changed = true; }
        if (row.PosId != model.PosId) { row.PosId = model.PosId; changed = true; }
        if (!string.Equals((row.HomeAddr ?? "").Trim(), (model.HomeAddr ?? "").Trim(), StringComparison.Ordinal)) { row.HomeAddr = model.HomeAddr; changed = true; }
        if (!string.Equals((row.Mobile ?? "").Trim(), (model.Mobile ?? "").Trim(), StringComparison.Ordinal)) { row.Mobile = model.Mobile; changed = true; }
        if (!string.Equals((row.HomePhone ?? "").Trim(), (model.HomePhone ?? "").Trim(), StringComparison.Ordinal)) { row.HomePhone = model.HomePhone; changed = true; }
        if (!string.Equals((row.WorkPhone ?? "").Trim(), (model.WorkPhone ?? "").Trim(), StringComparison.Ordinal)) { row.WorkPhone = model.WorkPhone; changed = true; }
        if (!string.Equals((row.WxNum ?? "").Trim(), (model.WxNum ?? "").Trim(), StringComparison.Ordinal)) { row.WxNum = model.WxNum; changed = true; }
        if (!string.Equals((row.QqNum ?? "").Trim(), (model.QqNum ?? "").Trim(), StringComparison.Ordinal)) { row.QqNum = model.QqNum; changed = true; }
        if (!string.Equals((row.EMail ?? "").Trim(), (model.EMail ?? "").Trim(), StringComparison.Ordinal)) { row.EMail = model.EMail; changed = true; }
        if (!string.Equals((row.Health ?? "").Trim(), (model.Health ?? "").Trim(), StringComparison.Ordinal)) { row.Health = model.Health; changed = true; }
        if (!string.Equals((row.Remark ?? "").Trim(), (model.Remark ?? "").Trim(), StringComparison.Ordinal)) { row.Remark = model.Remark; changed = true; }

        if (!changed) return true;

        row.AmendDate = DateTime.Now;
        row.OperatorName = operatorId;
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("人员编号已存在，请换一个。", ex);
        }

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var row = await _db.EMembers.FirstOrDefaultAsync(x => x.DataId == id, ct);
        if (row == null) return true;
        _db.EMembers.Remove(row);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task BatchDeleteAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return;
        var rows = await _db.EMembers.Where(x => ids.Contains(x.DataId)).ToListAsync(ct);
        if (rows.Count > 0)
        {
            _db.EMembers.RemoveRange(rows);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<EMember?> GetByIdNoTrackAsync(int id, CancellationToken ct) =>
        await _db.EMembers.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == id, ct);

    public async Task<EMemberDetailVm?> GetDetailVmAsync(int id, CancellationToken ct)
    {
        var x = await _db.EMembers.AsNoTracking().FirstOrDefaultAsync(e => e.DataId == id, ct);
        if (x == null) return null;
        var deptName = "-";
        if (x.DeptId.HasValue)
        {
            var d = await _db.EDepartments.AsNoTracking().FirstOrDefaultAsync(t => t.DataId == x.DeptId.Value, ct);
            if (d != null) deptName = $"{d.DeptCode} - {d.DeptCName}";
        }
        var posName = "-";
        if (x.PosId.HasValue)
        {
            var p = await _db.EPositions.AsNoTracking().FirstOrDefaultAsync(t => t.DataId == x.PosId.Value, ct);
            if (p != null) posName = $"{p.PostCode} - {p.PostCName}";
        }

        return new EMemberDetailVm
        {
            DataId = x.DataId,
            MemberId = x.MemberId,
            MemberName = x.MemberName,
            Sex = x.Sex,
            Nation = x.Nation,
            NativePlace = x.NativePlace,
            IdNoMasked = MaskIdNo(x.IdNo),
            Birthday = x.Birthday,
            ELevel = x.ELevel,
            Degree = x.Degree,
            School = x.School,
            Speciality = x.Speciality,
            ComputerAbility = x.ComputerAbility,
            WorkDate = x.WorkDate,
            PerGrade = x.PerGrade,
            DeptName = deptName,
            PosName = posName,
            HomeAddr = x.HomeAddr,
            Mobile = x.Mobile,
            HomePhone = x.HomePhone,
            WorkPhone = x.WorkPhone,
            WxNum = x.WxNum,
            QqNum = x.QqNum,
            EMail = x.EMail,
            Health = x.Health,
            Remark = x.Remark,
            CreateDate = x.CreateDate,
            AmendDate = x.AmendDate,
            OperatorName = x.OperatorName
        };
    }

    public async Task<(
        IReadOnlyList<EMemberListRowVm> pageRows,
        int totalRecords,
        int totalPages,
        int page,
        List<(string Value, string Text)> deptFilterOptions,
        List<(string Value, string Text)> posFilterOptions)> GetIndexPageAsync(
        string? deptId1,
        string? posId1,
        string? sex1,
        string? perGrade1,
        string searchField,
        string searchContent,
        string sortField,
        string sortArrow,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var deptFilterOptions = await GetDeptFilterOptionsAsync(ct);
        var posFilterOptions = await GetPosFilterOptionsAsync(ct);
        var sexMap = await _dict.GetItemMapAsync(DictCodes.MemberSex, ct: ct);
        var perGradeMap = await _dict.GetItemMapAsync(DictCodes.MemberPerGrade, ct: ct);

        var rows = await _db.EMembers.AsNoTracking().ToListAsync(ct);
        var deptRows = await _db.EDepartments.AsNoTracking().ToDictionaryAsync(x => x.DataId, x => x, ct);
        var posRows = await _db.EPositions.AsNoTracking().ToDictionaryAsync(x => x.DataId, x => x, ct);

        if (int.TryParse((deptId1 ?? "").Trim(), out var df) && df > 0)
            rows = rows.Where(x => x.DeptId == df).ToList();

        if (int.TryParse((posId1 ?? "").Trim(), out var pf) && pf > 0)
            rows = rows.Where(x => x.PosId == pf).ToList();

        var sx = (sex1 ?? "").Trim();
        if (sx.Length > 0)
            rows = rows.Where(x => string.Equals((x.Sex ?? "").Trim(), sx, StringComparison.Ordinal)).ToList();

        var pg = (perGrade1 ?? "").Trim();
        if (pg.Length > 0)
            rows = rows.Where(x => string.Equals((x.PerGrade ?? "").Trim(), pg, StringComparison.Ordinal)).ToList();

        var sc = (searchContent ?? "").Trim();
        if (sc.Length > 0)
        {
            rows = (searchField ?? "MemberID") switch
            {
                "MemberName" => rows.Where(x => (x.MemberName ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "Mobile" => rows.Where(x => (x.Mobile ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)
                    || (x.WorkPhone ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "IDNo" => rows.Where(x => (x.IdNo ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "Remark" => rows.Where(x => (x.Remark ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                "MemberID" => rows.Where(x => (x.MemberId ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => rows.Where(x => (x.MemberId ?? "").Contains(sc, StringComparison.OrdinalIgnoreCase)).ToList()
            };
        }

        rows = SortRows(rows, sortField, sortArrow);
        var total = rows.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages) page = totalPages;
        if (page < 1) page = 1;

        string DeptLabel(int? id)
        {
            if (!id.HasValue || !deptRows.TryGetValue(id.Value, out var d)) return "-";
            return $"{d.DeptCode} - {d.DeptCName}";
        }

        string PosLabel(int? id)
        {
            if (!id.HasValue || !posRows.TryGetValue(id.Value, out var p)) return "-";
            return $"{p.PostCode} - {p.PostCName}";
        }

        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).Select(x =>
        {
            var mob = string.IsNullOrWhiteSpace(x.Mobile) ? (x.WorkPhone ?? "").Trim() : x.Mobile!.Trim();
            return new EMemberListRowVm
            {
                DataId = x.DataId,
                MemberId = x.MemberId,
                MemberName = x.MemberName,
                SexDisplay = DictDisplay(x.Sex, sexMap),
                DeptName = DeptLabel(x.DeptId),
                PosName = PosLabel(x.PosId),
                Mobile = string.IsNullOrEmpty(mob) ? "-" : mob,
                PerGradeDisplay = DictDisplay(x.PerGrade, perGradeMap),
                IdNoMasked = MaskIdNo(x.IdNo),
                CreateDate = x.CreateDate
            };
        }).ToList();

        return (pageRows, total, totalPages, page, deptFilterOptions, posFilterOptions);
    }

    private static List<EMember> SortRows(List<EMember> rows, string field, string arrow)
    {
        var desc = arrow == "1";
        return field switch
        {
            "MemberID" => desc ? rows.OrderByDescending(x => x.MemberId).ToList() : rows.OrderBy(x => x.MemberId).ToList(),
            "MemberName" => desc ? rows.OrderByDescending(x => x.MemberName).ToList() : rows.OrderBy(x => x.MemberName).ToList(),
            "DeptID" => desc ? rows.OrderByDescending(x => x.DeptId).ToList() : rows.OrderBy(x => x.DeptId).ToList(),
            "PosID" => desc ? rows.OrderByDescending(x => x.PosId).ToList() : rows.OrderBy(x => x.PosId).ToList(),
            "CreateDate" => desc ? rows.OrderByDescending(x => x.CreateDate).ToList() : rows.OrderBy(x => x.CreateDate).ToList(),
            _ => rows.OrderByDescending(x => x.CreateDate).ThenByDescending(x => x.DataId).ToList()
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        for (var e = ex.InnerException; e != null; e = e.InnerException)
        {
            if (e is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
                return true;
        }
        return false;
    }
}
