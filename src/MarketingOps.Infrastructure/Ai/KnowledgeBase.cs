using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Infrastructure.Ai;

/// <summary>
/// /knowledge klasöründeki iç dokümanlar. Markdown dosyalarında <c>&lt;!-- ref: perf-doc:p30 --&gt;</c> işaretinden sonraki metin,
/// bir sonraki işarete kadar o anahtara aittir. AI'a yalnızca kuralın ReferenceKey'ine ait parçalar verilir; vektör arama yok,
/// bu ölçekte anahtar eşleşmesi yeterli. Dosyalar açılışta bir kez okunur.
/// </summary>
public sealed partial class KnowledgeBase
{
    private const int MaxChunkLength = 6000;
    private readonly Dictionary<string, List<string>> _chunks = new(StringComparer.OrdinalIgnoreCase);

    public KnowledgeBase(string path, ILogger<KnowledgeBase> logger)
    {
        if (!Directory.Exists(path))
        {
            logger.LogInformation("Bilgi tabanı klasörü yok ({Path}); öneriler yalnızca kural açıklamalarıyla üretilecek.", path);
            return;
        }
        // README biçimi anlatır ve örnek işaretler içerir; doküman değildir.
        foreach (var file in Directory.EnumerateFiles(path, "*.md", SearchOption.AllDirectories)
                     .Where(f => !Path.GetFileName(f).Equals("README.md", StringComparison.OrdinalIgnoreCase)))
            Load(File.ReadAllText(file));
        logger.LogInformation("Bilgi tabanı: {Count} anahtar yüklendi.", _chunks.Count);
    }

    public IReadOnlyList<string> For(string referenceKey) => _chunks.TryGetValue(referenceKey, out var list) ? list : [];

    private void Load(string text)
    {
        var markers = Marker().Matches(text);
        for (var i = 0; i < markers.Count; i++)
        {
            var start = markers[i].Index + markers[i].Length;
            var end = i + 1 < markers.Count ? markers[i + 1].Index : text.Length;
            var chunk = text[start..end].Trim();
            if (chunk.Length == 0) continue;
            if (chunk.Length > MaxChunkLength) chunk = chunk[..MaxChunkLength];
            var key = markers[i].Groups[1].Value.Trim();
            if (!_chunks.TryGetValue(key, out var list)) _chunks[key] = list = [];
            list.Add(chunk);
        }
    }

    [GeneratedRegex(@"<!--\s*ref:\s*([^>]+?)\s*-->", RegexOptions.IgnoreCase)]
    private static partial Regex Marker();
}
