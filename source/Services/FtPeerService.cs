using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FamilyTree.Configuration;
using FamilyTree.Helpers;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FamilyTree.Services;

public sealed class FtPeerService
{
    private static readonly JsonSerializerOptions JsonOpt = new() { PropertyNameCaseInsensitive = true };

    private readonly FrameworkDbContext _db;
    private readonly FamilyTreeOptions _opt;
    private readonly FtDutyAccess _duty;
    private readonly FtOpLogService _log;
    private readonly IHttpClientFactory _http;
    private readonly IHttpContextAccessor _httpCtx;

    public FtPeerService(
        FrameworkDbContext db,
        IOptions<FamilyTreeOptions> opt,
        FtDutyAccess duty,
        FtOpLogService log,
        IHttpClientFactory http,
        IHttpContextAccessor httpCtx)
    {
        _db = db;
        _opt = opt.Value;
        _duty = duty;
        _log = log;
        _http = http;
        _httpCtx = httpCtx;
    }

    public string ResolveSiteId()
    {
        var id = (_opt.SiteId ?? "").Trim();
        if (id.Length > 0) return id;
        return "local-dev-site";
    }

    public string ResolvePublicBaseUrl()
    {
        var u = (_opt.PublicBaseUrl ?? "").Trim().TrimEnd('/');
        if (u.Length > 0) return u;
        var req = _httpCtx.HttpContext?.Request;
        if (req == null) return "";
        return $"{req.Scheme}://{req.Host.Value}".TrimEnd('/');
    }

    public async Task<bool> CanManagePeerAsync(int userId, CancellationToken ct) =>
        await _duty.IsSuperAsync(userId, ct) || await _duty.IsBranchAdminAsync(userId, ct);

    public async Task<(string Code, string InviteUrl, FtPerson Person, DateTime Expire)> CreateInviteAsync(
        int localPersonId, int userId, string op, CancellationToken ct)
    {
        if (!await CanManagePeerAsync(userId, ct))
            throw new InvalidOperationException("仅分支管理员或超管可发起外链对接。");
        var person = await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == localPersonId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("人物不存在。");
        var baseUrl = ResolvePublicBaseUrl();
        if (baseUrl.Length == 0)
            throw new InvalidOperationException("请先在配置 FamilyTree:PublicBaseUrl 填写本站对外地址。");

        var now = DateTime.Now;
        var code = NewCode(16);
        var days = _opt.PeerInviteDays <= 0 ? 7 : _opt.PeerInviteDays;
        var invite = new FtPeerInvite
        {
            InviteCode = code,
            LocalPersonId = localPersonId,
            SiteId = ResolveSiteId(),
            ExpireDate = now.AddDays(days),
            InviteStatus = "OPEN",
            CreateUserId = userId,
            CreateDate = now,
            AmendDate = now,
            OperatorName = FtText.ClipReq(op, 30)
        };
        _db.FtPeerInvites.Add(invite);
        await _db.SaveChangesAsync(ct);
        await _log.WriteAsync("PEER_INVITE", "PeerInvite", invite.DataId.ToString(), userId, op,
            person.FullName, null, code, ct);
        var url = $"{baseUrl}/FtPeer/InviteOpen?c={Uri.EscapeDataString(code)}";
        return (code, url, person, invite.ExpireDate);
    }

    public async Task<object?> PublicInviteInfoAsync(string? code, CancellationToken ct)
    {
        var inv = await FindOpenInviteAsync(code, ct);
        if (inv == null) return null;
        var p = await _db.FtPersons.AsNoTracking().FirstOrDefaultAsync(x => x.DataId == inv.LocalPersonId && !x.IsDeleted, ct);
        if (p == null) return null;
        return new
        {
            inviteCode = inv.InviteCode,
            siteId = inv.SiteId,
            baseUrl = ResolvePublicBaseUrl(),
            personId = p.DataId,
            fullName = p.FullName,
            fatherName = p.FatherName,
            motherName = p.MotherName,
            birthDate = p.BirthDate,
            gender = p.Gender,
            expireDate = inv.ExpireDate
        };
    }

    public async Task AcceptInviteAsync(FtPeerAcceptVm m, int userId, string op, CancellationToken ct)
    {
        if (!await CanManagePeerAsync(userId, ct))
            throw new InvalidOperationException("仅分支管理员或超管可接受外链对接。");

        var peerBase = NormalizeBaseUrl(m.PeerBaseUrl);
        var code = (m.InviteCode ?? "").Trim().ToUpperInvariant();
        if (peerBase.Length == 0 || code.Length < 8)
            throw new InvalidOperationException("请填写对方站点地址和邀请码。");
        if (m.LocalPersonId <= 0)
            throw new InvalidOperationException("请选择本站对应的共同祖先。");

        var local = await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == m.LocalPersonId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("本站人物不存在。");

        var ourOutPlain = NewTokenPlain();
        var ourOutHash = HashToken(ourOutPlain);
        var ourSiteId = ResolveSiteId();
        var ourBase = ResolvePublicBaseUrl();
        if (ourBase.Length == 0)
            throw new InvalidOperationException("请先配置 FamilyTree:PublicBaseUrl。");

        var client = _http.CreateClient("FtPeer");
        var body = new
        {
            inviteCode = code,
            peerSiteId = ourSiteId,
            peerBaseUrl = ourBase,
            peerPersonId = local.DataId,
            peerLabel = FtText.Clip(m.PeerLabel, 64) ?? "对接房",
            peerFullName = local.FullName,
            peerFatherName = local.FatherName,
            peerMotherName = local.MotherName,
            peerBirthDate = local.BirthDate,
            outTokenForHost = ourOutPlain
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{peerBase}/api/FtPeer/RedeemInvite")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        using var resp = await client.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        var pack = TryParseApi(raw);
        if (!resp.IsSuccessStatusCode || pack == null || !pack.Value.Ok)
            // 不回显远端响应体：否则本接口就是一个带错误回显的半盲探测器
            throw new InvalidOperationException("对方站点拒绝了本次对接请求（HTTP " + (int)resp.StatusCode + "）。");

        var data = pack.Value.Data;
        if (data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            throw new InvalidOperationException("对方返回无效。");

        var hostSiteId = data.TryGetProperty("siteId", out var sid) ? sid.GetString() ?? "" : "";
        var hostPersonId = data.TryGetProperty("personId", out var pid) && pid.TryGetInt32(out var pi) ? pi : 0;
        var inToken = data.TryGetProperty("inToken", out var tok) ? tok.GetString() ?? "" : "";
        var hostLabel = data.TryGetProperty("label", out var lb) ? lb.GetString() : null;
        if (hostSiteId.Length == 0 || hostPersonId <= 0 || inToken.Length < 16)
            throw new InvalidOperationException("对方返回缺少接点信息。");

        var now = DateTime.Now;
        var bridge = new FtPeerBridge
        {
            LocalPersonId = local.DataId,
            PeerBaseUrl = peerBase,
            PeerSiteId = hostSiteId,
            PeerPersonId = hostPersonId,
            PeerLabel = FtText.Clip(m.PeerLabel, 64) ?? hostLabel ?? "对接房",
            BridgeStatus = "ACTIVE",
            OutTokenHash = ourOutHash,
            InToken = inToken,
            InviteCode = code,
            CreateUserId = userId,
            CreateDate = now,
            AmendDate = now,
            OperatorName = FtText.ClipReq(op, 30)
        };
        _db.FtPeerBridges.Add(bridge);
        await _db.SaveChangesAsync(ct);
        await _log.WriteAsync("PEER_ACCEPT", "PeerBridge", bridge.DataId.ToString(), userId, op,
            $"{local.FullName}<->{hostPersonId}@{hostSiteId}", null, peerBase, ct);
    }

    /// <summary>邀请方被调用：核销邀请并建立本站接点，返回给接受方的 InToken。</summary>
    public async Task<object> RedeemInviteApiAsync(RedeemInviteBody body, CancellationToken ct)
    {
        var inv = await FindOpenInviteAsync(body.InviteCode, ct)
            ?? throw new InvalidOperationException("邀请码无效或已过期。");
        var hostPerson = await _db.FtPersons.FirstOrDefaultAsync(x => x.DataId == inv.LocalPersonId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("邀请对应人物不存在。");

        var peerBase = NormalizeBaseUrl(body.PeerBaseUrl);
        var peerSite = (body.PeerSiteId ?? "").Trim();
        if (peerBase.Length == 0 || peerSite.Length == 0 || body.PeerPersonId <= 0)
            throw new InvalidOperationException("对接方站点信息不完整。");
        if (string.Equals(peerSite, ResolveSiteId(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("不能对接本站自己。");

        var tokenFromPeer = (body.OutTokenForHost ?? "").Trim();
        if (tokenFromPeer.Length < 16)
            throw new InvalidOperationException("对接令牌无效。");

        var ourOutPlain = NewTokenPlain();
        var ourOutHash = HashToken(ourOutPlain);
        var now = DateTime.Now;
        var bridge = new FtPeerBridge
        {
            LocalPersonId = hostPerson.DataId,
            PeerBaseUrl = peerBase,
            PeerSiteId = peerSite,
            PeerPersonId = body.PeerPersonId,
            PeerLabel = FtText.Clip(body.PeerLabel, 64) ?? "对接房",
            BridgeStatus = "ACTIVE",
            OutTokenHash = ourOutHash,
            InToken = tokenFromPeer,
            InviteCode = inv.InviteCode,
            CreateUserId = inv.CreateUserId,
            CreateDate = now,
            AmendDate = now,
            OperatorName = "PEER-API"
        };
        _db.FtPeerBridges.Add(bridge);
        inv.InviteStatus = "USED";
        inv.AmendDate = now;
        await _db.SaveChangesAsync(ct);
        await _log.WriteAsync("PEER_REDEEM", "PeerBridge", bridge.DataId.ToString(), inv.CreateUserId, "PEER-API",
            $"{hostPerson.FullName}<->{body.PeerPersonId}@{peerSite}", null, null, ct);

        return new
        {
            siteId = ResolveSiteId(),
            personId = hostPerson.DataId,
            fullName = hostPerson.FullName,
            label = bridge.PeerLabel,
            inToken = ourOutPlain
        };
    }

    public async Task<List<FtPeerChildDto>> ChildrenForPeerTokenAsync(string? bearer, int personId, CancellationToken ct)
    {
        var bridge = await ValidateIncomingTokenAsync(bearer, ct)
            ?? throw new InvalidOperationException("外链令牌无效或已撤销。");
        if (personId != bridge.LocalPersonId && !await IsDescendantOfAsync(bridge.LocalPersonId, personId, ct))
            throw new InvalidOperationException("无权查看该人物（超出对接接点范围）。");

        var kids = await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted && x.SameAsPersonId == null
                        && (x.FatherPersonId == personId || x.MotherPersonId == personId))
            .ToListAsync(ct);
        kids = FtTreeService.OrderSiblingsByAge(kids).ToList();

        var result = new List<FtPeerChildDto>();
        foreach (var k in kids)
        {
            var has = await _db.FtPersons.AsNoTracking().AnyAsync(x =>
                !x.IsDeleted && x.SameAsPersonId == null
                && (x.FatherPersonId == k.DataId || x.MotherPersonId == k.DataId), ct);
            result.Add(new FtPeerChildDto
            {
                Id = k.DataId,
                Name = k.FullName,
                Birth = k.ShowBirthDetail ? k.BirthDate : (k.BirthYear?.ToString()),
                Gender = k.Gender,
                HasChildren = has
            });
        }
        return result;
    }

    public async Task<List<FtTreeNodeVm>> ExpandRemoteAsync(int bridgeId, int remotePersonId, CancellationToken ct)
    {
        var bridge = await _db.FtPeerBridges.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DataId == bridgeId && !x.IsDeleted && x.BridgeStatus == "ACTIVE", ct)
            ?? throw new InvalidOperationException("对接不存在或已取消。");

        // 历史行可能存着 http:// 或内网地址，出站前再过一次白名单/内网校验
        var peerBase = NormalizeBaseUrl(bridge.PeerBaseUrl);
        var client = _http.CreateClient("FtPeer");
        var url = $"{peerBase}/api/FtPeer/Children?personId={remotePersonId}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bridge.InToken);
        using var resp = await client.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        var pack = TryParseApi(raw);
        if (!resp.IsSuccessStatusCode || pack == null || !pack.Value.Ok)
            throw new InvalidOperationException("对方站点无法展开（可能已取消互通）。");

        var list = new List<FtTreeNodeVm>();
        if (pack.Value.Data.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in pack.Value.Data.EnumerateArray())
            {
                var id = el.TryGetProperty("id", out var idp) && idp.TryGetInt32(out var i) ? i : 0;
                var name = el.TryGetProperty("name", out var np) ? np.GetString() ?? "" : "";
                var birth = el.TryGetProperty("birth", out var bp) ? bp.GetString() : null;
                var has = el.TryGetProperty("hasChildren", out var hp) && hp.ValueKind == JsonValueKind.True;
                if (id <= 0 || name.Length == 0) continue;
                list.Add(new FtTreeNodeVm
                {
                    Id = 0,
                    NodeKey = $"peer:{bridgeId}:{id}",
                    Name = name,
                    Birth = birth,
                    IsRemote = true,
                    LazyExpand = has,
                    BridgeId = bridgeId,
                    RemotePersonId = id,
                    PeerLabel = bridge.PeerLabel
                });
            }
        }
        return list;
    }

    public async Task RevokeAsync(int bridgeId, int userId, string op, CancellationToken ct)
    {
        if (!await CanManagePeerAsync(userId, ct))
            throw new InvalidOperationException("仅分支管理员或超管可取消互通。");
        var row = await _db.FtPeerBridges.FirstOrDefaultAsync(x => x.DataId == bridgeId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("对接不存在。");
        if (row.BridgeStatus == "REVOKED") return;
        row.BridgeStatus = "REVOKED";
        row.AmendDate = DateTime.Now;
        row.OperatorName = FtText.ClipReq(op, 30);
        await _db.SaveChangesAsync(ct);
        await _log.WriteAsync("PEER_REVOKE", "PeerBridge", bridgeId.ToString(), userId, op, "取消互通", null, null, ct);

        // 通知对方吊销（失败不影响本站）
        try
        {
            var client = _http.CreateClient("FtPeer");
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{row.PeerBaseUrl.TrimEnd('/')}/api/FtPeer/RevokeByPeer")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    peerSiteId = ResolveSiteId(),
                    peerPersonId = row.LocalPersonId,
                    localPersonId = row.PeerPersonId
                }), Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", row.InToken);
            await client.SendAsync(req, ct);
        }
        catch { /* ignore */ }
    }

    public async Task RevokeByPeerApiAsync(string? bearer, int peerPersonIdOnUs, int theirLocalPersonId, string theirSiteId, CancellationToken ct)
    {
        var bridge = await ValidateIncomingTokenAsync(bearer, ct);
        if (bridge == null) return;
        if (bridge.LocalPersonId != peerPersonIdOnUs) return;
        if (!string.Equals(bridge.PeerSiteId, theirSiteId, StringComparison.OrdinalIgnoreCase)) return;
        if (bridge.PeerPersonId != theirLocalPersonId) return;
        if (bridge.BridgeStatus == "REVOKED") return;
        var tracked = await _db.FtPeerBridges.FirstAsync(x => x.DataId == bridge.DataId, ct);
        tracked.BridgeStatus = "REVOKED";
        tracked.AmendDate = DateTime.Now;
        tracked.OperatorName = "PEER-API";
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<FtPeerBridgeRowVm>> ListBridgesAsync(CancellationToken ct)
    {
        var rows = await _db.FtPeerBridges.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync(ct);
        var names = await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToDictionaryAsync(x => x.DataId, x => x.FullName, ct);
        return rows.Select(x => new FtPeerBridgeRowVm
        {
            DataId = x.DataId,
            LocalPersonId = x.LocalPersonId,
            LocalName = names.GetValueOrDefault(x.LocalPersonId, "#" + x.LocalPersonId),
            PeerBaseUrl = x.PeerBaseUrl,
            PeerSiteId = x.PeerSiteId,
            PeerPersonId = x.PeerPersonId,
            PeerLabel = x.PeerLabel,
            BridgeStatus = x.BridgeStatus,
            CreateDate = x.CreateDate
        }).ToList();
    }

    public async Task<List<FtPeerBridge>> ActiveBridgesForPersonAsync(int localPersonId, CancellationToken ct) =>
        await _db.FtPeerBridges.AsNoTracking()
            .Where(x => !x.IsDeleted && x.BridgeStatus == "ACTIVE" && x.LocalPersonId == localPersonId)
            .ToListAsync(ct);

    public async Task AttachPeerStubsAsync(FtTreeNodeVm? root, CancellationToken ct)
    {
        if (root == null) return;
        var bridges = await _db.FtPeerBridges.AsNoTracking()
            .Where(x => !x.IsDeleted && x.BridgeStatus == "ACTIVE")
            .ToListAsync(ct);
        if (bridges.Count == 0) return;
        var map = bridges.GroupBy(x => x.LocalPersonId).ToDictionary(g => g.Key, g => g.ToList());
        void Walk(FtTreeNodeVm n)
        {
            if (!n.IsRemote && map.TryGetValue(n.Id, out var list))
            {
                foreach (var b in list)
                {
                    n.Children.Add(new FtTreeNodeVm
                    {
                        Id = 0,
                        NodeKey = $"peer:{b.DataId}:{b.PeerPersonId}",
                        Name = string.IsNullOrWhiteSpace(b.PeerLabel) ? "外链房" : b.PeerLabel!,
                        IsRemote = true,
                        LazyExpand = true,
                        BridgeId = b.DataId,
                        RemotePersonId = b.PeerPersonId,
                        PeerLabel = b.PeerLabel
                    });
                }
            }
            foreach (var c in n.Children) Walk(c);
        }
        Walk(root);
    }

    private async Task<FtPeerInvite?> FindOpenInviteAsync(string? code, CancellationToken ct)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();
        if (c.Length < 8) return null;
        var inv = await _db.FtPeerInvites.FirstOrDefaultAsync(x =>
            !x.IsDeleted && x.InviteCode == c && x.InviteStatus == "OPEN", ct);
        if (inv == null) return null;
        if (inv.ExpireDate < DateTime.Now) return null;
        return inv;
    }

    private async Task<FtPeerBridge?> ValidateIncomingTokenAsync(string? bearer, CancellationToken ct)
    {
        var t = (bearer ?? "").Trim();
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t["Bearer ".Length..].Trim();
        if (t.Length < 16) return null;
        var hash = HashToken(t);
        return await _db.FtPeerBridges.FirstOrDefaultAsync(x =>
            !x.IsDeleted && x.BridgeStatus == "ACTIVE" && x.OutTokenHash == hash, ct);
    }

    private async Task<bool> IsDescendantOfAsync(int ancestorId, int personId, CancellationToken ct)
    {
        if (ancestorId == personId) return true;
        var all = await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => new { x.DataId, x.FatherPersonId, x.MotherPersonId })
            .ToListAsync(ct);
        var byId = all.ToDictionary(x => x.DataId);
        var cur = personId;
        var guard = 0;
        while (guard++ < 80 && byId.TryGetValue(cur, out var p))
        {
            if (p.FatherPersonId == ancestorId || p.MotherPersonId == ancestorId) return true;
            if (p.FatherPersonId is int f) { cur = f; continue; }
            if (p.MotherPersonId is int m) { cur = m; continue; }
            break;
        }
        return false;
    }

    /// <summary>
    /// 归一化并<strong>校验</strong>对端站点地址。原实现在无 scheme 时主动补 <c>http://</c> 且不做任何限制，
    /// 于是「对端地址」这个表单字段成了服务端可控的出站请求目标（SSRF）：
    /// 可探测云元数据 169.254.169.254、127.0.0.1 上的内部服务与管理端口，且响应体会被回显。
    /// 现在：必须是 https、必须显式带 scheme、主机名必须在 <c>FamilyTree:PeerAllowedHosts</c> 白名单内、
    /// 且不得解析到私有/环回/链路本地地址。
    /// </summary>
    private string NormalizeBaseUrl(string? url)
    {
        var u = (url ?? "").Trim().TrimEnd('/');
        if (u.Length == 0) return "";

        if (!u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("对端站点地址必须以 https:// 开头。");
        if (!Uri.TryCreate(u, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("对端站点地址格式无效。");
        if (!string.IsNullOrEmpty(uri.PathAndQuery.TrimEnd('/')) && uri.PathAndQuery.TrimEnd('/').Length > 0)
            throw new InvalidOperationException("对端站点地址只填到域名，不要带路径。");

        var allowed = _opt.PeerAllowedHosts ?? new List<string>();
        if (allowed.Count == 0)
            throw new InvalidOperationException("尚未配置联邦对端白名单 FamilyTree:PeerAllowedHosts，外链对接已禁用。");
        if (!allowed.Any(h => string.Equals(h?.Trim(), uri.Host, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("对端站点不在白名单内，请先由管理员加入 FamilyTree:PeerAllowedHosts。");

        EnsureNotInternalHost(uri.Host);
        return $"{uri.Scheme}://{uri.Authority}";
    }

    /// <summary>拒绝解析到私有网段 / 环回 / 链路本地（云元数据）的目标主机。</summary>
    private static void EnsureNotInternalHost(string host)
    {
        IPAddress[] addrs;
        if (IPAddress.TryParse(host, out var literal)) addrs = new[] { literal };
        else
        {
            try { addrs = Dns.GetHostAddresses(host); }
            catch (Exception ex) when (ex is SocketException or ArgumentException)
            {
                throw new InvalidOperationException("无法解析对端站点域名。");
            }
        }
        if (addrs.Length == 0) throw new InvalidOperationException("无法解析对端站点域名。");
        if (addrs.Any(IsInternal))
            throw new InvalidOperationException("对端站点解析到内网地址，已拒绝。");
    }

    private static bool IsInternal(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;
        if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return true;
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv4MappedToIPv6) return IsInternal(ip.MapToIPv4());
            return ip.Equals(IPAddress.IPv6Any) || ip.Equals(IPAddress.IPv6None);
        }
        var b = ip.GetAddressBytes();
        if (b.Length != 4) return false;
        return b[0] == 0                                   // 0.0.0.0/8
            || b[0] == 10                                  // 10/8
            || b[0] == 127                                 // 环回
            || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)   // CGNAT 100.64/10
            || (b[0] == 169 && b[1] == 254)                 // 链路本地 / 云元数据
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)    // 172.16/12
            || (b[0] == 192 && b[1] == 168)                 // 192.168/16
            || (b[0] == 192 && b[1] == 0 && b[2] == 0)      // IETF 保留
            || b[0] >= 224;                                 // 组播 / 保留
    }

    private static string NewCode(int bytes)
    {
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(bytes));
        return raw.ToUpperInvariant();
    }

    private static string NewTokenPlain() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

    private static string HashToken(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain ?? ""));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static (bool Ok, string Msg, JsonElement Data)? TryParseApi(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
            var r = doc.RootElement;
            var ok = r.TryGetProperty("ok", out var op) && op.ValueKind == JsonValueKind.True;
            var msg = r.TryGetProperty("message", out var mp) ? mp.GetString() ?? "" : "";
            var data = r.TryGetProperty("data", out var dp) ? dp.Clone() : default;
            return (ok, msg, data);
        }
        catch { return null; }
    }

    public sealed class RedeemInviteBody
    {
        public string? InviteCode { get; set; }
        public string? PeerSiteId { get; set; }
        public string? PeerBaseUrl { get; set; }
        public int PeerPersonId { get; set; }
        public string? PeerLabel { get; set; }
        public string? PeerFullName { get; set; }
        public string? PeerFatherName { get; set; }
        public string? PeerMotherName { get; set; }
        public string? PeerBirthDate { get; set; }
        public string? OutTokenForHost { get; set; }
    }
}
