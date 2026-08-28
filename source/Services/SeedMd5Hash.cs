using System.Security.Cryptography;
using System.Text;

namespace FamilyTree.Services;

/// <summary>种子数据用 MD5 16 位十六进制哈希（与测试脚本 PwdHash 一致）。</summary>
public static class SeedMd5Hash
{
    public static string Md5_16(string password)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));
        var bytes = new byte[password.Length];
        for (var i = 0; i < password.Length; i++)
        {
            var ch = password[i];
            if (ch > 127)
                throw new NotSupportedException("种子哈希仅支持 ASCII 字符。");
            bytes[i] = (byte)ch;
        }

        Span<byte> hash = stackalloc byte[16];
        MD5.HashData(bytes, hash);

        var wb = BitConverter.ToUInt32(hash.Slice(4, 4));
        var wc = BitConverter.ToUInt32(hash.Slice(8, 4));
        return WordToHex(wb) + WordToHex(wc);

        static string WordToHex(uint v)
        {
            var sb = new StringBuilder(8);
            for (var i = 0; i < 4; i++)
            {
                var lByte = (byte)((v >> (i * 8)) & 0xff);
                sb.Append(lByte.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
