using System.Text.Json;
using AngleSharp.Dom;
using MarketingOps.Domain.Crawls;

namespace MarketingOps.Infrastructure.Crawling;

/// <summary>
/// JSON-LD bloklarını düğümlere ayırır. Her düğüm için değeri dolu özellik yolları tutulur; zorunlu alan kontrolü
/// (Domain'deki SchemaRequirements) yalnızca bu yollara bakar, ham JSON saklanmaz.
/// </summary>
public static class StructuredDataExtractor
{
    private const int MaxNodes = 40;
    private const int MaxPropertiesPerNode = 60;
    private const int MaxPathDepth = 3;
    private const int InvalidSampleLength = 80;

    public static StructuredDataInfo Extract(IDocument document)
    {
        var blocks = document.QuerySelectorAll("script").Where(IsJsonLd).ToList();
        var nodes = new List<SchemaNode>();
        var invalid = new List<string>();
        foreach (var block in blocks)
        {
            try
            {
                using var json = JsonDocument.Parse(block.TextContent);
                CollectNodes(json.RootElement, topLevel: true, nodes);
            }
            catch (JsonException)
            {
                invalid.Add(Sample(block.TextContent));
            }
        }

        return new StructuredDataInfo
        {
            JsonLdBlocks = blocks.Count,
            Invalid = new ElementIssue { Count = invalid.Count, Samples = invalid.Take(ElementIssue.MaxSamples).ToList() },
            Nodes = nodes.Take(MaxNodes).ToList(),
            MicrodataTypes = document.QuerySelectorAll("[itemscope][itemtype]")
                .Where(e => e.ParentElement?.Closest("[itemscope]") is null)
                .Select(e => e.GetAttribute("itemtype")!.TrimEnd('/').Split('/')[^1])
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    public static bool IsJsonLd(IElement script) =>
        string.Equals(script.GetAttribute("type"), "application/ld+json", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Kökteki ve @graph'taki düğümler üst düzeydir. İç içe tipli nesneler (ör. WebPage.breadcrumb) de düğüm olarak
    /// eklenir ki varlıkları sayılsın; ama zorunlu alanları yalnızca üst düzeyde denetlenir (ör. Article.publisher'daki
    /// Organization'dan url beklenmez).
    /// </summary>
    private static void CollectNodes(JsonElement element, bool topLevel, List<SchemaNode> nodes)
    {
        if (nodes.Count >= MaxNodes) return;
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray()) CollectNodes(item, topLevel, nodes);
                break;
            case JsonValueKind.Object:
                var properties = new List<string>();
                Flatten(element, "", 1, properties);
                foreach (var type in Types(element))
                    nodes.Add(new SchemaNode { Type = type, TopLevel = topLevel, Properties = properties.Take(MaxPropertiesPerNode).ToList() });
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name == "@graph") CollectNodes(property.Value, topLevel, nodes);
                    else if (!property.Name.StartsWith('@')) CollectNodes(property.Value, topLevel: false, nodes);
                }
                break;
        }
    }

    private static IEnumerable<string> Types(JsonElement element)
    {
        if (!element.TryGetProperty("@type", out var type)) return [];
        if (type.ValueKind == JsonValueKind.String) return [type.GetString()!];
        return type.ValueKind == JsonValueKind.Array
            ? type.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String).Select(t => t.GetString()!)
            : [];
    }

    private static void Flatten(JsonElement element, string prefix, int depth, List<string> paths)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.StartsWith('@') || !HasValue(property.Value)) continue;
            var path = prefix + property.Name;
            if (!paths.Contains(path)) paths.Add(path);
            if (depth >= MaxPathDepth) continue;
            foreach (var child in Objects(property.Value)) Flatten(child, path + ".", depth + 1, paths);
        }
    }

    private static IEnumerable<JsonElement> Objects(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => [value],
        JsonValueKind.Array => value.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.Object),
        _ => []
    };

    private static bool HasValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => !string.IsNullOrWhiteSpace(value.GetString()),
        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => true,
        JsonValueKind.Object => value.EnumerateObject().Any(p => HasValue(p.Value)),
        JsonValueKind.Array => value.EnumerateArray().Any(HasValue),
        _ => false
    };

    private static string Sample(string text)
    {
        var trimmed = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return trimmed.Length <= InvalidSampleLength ? trimmed : trimmed[..InvalidSampleLength] + "…";
    }
}
