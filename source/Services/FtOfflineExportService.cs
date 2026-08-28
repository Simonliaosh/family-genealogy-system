using System.IO.Compression;
using System.Text;
using System.Xml;
using FamilyTree.Models;
using FamilyTree.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

/// <summary>离线族谱包：genealogy.xml + index.html + photos → zip，解压后双击 html 即可浏览。</summary>
public sealed class FtOfflineExportService
{
    private readonly FrameworkDbContext _db;
    private readonly FtTreeService _tree;
    private readonly FtPersonService _persons;
    private readonly FtClanService _clans;
    private readonly IWebHostEnvironment _env;

    public FtOfflineExportService(
        FrameworkDbContext db, FtTreeService tree, FtPersonService persons, FtClanService clans, IWebHostEnvironment env)
    {
        _db = db;
        _tree = tree;
        _persons = persons;
        _clans = clans;
        _env = env;
    }

    public async Task<(byte[] Bytes, string FileName)?> BuildZipAsync(int userId, CancellationToken ct)
    {
        var (forest, clanName, mainMode) = await BuildForestAsync(userId, ct);
        if (forest.Count == 0) return null;

        var personIds = CollectPersonIds(forest);
        var personMap = await LoadPersonsAsync(personIds, ct);
        var marryMap = await LoadMarriagesAsync(personIds, ct);
        var photoFiles = CollectPhotoFiles(personMap);
        var photoPaths = photoFiles
            .GroupBy(x => x.PersonId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ZipPath).ToList());

        var xml = BuildXml(forest, personMap, marryMap, photoPaths, clanName, mainMode);
        var html = BuildHtml(xml, clanName, personMap.Count);
        var fileName = SanitizeFileName($"{clanName}_族谱_{DateTime.Now:yyyyMMdd}.zip");

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteTextEntry(zip, "genealogy.xml", xml, Encoding.UTF8);
            WriteTextEntry(zip, "index.html", html, Encoding.UTF8);
            WriteTextEntry(zip, "README.txt",
                "族谱离线包使用说明\r\n" +
                "1. 解压本 zip 到任意文件夹\r\n" +
                "2. 双击打开 index.html 即可浏览完整族谱（无需联网）\r\n" +
                "3. genealogy.xml 为族谱数据（含配偶信息）\r\n" +
                "4. photos 目录为人物照片，与 XML / HTML 中路径对应\r\n",
                Encoding.UTF8);
            foreach (var pf in photoFiles)
                WriteBinaryEntry(zip, pf.ZipPath, pf.DiskPath);
        }

        return (ms.ToArray(), fileName);
    }

    private async Task<(List<FtTreeNodeVm> Forest, string ClanName, bool MainMode)> BuildForestAsync(int userId, CancellationToken ct)
    {
        var (roots, allowIds, mainMode) = await _tree.ResolveScopeAsync(userId, _persons, ct);
        int? clanId = await _clans.GetUserClanIdAsync(userId, ct);
        var clan = clanId.HasValue ? await _clans.GetAsync(clanId.Value, ct) : await _clans.GetUserClanAsync(userId, ct);
        var clanName = clan?.ClanName ?? "家族族谱";

        if (mainMode && clanId is int cid)
            allowIds = await _clans.GetClanPersonIdsAsync(cid, ct);

        if (roots.Count == 0 && mainMode && clanId is int c2)
            roots = await _tree.CurrentRootsAsync(ct, c2);

        var forest = new List<FtTreeNodeVm>();
        var seen = new HashSet<int>();
        foreach (var r in roots)
        {
            if (!seen.Add(r.Id)) continue;
            var node = await _tree.BuildDownAsync(r.Id, ct, withPeerStubs: false, allowIds: allowIds, mainGenealogyOnly: mainMode);
            if (node != null) forest.Add(node);
        }

        return (forest, clanName, mainMode);
    }

    private static HashSet<int> CollectPersonIds(IEnumerable<FtTreeNodeVm> nodes)
    {
        var ids = new HashSet<int>();
        void Walk(FtTreeNodeVm n)
        {
            if (n.Id > 0) ids.Add(n.Id);
            foreach (var c in n.Children) Walk(c);
        }
        foreach (var root in nodes) Walk(root);
        return ids;
    }

    private async Task<Dictionary<int, FtPerson>> LoadPersonsAsync(HashSet<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return new Dictionary<int, FtPerson>();
        var list = await _db.FtPersons.AsNoTracking()
            .Where(x => !x.IsDeleted && ids.Contains(x.DataId))
            .ToListAsync(ct);
        return list.ToDictionary(x => x.DataId);
    }

    private async Task<Dictionary<int, List<FtPersonMarry>>> LoadMarriagesAsync(HashSet<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return new Dictionary<int, List<FtPersonMarry>>();
        var list = await _db.FtPersonMarrys.AsNoTracking()
            .Where(x => !x.IsDeleted && ids.Contains(x.PersonId))
            .OrderBy(x => x.HouseSeq).ThenBy(x => x.DataId)
            .ToListAsync(ct);
        return list.GroupBy(x => x.PersonId).ToDictionary(g => g.Key, g => g.ToList());
    }

    private sealed record PhotoFileEntry(int PersonId, string ZipPath, string DiskPath);

    private List<PhotoFileEntry> CollectPhotoFiles(Dictionary<int, FtPerson> personMap)
    {
        var result = new List<PhotoFileEntry>();
        foreach (var p in personMap.Values)
        {
            foreach (var rel in FtPhotoService.Parse(p.PhotoListJson))
            {
                var disk = MapPhotoToDisk(rel);
                if (disk == null || !File.Exists(disk)) continue;
                var fileName = Path.GetFileName(disk);
                var zipPath = $"photos/{p.DataId}/{fileName}";
                result.Add(new PhotoFileEntry(p.DataId, zipPath, disk));
            }
        }
        return result;
    }

    private string WebRoot()
    {
        var root = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(root))
            root = Path.Combine(_env.ContentRootPath, "wwwroot");
        return root;
    }

    private string? MapPhotoToDisk(string rel)
    {
        var parts = rel.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) return null;
        return Path.Combine(WebRoot(), parts[0], parts[1], parts[2], parts[3]);
    }

    private static string BuildXml(
        List<FtTreeNodeVm> forest,
        Dictionary<int, FtPerson> personMap,
        Dictionary<int, List<FtPersonMarry>> marryMap,
        Dictionary<int, List<string>> photoPaths,
        string clanName,
        bool mainMode)
    {
        var sb = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            OmitXmlDeclaration = false
        };
        using (var writer = XmlWriter.Create(sb, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("GenealogyExport");
            writer.WriteAttributeString("version", "1.1");
            writer.WriteAttributeString("exportedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            writer.WriteAttributeString("mainGenealogyOnly", mainMode ? "true" : "false");

            writer.WriteStartElement("Clan");
            writer.WriteAttributeString("name", clanName);
            writer.WriteEndElement();

            writer.WriteStartElement("Persons");
            foreach (var p in personMap.Values.OrderBy(x => x.FullName))
            {
                marryMap.TryGetValue(p.DataId, out var marriages);
                photoPaths.TryGetValue(p.DataId, out var photos);
                WritePersonElement(writer, p, marriages, photos);
            }
            writer.WriteEndElement();

            writer.WriteStartElement("Forest");
            foreach (var tree in forest)
            {
                writer.WriteStartElement("Tree");
                writer.WriteAttributeString("rootId", tree.Id.ToString());
                writer.WriteAttributeString("rootName", tree.Name);
                WriteTreeNode(writer, tree);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
        return sb.ToString();
    }

    private static void WritePersonElement(XmlWriter w, FtPerson p, List<FtPersonMarry>? marriages, List<string>? photos)
    {
        w.WriteStartElement("Person");
        w.WriteAttributeString("id", p.DataId.ToString());
        w.WriteAttributeString("name", p.FullName);
        w.WriteAttributeString("fatherName", p.FatherName ?? "");
        w.WriteAttributeString("motherName", p.MotherName ?? "");
        w.WriteAttributeString("birth", p.BirthDate ?? "");
        w.WriteAttributeString("gender", FtPersonService.GenderText(p.Gender));
        if (p.GenerationNo > 0) w.WriteAttributeString("generation", p.GenerationNo.ToString());
        if (!string.IsNullOrWhiteSpace(p.WordOfGeneration))
            w.WriteAttributeString("word", p.WordOfGeneration);
        if (p.FatherPersonId.HasValue) w.WriteAttributeString("fatherId", p.FatherPersonId.Value.ToString());
        if (p.MotherPersonId.HasValue) w.WriteAttributeString("motherId", p.MotherPersonId.Value.ToString());
        if (p.InMainGenealogy) w.WriteAttributeString("inMain", "true");
        if (!string.IsNullOrWhiteSpace(p.DeathInfo)) w.WriteAttributeString("death", p.DeathInfo);
        if (p.ShowSelfIntro && !string.IsNullOrWhiteSpace(p.SelfIntro))
            w.WriteAttributeString("intro", p.SelfIntro);

        if (marriages != null)
        {
            foreach (var m in marriages)
            {
                w.WriteStartElement("Marriage");
                w.WriteAttributeString("type", m.MarryType);
                w.WriteAttributeString("houseSeq", m.HouseSeq.ToString());
                if (!string.IsNullOrWhiteSpace(m.SpouseName)) w.WriteAttributeString("spouseName", m.SpouseName);
                if (!string.IsNullOrWhiteSpace(m.SpouseBirth)) w.WriteAttributeString("spouseBirth", m.SpouseBirth);
                if (m.SpousePersonId.HasValue) w.WriteAttributeString("spouseId", m.SpousePersonId.Value.ToString());
                w.WriteEndElement();
            }
        }

        if (photos != null)
        {
            foreach (var path in photos)
            {
                w.WriteStartElement("Photo");
                w.WriteAttributeString("file", path);
                w.WriteEndElement();
            }
        }

        w.WriteEndElement();
    }

    private static void WriteTreeNode(XmlWriter w, FtTreeNodeVm node)
    {
        w.WriteStartElement("Node");
        w.WriteAttributeString("id", node.Id.ToString());
        w.WriteAttributeString("name", node.Name);
        if (!string.IsNullOrWhiteSpace(node.Birth)) w.WriteAttributeString("birth", node.Birth);
        foreach (var c in node.Children) WriteTreeNode(w, c);
        w.WriteEndElement();
    }

    private static string BuildHtml(string xml, string clanName, int personCount)
    {
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));
        var title = EscapeHtml(clanName + " · 族谱一览");
        var exportDate = EscapeHtml(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

        return $@"<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
<meta charset=""utf-8""/>
<meta name=""viewport"" content=""width=device-width, initial-scale=1""/>
<title>{title}</title>
<style>
*{{box-sizing:border-box}}
html,body{{margin:0;height:100%;font-family:""Microsoft YaHei"",""PingFang SC"",sans-serif;background:#f4f1ea;color:#2c2a26}}
.bar{{position:sticky;top:0;z-index:10;display:flex;flex-wrap:wrap;align-items:center;gap:.5rem 1rem;padding:.65rem 1rem;background:rgba(255,252,247,.96);border-bottom:1px solid #e4dfd4}}
.bar h1{{margin:0;font-size:1.05rem;flex:1 1 auto}}
.bar .meta{{font-size:.8rem;color:#6c757d;width:100%}}
@media(min-width:576px){{.bar .meta{{width:auto}}}}
.tools{{display:flex;flex-wrap:wrap;gap:.4rem;align-items:center}}
.tools input{{padding:.25rem .5rem;border:1px solid #ced4da;border-radius:.25rem;min-width:10rem}}
.btn{{padding:.25rem .6rem;border:1px solid #adb5bd;border-radius:.25rem;background:#f8f9fa;cursor:pointer;font-size:.875rem}}
.btn:hover{{background:#e9ecef}}
main{{max-width:56rem;margin:0 auto;padding:1rem 1rem 3rem}}
.note{{font-size:.85rem;color:#6c757d;margin-bottom:1rem}}
.block{{background:#fff;border-radius:.5rem;padding:.85rem 1rem 1rem;margin-bottom:1rem;box-shadow:0 1px 3px rgba(0,0,0,.04)}}
.root-title{{font-size:.9rem;color:#6c757d;margin-bottom:.5rem}}
.ft-tree-root{{list-style:none;padding-left:0;margin:0}}
.ft-tree-children{{list-style:none;padding-left:1.25rem;margin:.15rem 0 .15rem .35rem;border-left:1px solid #dee2e6}}
.ft-tree-node{{margin:.2rem 0;line-height:1.45}}
.ft-tree-node.ft-hidden{{display:none}}
.ft-tree-toggle{{display:inline-flex;align-items:center;justify-content:center;width:1.25rem;height:1.25rem;padding:0;margin-right:.25rem;border:1px solid #adb5bd;border-radius:.2rem;background:#f8f9fa;cursor:pointer;vertical-align:middle}}
.ft-tree-toggle-spacer{{display:inline-block;width:1.25rem;margin-right:.25rem}}
.ft-tree-node.collapsed>.ft-tree-children{{display:none!important}}
.ft-tree-node mark{{background:#fff3cd;padding:0 .1rem}}
.person-link{{cursor:pointer;color:#0d6efd;text-decoration:none;border-bottom:1px dashed transparent}}
.person-link:hover{{border-bottom-color:#0d6efd}}
#detail{{display:none;margin-top:1rem;padding:1rem;background:#fff;border-radius:.5rem;border:1px solid #e4dfd4}}
#detail.show{{display:block}}
#detail h2{{margin:0 0 .5rem;font-size:1.1rem}}
#detail dl{{margin:0;display:grid;grid-template-columns:5rem 1fr;gap:.25rem .75rem;font-size:.9rem}}
#detail dt{{color:#6c757d}}
#detail .photo-grid{{display:flex;flex-wrap:wrap;gap:.5rem;margin-top:.75rem;grid-column:1/-1}}
#detail .photo-grid img{{max-width:140px;max-height:140px;object-fit:cover;border-radius:.35rem;border:1px solid #dee2e6}}
#detail .spouse-list{{margin:.25rem 0 0;padding-left:1.1rem;font-size:.9rem}}
.err{{color:#b02a37;padding:1rem}}
</style>
</head>
<body>
<header class=""bar"">
  <h1>{title}</h1>
  <span class=""meta"">离线包 · 导出 {exportDate} · {personCount} 人</span>
  <div class=""tools"">
    <input type=""search"" id=""q"" placeholder=""搜索姓名…"" autocomplete=""off""/>
    <button type=""button"" class=""btn"" id=""btnExpand"">全部展开</button>
    <button type=""button"" class=""btn"" id=""btnCollapse"">全部收起</button>
  </div>
</header>
<main>
  <p class=""note"">本页由族谱系统导出，数据来自同目录 genealogy.xml。点击 −/+ 收放支系，点击姓名查看简介、配偶与照片。</p>
  <div id=""app""></div>
  <div id=""detail""></div>
</main>
<script type=""application/json"" id=""ft-xml-b64"">{b64}</script>
<script>
(function(){{
  function decodeXml() {{
    var el = document.getElementById('ft-xml-b64');
    var b64 = (el && el.textContent) ? el.textContent.trim() : '';
    var bin = atob(b64);
    var bytes = new Uint8Array(bin.length);
    for (var i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
    return new TextDecoder('utf-8').decode(bytes);
  }}

  function parsePersons(doc) {{
    var map = {{}};
    doc.querySelectorAll('Persons > Person').forEach(function(p) {{
      var spouses = [];
      p.querySelectorAll(':scope > Marriage').forEach(function(m) {{
        spouses.push({{
          type: m.getAttribute('type') || '',
          spouseName: m.getAttribute('spouseName') || '',
          spouseBirth: m.getAttribute('spouseBirth') || ''
        }});
      }});
      var photos = [];
      p.querySelectorAll(':scope > Photo').forEach(function(ph) {{
        var f = ph.getAttribute('file');
        if (f) photos.push(f);
      }});
      map[p.getAttribute('id')] = {{
        id: p.getAttribute('id'),
        name: p.getAttribute('name') || '',
        fatherName: p.getAttribute('fatherName') || '',
        motherName: p.getAttribute('motherName') || '',
        birth: p.getAttribute('birth') || '',
        gender: p.getAttribute('gender') || '',
        generation: p.getAttribute('generation') || '',
        word: p.getAttribute('word') || '',
        death: p.getAttribute('death') || '',
        intro: p.getAttribute('intro') || '',
        spouses: spouses,
        photos: photos
      }};
    }});
    return map;
  }}

  function esc(s) {{
    return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/""/g,'&quot;');
  }}

  function highlight(name, q) {{
    if (!q) return esc(name);
    var i = name.toLowerCase().indexOf(q.toLowerCase());
    if (i < 0) return esc(name);
    return esc(name.slice(0,i)) + '<mark>' + esc(name.slice(i, i+q.length)) + '</mark>' + esc(name.slice(i+q.length));
  }}

  function renderNode(node, persons, q) {{
    var id = node.getAttribute('id');
    var name = node.getAttribute('name') || '';
    var birth = node.getAttribute('birth') || '';
    var kids = node.querySelectorAll(':scope > Node');
    var hasKids = kids.length > 0;
    var li = document.createElement('li');
    li.className = 'ft-tree-node' + (hasKids ? '' : ' ft-tree-leaf');
    li.setAttribute('data-name', name);
    li.setAttribute('data-id', id);
    if (q && name.toLowerCase().indexOf(q.toLowerCase()) < 0) {{
      var childMatch = false;
      kids.forEach(function(k) {{
        if ((k.getAttribute('name')||'').toLowerCase().indexOf(q.toLowerCase()) >= 0) childMatch = true;
      }});
      if (!childMatch) li.classList.add('ft-hidden');
    }}
    if (hasKids) {{
      var btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'ft-tree-toggle';
      btn.textContent = '−';
      btn.setAttribute('aria-expanded', 'true');
      li.appendChild(btn);
    }} else {{
      var sp = document.createElement('span');
      sp.className = 'ft-tree-toggle-spacer';
      li.appendChild(sp);
    }}
    var a = document.createElement('a');
    a.href = 'javascript:void(0)';
    a.className = 'person-link';
    a.innerHTML = '<strong>' + highlight(name, q) + '</strong>';
    if (birth) a.innerHTML += ' <span style=""color:#6c757d"">（' + esc(birth) + '）</span>';
    a.addEventListener('click', function() {{ showPerson(persons[id]); }});
    li.appendChild(a);
    if (hasKids) {{
      var ul = document.createElement('ul');
      ul.className = 'ft-tree-children';
      kids.forEach(function(k) {{ ul.appendChild(renderNode(k, persons, q)); }});
      li.appendChild(ul);
    }}
    return li;
  }}

  function showPerson(p) {{
    var box = document.getElementById('detail');
    if (!p) {{ box.classList.remove('show'); box.innerHTML = ''; return; }}
    var rows = [
      ['姓名', p.name], ['性别', p.gender], ['出生', p.birth || '—'],
      ['父亲', p.fatherName || '—'], ['母亲', p.motherName || '—']
    ];
    if (p.generation) rows.push(['世代', p.generation]);
    if (p.word) rows.push(['字辈', p.word]);
    if (p.death) rows.push(['卒葬', p.death]);
    if (p.spouses && p.spouses.length) {{
      var sp = p.spouses.map(function(s) {{
        var t = (s.type ? s.type + '：' : '') + (s.spouseName || '—');
        if (s.spouseBirth) t += '（' + s.spouseBirth + '）';
        return t;
      }}).join('；');
      rows.push(['配偶', sp]);
    }}
    var html = '<h2>' + esc(p.name) + '</h2><dl>';
    rows.forEach(function(r) {{ html += '<dt>' + esc(r[0]) + '</dt><dd>' + esc(r[1]) + '</dd>'; }});
    if (p.intro) html += '<dt>简介</dt><dd style=""grid-column:2"">' + esc(p.intro) + '</dd>';
    if (p.photos && p.photos.length) {{
      html += '<dt>照片</dt><dd class=""photo-grid"">';
      p.photos.forEach(function(src) {{
        html += '<img src=""' + esc(src) + '"" alt=""照片"" loading=""lazy""/>';
      }});
      html += '</dd>';
    }}
    html += '</dl>';
    box.innerHTML = html;
    box.classList.add('show');
    box.scrollIntoView({{behavior:'smooth', block:'nearest'}});
  }}

  function setExpanded(li, open) {{
    if (!li || !li.classList.contains('ft-tree-node')) return;
    var btn = li.querySelector('.ft-tree-toggle');
    if (!btn) return;
    li.classList.toggle('collapsed', !open);
    var kids = li.querySelector('.ft-tree-children');
    if (kids) kids.hidden = !open;
    btn.textContent = open ? '−' : '+';
  }}

  function bindTree() {{
    document.addEventListener('click', function(e) {{
      var t = e.target;
      if (t.closest('.ft-tree-toggle')) {{
        e.preventDefault();
        var li = t.closest('li.ft-tree-node');
        setExpanded(li, li.classList.contains('collapsed'));
        return;
      }}
      if (t.closest('#btnExpand')) {{
        e.preventDefault();
        document.querySelectorAll('li.ft-tree-node').forEach(function(li) {{ setExpanded(li, true); }});
        return;
      }}
      if (t.closest('#btnCollapse')) {{
        e.preventDefault();
        document.querySelectorAll('li.ft-tree-node').forEach(function(li) {{ setExpanded(li, false); }});
      }}
    }});
  }}

  var app = document.getElementById('app');
  try {{
    var xml = decodeXml();
    var doc = new DOMParser().parseFromString(xml, 'text/xml');
    if (doc.querySelector('parsererror')) throw new Error('XML 解析失败');
    var persons = parsePersons(doc);
    var trees = doc.querySelectorAll('Forest > Tree');
    if (!trees.length) {{
      app.innerHTML = '<p class=""err"">族谱树为空。</p>';
      return;
    }}
    trees.forEach(function(tree, idx) {{
      var block = document.createElement('div');
      block.className = 'block';
      block.setAttribute('data-tree-idx', String(idx));
      var rt = document.createElement('div');
      rt.className = 'root-title';
      rt.textContent = '根：' + (tree.getAttribute('rootName') || '');
      block.appendChild(rt);
      var ul = document.createElement('ul');
      ul.className = 'ft-tree-root';
      var rootNode = tree.querySelector(':scope > Node');
      if (rootNode) ul.appendChild(renderNode(rootNode, persons, ''));
      block.appendChild(ul);
      app.appendChild(block);
    }});
    bindTree();
    document.getElementById('q').addEventListener('input', function() {{
      var q = this.value.trim();
      app.querySelectorAll('.block').forEach(function(block) {{
        var idx = parseInt(block.getAttribute('data-tree-idx') || '0', 10);
        var tree = trees[idx];
        if (!tree) return;
        var ul = block.querySelector('.ft-tree-root');
        var rootNode = tree.querySelector(':scope > Node');
        ul.innerHTML = '';
        if (rootNode) ul.appendChild(renderNode(rootNode, persons, q));
      }});
    }});
  }} catch (err) {{
    app.innerHTML = '<p class=""err"">加载失败：' + esc(err.message || err) + '</p>';
  }}
}})();
</script>
</body>
</html>";
    }

    private static void WriteTextEntry(ZipArchive zip, string name, string content, Encoding enc)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, enc);
        writer.Write(content);
    }

    private static void WriteBinaryEntry(ZipArchive zip, string name, string diskPath)
    {
        var entry = zip.CreateEntry(name.Replace('\\', '/'), CompressionLevel.Optimal);
        using var outStream = entry.Open();
        using var inStream = File.OpenRead(diskPath);
        inStream.CopyTo(outStream);
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "族谱导出.zip" : name;
    }

    private static string EscapeHtml(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
