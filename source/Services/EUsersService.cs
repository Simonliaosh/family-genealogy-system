using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;

namespace FamilyTree.Services;

public class EUsersService
{
    public string Normalize(string? value, int maxLen, string fallback = "")
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) v = fallback;
        return v.Length <= maxLen ? v : v[..maxLen];
    }

    public string NormalizeStatus(string? value)
    {
        var v = Normalize(value, 10);
        return string.IsNullOrWhiteSpace(v) ? "0" : v;
    }

    public string NormalizeCode(string? value, string fallback = "0")
    {
        var v = Normalize(value, 10);
        return string.IsNullOrWhiteSpace(v) ? fallback : v;
    }

    /// <summary>种子格式哈希（测试用）。新用户请用 <see cref="HashPasswordForStore"/>。</summary>
    public string HashPassword(string plainText) => PasswordHasher.HashMd5_16(plainText);

    public (string Hash, string Algo, int Version) HashPasswordForStore(string plainText) =>
        PasswordHasher.HashForStore(plainText);

    public bool VerifyPassword(string plainText, EUser user) =>
        PasswordHasher.Verify(plainText, user.PwdHash, user.PasswordAlgo);

    public void ApplyStoredPassword(EUser user, string plainText)
    {
        var (hash, algo, version) = HashPasswordForStore(plainText);
        user.PwdHash = hash;
        user.PasswordAlgo = algo;
        user.PasswordVersion = version;
    }

    public bool ToBit(string? code)
    {
        var c = NormalizeCode(code);
        return string.Equals(c, "1", StringComparison.OrdinalIgnoreCase);
    }

    public EUser BuildCreateEntity(EUsersFormVm model, string? operatorId)
    {
        var entity = new EUser
        {
            LoginId = Normalize(model.LoginId, 30),
            RealName = Normalize(model.RealName, 20),
            PwdHash = "",
            UserType = Normalize(model.UserType, 12),
            LoginCount = 0,
            MaxLoginCount = model.MaxLoginCount < 1 ? 1 : model.MaxLoginCount,
            PwdErrorCount = 0,
            MaxPwdErrorCount = model.MaxPwdErrorCount < 1 ? 1 : model.MaxPwdErrorCount,
            IsLocked = ToBit(model.IsLocked),
            IsEnabled = ToBit(model.IsEnabled),
            LastLoginTime = null,
            LastPwdErrorTime = null,
            BStatus = EBStatusHelper.NormalizeBStatusForSave(model.BStatus),
            IsDeleted = false,
            PasswordAlgo = PasswordHasher.AlgoPbkdf2,
            PasswordVersion = PasswordHasher.PasswordVersionPbkdf2,
            CreateDate = DateTime.Now,
            AmendDate = DateTime.Now,
            OperatorName = Normalize(operatorId, 30, "system")
        };
        EUserExternalRefHelper.ApplyCustomerId(entity, model.CustomerId);
        EUserExternalRefHelper.ApplyPartnerId(entity, model.PartnerId);
        EUserExternalRefHelper.ApplySupplierId(entity, model.SupplierId);
        var initPwd = string.IsNullOrWhiteSpace(model.InitialPassword) ? "-" : model.InitialPassword;
        var (hash, algo, ver) = HashPasswordForStore(initPwd);
        entity.PwdHash = hash;
        entity.PasswordAlgo = algo;
        entity.PasswordVersion = ver;
        return entity;
    }

    public EUsersFormVm BuildFormVm(EUser row)
    {
        return new EUsersFormVm
        {
            DataId = row.DataId,
            LoginId = row.LoginId ?? "",
            RealName = row.RealName ?? "",
            UserType = row.UserType ?? "",
            LoginCount = row.LoginCount,
            MaxLoginCount = row.MaxLoginCount,
            PwdErrorCount = row.PwdErrorCount,
            MaxPwdErrorCount = row.MaxPwdErrorCount,
            IsLocked = row.IsLocked ? "1" : "0",
            IsEnabled = row.IsEnabled ? "1" : "0",
            LastLoginTime = row.LastLoginTime,
            LastPwdErrorTime = row.LastPwdErrorTime,
            BStatus = row.BStatus ?? "1",
            CustomerId = EUserExternalRefHelper.GetCustomerId(row),
            PartnerId = EUserExternalRefHelper.GetPartnerId(row),
            SupplierId = EUserExternalRefHelper.GetSupplierId(row),
            ShareHolderCode = row.ShareHolderCode
        };
    }

    public (List<string> SetList, Dictionary<string, object?> Params) BuildUpdateSet(EUser oldRow, EUsersFormVm model)
    {
        var setList = new List<string>();
        var ps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var realName = Normalize(model.RealName, 20);
        var userType = Normalize(model.UserType, 12);
        var maxLogin = model.MaxLoginCount < 1 ? 1 : model.MaxLoginCount;
        var maxPwdErr = model.MaxPwdErrorCount < 1 ? 1 : model.MaxPwdErrorCount;
        var bStatus = EBStatusHelper.NormalizeBStatusForSave(model.BStatus);
        var isEnabled = ToBit(model.IsEnabled);
        var isLocked = ToBit(model.IsLocked);
        AddIfChanged(setList, ps, "RealName=@RealName", "@RealName", oldRow.RealName, realName);
        AddIfChanged(setList, ps, "UserType=@UserType", "@UserType", oldRow.UserType, userType);
        if (oldRow.MaxLoginCount != maxLogin)
        {
            setList.Add("MaxLoginCount=@MaxLoginCount");
            ps["@MaxLoginCount"] = maxLogin;
        }
        if (oldRow.MaxPwdErrorCount != maxPwdErr)
        {
            setList.Add("MaxPwdErrorCount=@MaxPwdErrorCount");
            ps["@MaxPwdErrorCount"] = maxPwdErr;
        }
        if (oldRow.IsEnabled != isEnabled)
        {
            setList.Add("IsEnabled=@IsEnabled");
            ps["@IsEnabled"] = isEnabled;
        }
        if (oldRow.IsLocked != isLocked)
        {
            setList.Add("IsLocked=@IsLocked");
            ps["@IsLocked"] = isLocked;
        }
        AddIfChanged(setList, ps, "BStatus=@BStatus", "@BStatus", oldRow.BStatus, bStatus);
        var extType = model.CustomerId.HasValue ? "CUSTOMER"
            : model.PartnerId.HasValue ? "PARTNER"
            : model.SupplierId.HasValue ? "SUPPLIER" : null;
        var extId = model.CustomerId?.ToString() ?? model.PartnerId?.ToString() ?? model.SupplierId?.ToString();
        AddIfChanged(setList, ps, "ExternalRefType=@ExternalRefType", "@ExternalRefType", oldRow.ExternalRefType, extType);
        AddIfChanged(setList, ps, "ExternalRefID=@ExternalRefID", "@ExternalRefID", oldRow.ExternalRefId, extId);

        if (!string.IsNullOrWhiteSpace(model.ResetPassword))
        {
            var (hash, algo, ver) = HashPasswordForStore(model.ResetPassword);
            setList.Add("PwdHash=@PwdHash");
            ps["@PwdHash"] = hash;
            setList.Add("PasswordAlgo=@PasswordAlgo");
            ps["@PasswordAlgo"] = algo;
            setList.Add("PasswordVersion=@PasswordVersion");
            ps["@PasswordVersion"] = ver;
            setList.Add("PwdErrorCount=@PwdErrorCount");
            ps["@PwdErrorCount"] = 0;
            setList.Add("LastPwdErrorTime=@LastPwdErrorTime");
            ps["@LastPwdErrorTime"] = DBNull.Value;
        }

        if (setList.Count > 0)
        {
            setList.Add("AmendDate=@AmendDate");
            ps["@AmendDate"] = DateTime.Now;
        }

        return (setList, ps);
    }

    private static void AddIfChanged(
        List<string> setList,
        Dictionary<string, object?> ps,
        string setExpr,
        string paramName,
        string? oldValue,
        string? newValue)
    {
        var ov = (oldValue ?? "").Trim();
        var nv = (newValue ?? "").Trim();
        if (string.Equals(ov, nv, StringComparison.OrdinalIgnoreCase)) return;
        setList.Add(setExpr);
        ps[paramName] = nv;
    }
}

