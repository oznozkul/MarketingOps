using MarketingOps.Domain.Rules.Definitions;

namespace MarketingOps.Domain.Rules;

/// <summary>Kodda tanımlı tüm kurallar. Yeni kural eklemek için sınıfı yazıp buraya eklemek yeterli.</summary>
public static class RuleCatalog
{
    public static IReadOnlyList<IRule> All { get; } =
    [
        new TitleMissingRule(),
        new TitleDuplicateRule(),
        new TitleLengthRule(),
        new MetaDescriptionMissingRule(),
        new MetaDescriptionDuplicateRule(),
        new HeadingRule(),
        new CanonicalRule(),
        new NoindexInSitemapRule(),
        new BrokenInternalLinksRule(),
        new RedirectChainRule(),
        new ImageAltMissingRule(),
        new RequiredSchemaRule(),
        new ThinContentRule(),
        new CompressionMissingRule(),
        new StaticCacheControlRule(),
        new RenderBlockingScriptRule(),
        new ImageDimensionsRule(),
        new LazyLoadMissingRule(),
        new PreconnectRule(),
        new HtmlSizeRule(),
        new TtfbRule()
    ];

    public static IRule? Find(string code) => All.FirstOrDefault(r => r.Code == code);
}
