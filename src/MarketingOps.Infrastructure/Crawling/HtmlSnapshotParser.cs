using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using MarketingOps.Domain.Crawls;

namespace MarketingOps.Infrastructure.Crawling;

/// <summary>HTML'den <see cref="PageSnapshotData"/> çıkarır. Linkler mutlak URL'e çevrilir, iç/dış olarak ayrılır.</summary>
public static partial class HtmlSnapshotParser
{
    private const int MaxLinks = 500;
    private const int MaxImages = 300;
    private const int MaxTextLength = 500;

    public static ParsedPage Parse(string html, Uri pageUrl, Func<Uri, bool> isInternal)
    {
        var document = new HtmlParser().ParseDocument(html);
        var head = document.Head;

        string? Meta(string name) => document.QuerySelector($"meta[name='{name}' i]")?.GetAttribute("content")?.Trim();
        string? Og(string property) => document.QuerySelector($"meta[property='og:{property}' i]")?.GetAttribute("content")?.Trim();
        Uri? Resolve(string? href) => UrlNormalizer.Resolve(pageUrl, href);

        var linkUrls = document.QuerySelectorAll("a[href]").Select(a => Resolve(a.GetAttribute("href"))).OfType<Uri>().ToList();
        var internalLinks = linkUrls.Where(isInternal).DistinctBy(UrlNormalizer.Normalize).Take(MaxLinks).ToList();
        var externalLinks = linkUrls.Where(u => !isInternal(u)).Select(u => u.AbsoluteUri).Distinct().Take(MaxLinks).ToList();

        var data = new PageSnapshotData
        {
            Title = Clip(document.Title),
            MetaDescription = Clip(Meta("description")),
            Canonical = Resolve(document.QuerySelector("link[rel~='canonical' i]")?.GetAttribute("href"))?.AbsoluteUri,
            MetaRobots = Meta("robots"),
            H1 = Texts(document, "h1"),
            H2 = Texts(document, "h2"),
            Lang = document.DocumentElement.GetAttribute("lang"),
            Viewport = Meta("viewport"),
            Charset = document.QuerySelector("meta[charset]")?.GetAttribute("charset")
                      ?? CharsetRegex().Match(document.QuerySelector("meta[http-equiv='content-type' i]")?.GetAttribute("content") ?? "").Groups[1].Value.NullIfEmpty(),
            SchemaTypes = SchemaTypes(document),
            OpenGraph = Og("title") is null && Og("description") is null && Og("image") is null
                ? null
                : new OpenGraphInfo(Og("title"), Og("description"), Og("image"), Og("type"), Og("url"),
                    Og("image:width"), Og("image:height"), Og("image:alt")),
            InternalLinks = internalLinks.Select(u => u.AbsoluteUri).ToList(),
            ExternalLinks = externalLinks,
            Images = document.QuerySelectorAll("img").Take(MaxImages)
                .Select(i => new ImageInfo(i.GetAttribute("src") ?? i.GetAttribute("data-src") ?? "", i.GetAttribute("alt"),
                    i.GetAttribute("width"), i.GetAttribute("height"), i.GetAttribute("loading")))
                .ToList(),
            Scripts = document.QuerySelectorAll("script")
                .Where(s => !StructuredDataExtractor.IsJsonLd(s))
                .Select(s => new ScriptInfo(Resolve(s.GetAttribute("src"))?.AbsoluteUri, s.HasAttribute("async"), s.HasAttribute("defer"),
                    head is not null && head.Contains(s)))
                .ToList(),
            Stylesheets = document.QuerySelectorAll("link[rel~='stylesheet' i]").Select(l => Resolve(l.GetAttribute("href"))?.AbsoluteUri).OfType<string>().ToList(),
            Preloads = document.QuerySelectorAll("link[rel~='preload' i]")
                .Select(l => (Href: Resolve(l.GetAttribute("href"))?.AbsoluteUri, As: l.GetAttribute("as")))
                .Where(l => l.Href is not null)
                .Select(l => new PreloadInfo(l.Href!, l.As))
                .ToList(),
            Preconnects = document.QuerySelectorAll("link[rel~='preconnect' i]").Select(l => l.GetAttribute("href")).OfType<string>().ToList(),
            WordCount = CountWords(document),
            Version = PageSnapshotData.MarkupVersion,
            StructuredData = StructuredDataExtractor.Extract(document),
            Links = MarkupExtractor.Links(document),
            Headings = MarkupExtractor.Headings(document),
            Forms = MarkupExtractor.Forms(document),
            Tables = MarkupExtractor.Tables(document),
            Iframes = MarkupExtractor.Iframes(document),
            Landmarks = MarkupExtractor.Landmarks(document)
        };
        return new ParsedPage(data, internalLinks);
    }

    private static IReadOnlyList<string> Texts(IDocument document, string selector) =>
        document.QuerySelectorAll(selector).Select(e => Clip(WhitespaceRegex().Replace(e.TextContent, " ").Trim())).OfType<string>().Where(t => t.Length > 0).ToList();

    /// <summary>JSON-LD (@graph dahil) ve microdata'daki üst düzey schema tipleri.</summary>
    private static IReadOnlyList<string> SchemaTypes(IDocument document)
    {
        var types = new List<string>();
        foreach (var script in document.QuerySelectorAll("script").Where(StructuredDataExtractor.IsJsonLd))
        {
            try
            {
                using var json = JsonDocument.Parse(script.TextContent);
                CollectTypes(json.RootElement, types);
            }
            catch (JsonException)
            {
                // Bozuk JSON-LD'nin tipi okunamaz; seo.schema.invalid kuralı StructuredData.Invalid'den raporlar.
            }
        }
        types.AddRange(document.QuerySelectorAll("[itemscope][itemtype]")
            .Where(e => e.ParentElement?.Closest("[itemscope]") is null)
            .Select(e => e.GetAttribute("itemtype")!.TrimEnd('/').Split('/')[^1]));
        return types.Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void CollectTypes(JsonElement element, List<string> types)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray()) CollectTypes(item, types);
                break;
            case JsonValueKind.Object:
                if (element.TryGetProperty("@type", out var type))
                {
                    if (type.ValueKind == JsonValueKind.String) types.Add(type.GetString()!);
                    else if (type.ValueKind == JsonValueKind.Array)
                        types.AddRange(type.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String).Select(t => t.GetString()!));
                }
                if (element.TryGetProperty("@graph", out var graph)) CollectTypes(graph, types);
                break;
        }
    }

    private static int CountWords(IDocument document)
    {
        var body = document.Body;
        if (body is null) return 0;
        var clone = (IElement)body.Clone();
        foreach (var node in clone.QuerySelectorAll("script, style, noscript, template").ToList()) node.Remove();
        return WhitespaceRegex().Split(clone.TextContent).Count(w => w.Any(char.IsLetterOrDigit));
    }

    private static string? Clip(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Length <= MaxTextLength ? value.Trim() : value[..MaxTextLength].Trim();

    private static string? NullIfEmpty(this string value) => value.Length == 0 ? null : value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"charset=([\w-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CharsetRegex();
}
