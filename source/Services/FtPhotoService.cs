using System.Text.Json;
using System.Text.RegularExpressions;
using FamilyTree.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public sealed class FtPhotoService
{
    public const int MaxCount = 12;
    public const long MaxBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> ExtOk = new(StringComparer.OrdinalIgnoreCase)
    { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private readonly FrameworkDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly FtPersonService _persons;

    public FtPhotoService(FrameworkDbContext db, IWebHostEnvironment env, FtPersonService persons)
    {
        _db = db;
        _env = env;
        _persons = persons;
    }

    public static List<string> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<string>> ListVisibleAsync(FtPerson person, int userId, CancellationToken ct)
    {
        var all = Parse(person.PhotoListJson);
        if (all.Count == 0) return all;
        if (await CanSeePhotosAsync(person, userId, ct)) return all;
        return [];
    }

    public async Task<bool> CanManageAsync(int userId, FtPerson p, CancellationToken ct)
    {
        if (p.BindUserId == userId) return true;
        return await _persons.CanEditAsync(userId, p, ct);
    }

    public async Task<bool> CanSeePhotosAsync(FtPerson p, int userId, CancellationToken ct)
    {
        if (p.BindUserId == userId || p.OwnerUserId == userId) return true;
        if (await _persons.CanEditAsync(userId, p, ct)) return true;
        if (!p.ShowPhoto) return false;
        return p.PrivacyLevel >= 3;
    }

    public async Task<string> UploadAsync(int personId, int userId, IFormFile file, CancellationToken ct)
    {
        var p = await _persons.GetAsync(personId, ct) ?? throw new InvalidOperationException("人物不存在。");
        if (!await CanManageAsync(userId, p, ct))
            throw new InvalidOperationException("无权上传照片。仅本人绑定档案或有编辑权的填写岗可传。");
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("请选择照片。");
        if (file.Length > MaxBytes)
            throw new InvalidOperationException("单张照片不超过 2MB。");

        var ext = Path.GetExtension(file.FileName ?? "");
        if (!ExtOk.Contains(ext))
            throw new InvalidOperationException("仅支持 jpg / jpeg / png / gif / webp。");

        var list = Parse(p.PhotoListJson);
        if (list.Count >= MaxCount)
            throw new InvalidOperationException($"最多上传 {MaxCount} 张。");

        var dir = PersonDir(p.DataId);
        Directory.CreateDirectory(dir);
        var name = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + Guid.NewGuid().ToString("N")[..8] + ext.ToLowerInvariant();
        var full = Path.Combine(dir, name);
        await using (var fs = File.Create(full))
            await file.CopyToAsync(fs, ct);

        var rel = $"/uploads/person/{p.DataId}/{name}";
        list.Add(rel);
        p.PhotoListJson = JsonSerializer.Serialize(list);
        p.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        return rel;
    }

    public async Task DeleteAsync(int personId, int userId, string? path, CancellationToken ct)
    {
        var p = await _persons.GetAsync(personId, ct) ?? throw new InvalidOperationException("人物不存在。");
        if (!await CanManageAsync(userId, p, ct))
            throw new InvalidOperationException("无权删除照片。");
        var rel = (path ?? "").Trim().Replace('\\', '/');
        if (!IsOwnedPath(p.DataId, rel))
            throw new InvalidOperationException("路径无效。");
        var list = Parse(p.PhotoListJson);
        list = list.Where(x => !string.Equals(x, rel, StringComparison.OrdinalIgnoreCase)).ToList();
        p.PhotoListJson = JsonSerializer.Serialize(list);
        p.AmendDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);

        var full = MapToDisk(rel);
        if (full != null && File.Exists(full))
            File.Delete(full);
    }

    private string WebRoot()
    {
        var root = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(root))
            root = Path.Combine(_env.ContentRootPath, "wwwroot");
        return root;
    }

    private string PersonDir(int personId) =>
        Path.Combine(WebRoot(), "uploads", "person", personId.ToString());

    private static bool IsOwnedPath(int personId, string rel)
    {
        if (!rel.StartsWith($"/uploads/person/{personId}/", StringComparison.OrdinalIgnoreCase))
            return false;
        var file = rel[($"/uploads/person/{personId}/".Length)..];
        return file.Length > 0 && Regex.IsMatch(file, @"^[A-Za-z0-9._-]+$") && !file.Contains("..");
    }

    private string? MapToDisk(string rel)
    {
        var parts = rel.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) return null;
        return Path.Combine(WebRoot(), parts[0], parts[1], parts[2], parts[3]);
    }
}
