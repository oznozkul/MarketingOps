using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Sites;

namespace MarketingOps.UnitTests.Domain.Rules;

/// <summary>Kural testleri için snapshot ve bağlam üretimi. Varsayılan sayfa hiçbir kuralı ihlal etmez.</summary>
public static class RuleFixture
{
    public static readonly DateTimeOffset T0 = new(2026, 10, 22, 2, 0, 0, TimeSpan.Zero);
    private static readonly Guid CrawlId = Guid.CreateVersion7();

    public static Site NewSite() => Site.Create(Guid.CreateVersion7(), "Örnek", "example.com", T0);

    public static PageSnapshotData Good { get; } = new()
    {
        Title = "Yaz lastikleri ve jant modelleri | Örnek",
        MetaDescription = "Yaz lastiği modelleri, fiyatları ve araç uyumluluğu. Aynı gün kargo ve ücretsiz montaj.",
        Canonical = "https://example.com/sayfa",
        H1 = ["Yaz lastikleri"],
        WordCount = 400
    };

    /// <summary>Faz 8 işaretleme alanları dolu ve hiçbir kuralı ihlal etmeyen sayfa.</summary>
    public static PageSnapshotData GoodMarkup { get; } = Good with
    {
        Version = PageSnapshotData.MarkupVersion,
        Lang = "tr",
        Viewport = "width=device-width, initial-scale=1",
        OpenGraph = new OpenGraphInfo("Yaz lastikleri", "Yaz lastiği modelleri", "https://example.com/og.jpg", "website", "https://example.com/sayfa",
            "1200", "630", "Yaz lastikleri"),
        StructuredData = new StructuredDataInfo
        {
            JsonLdBlocks = 1,
            Nodes =
            [
                new SchemaNode { Type = "WebPage", TopLevel = true, Properties = ["url", "name"] },
                new SchemaNode { Type = "BreadcrumbList", TopLevel = true, Properties = ["itemListElement", "itemListElement.position", "itemListElement.name", "itemListElement.item"] }
            ]
        },
        Headings = new HeadingFacts { Levels = [1, 2, 3, 2] },
        Landmarks = new LandmarkFacts { Main = 1, Header = 1, Nav = 1, Footer = 1 }
    };

    public static ElementIssue Issue(params string[] samples) => new() { Count = samples.Length, Samples = samples };

    public static PageSnapshot Page(
        PageSnapshotData? data = null, string path = "/sayfa", int? status = 200, Guid? pageTypeId = null,
        string? encoding = "gzip", int? sizeBytes = 40_000, int? ttfbMs = 120, IReadOnlyList<RedirectHop>? chain = null, bool inSitemap = false)
    {
        var url = $"https://example.com{path}";
        return PageSnapshot.Create(CrawlId, Guid.CreateVersion7(), url, url, pageTypeId, status, chain ?? [], ttfbMs, "text/html; charset=utf-8",
            encoding, null, sizeBytes, status is null ? "Zaman aşımı." : null, T0, data ?? Good, inSitemap);
    }

    public static RuleContext Context(Site? site = null, IReadOnlyDictionary<string, decimal>? thresholds = null,
        IReadOnlySet<string>? failedUrls = null, IReadOnlyList<AssetCheck>? assets = null) =>
        new(site ?? NewSite(), thresholds ?? new Dictionary<string, decimal>(), failedUrls ?? new HashSet<string>(), assets ?? []);

    /// <summary>Sayfa ya da site kuralını aynı şekilde çalıştırır.</summary>
    public static IReadOnlyList<RuleViolation> Run(IRule rule, RuleContext context, params PageSnapshot[] pages)
    {
        if (rule is IPageRule pageRule) return pages.SelectMany(p => pageRule.Evaluate(p, context)).ToList();
        var run = ((ISiteRule)rule).Start(context);
        foreach (var page in pages) run.Observe(page);
        return run.Complete().ToList();
    }

    public static IReadOnlyList<RuleViolation> Run(IRule rule, params PageSnapshot[] pages) => Run(rule, Context(), pages);
}
