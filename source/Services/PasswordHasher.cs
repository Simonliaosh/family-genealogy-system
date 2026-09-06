using System.Security.Cryptography;
using System.Text;

namespace FamilyTree.Services;

/// <summary>
/// 密码哈希：种子数据使用 MD5_16 格式；新密码使用 PBKDF2-SHA256。登录成功后可透明升级。
/// </summary>
public static class PasswordHasher
{
    public const string AlgoMd5_16 = "MD5_16";
    public const string AlgoPbkdf2 = "PBKDF2";
    public const int Pbkdf2Iterations = 100_000;
    public const int PasswordVersionPbkdf2 = 2;

    public static string HashMd5_16(string plainText) =>
        SeedMd5Hash.Md5_16((plainText ?? "").Trim());

    public static string HashPbkdf2(string plainText)
    {
        var password = (plainText ?? "").Trim();
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, 32);
        return $"{AlgoPbkdf2}|{Pbkdf2Iterations}|{Convert.ToBase64String(salt)}|{Convert.ToBase64String(hash)}";
    }

    public static (string Hash, string Algo, int Version) HashForStore(string plainText) =>
        (HashPbkdf2(plainText), AlgoPbkdf2, PasswordVersionPbkdf2);

    public static bool ShouldUpgrade(string? passwordAlgo) =>
        string.IsNullOrWhiteSpace(passwordAlgo)
        || string.Equals(passwordAlgo.Trim(), AlgoMd5_16, StringComparison.OrdinalIgnoreCase);

    public static bool Verify(string plainText, string storedHash, string? passwordAlgo)
    {
        var plain = (plainText ?? "").Trim();
        var stored = (storedHash ?? "").Trim();
        if (plain.Length == 0 || stored.Length == 0) return false;

        var algo = (passwordAlgo ?? "").Trim();

        // PBKDF2 优先：算法字段声明 PBKDF2，或存储串自带 PBKDF2 前缀（算法字段缺失的历史行）
        if (string.Equals(algo, AlgoPbkdf2, StringComparison.OrdinalIgnoreCase)
            || stored.StartsWith($"{AlgoPbkdf2}|", StringComparison.Ordinal))
            return VerifyPbkdf2(plain, stored);

        // MD5_16 仅保留登录时校验一次并立即升级的兼容路径（AccountController 调 ShouldUpgrade）
        if (algo.Length == 0 || string.Equals(algo, AlgoMd5_16, StringComparison.OrdinalIgnoreCase))
            return VerifyMd5_16(plain, stored);

        // 未知算法一律失败，不再回落到 MD5_16
        return false;
    }

    private static bool VerifyMd5_16(string plain, string stored)
    {
        string computed;
        try { computed = HashMd5_16(plain); }
        catch (NotSupportedException) { return false; }   // 非 ASCII 口令不可能是 MD5_16 种子哈希
        var a = Encoding.UTF8.GetBytes(computed.ToLowerInvariant());
        var b = Encoding.UTF8.GetBytes(stored.ToLowerInvariant());
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static bool VerifyPbkdf2(string plain, string stored)
    {
        var parts = stored.Split('|');
        if (parts.Length != 4 || !string.Equals(parts[0], AlgoPbkdf2, StringComparison.Ordinal))
            return false;
        if (!int.TryParse(parts[1], out var iterations) || iterations < 1)
            return false;

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(plain, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
