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
        new StructuredDataMissingRule(),
        new StructuredDataInvalidRule(),
        new StructuredDataPropertiesRule(),
        new SiteIdentitySchemaRule(),
        new BreadcrumbSchemaRule(),
        new OpenGraphIncompleteRule(),
        new OpenGraphImageMetaRule(),
        new OpenGraphUrlMismatchRule(),
        new MetaDescriptionLengthRule(),
        new GenericLinkTextRule(),
        new EmptyHrefRule(),
        new BlankNoopenerRule(),
        new ThinContentRule(),
        new CompressionMissingRule(),
        new StaticCacheControlRule(),
        new RenderBlockingScriptRule(),
        new ImageDimensionsRule(),
        new LazyLoadMissingRule(),
        new PreconnectRule(),
        new HtmlSizeRule(),
        new IframeLazyRule(),
        new TtfbRule(),
        new OpportunityRule(),
        new NotIndexedRule(),
        new LcpRule(),
        new InpRule(),
        new ClsRule(),
        new TagMissingRule(),
        new TagDuplicateRule(),
        new TagWrongIdRule(),
        new ConsentPrefireRule(),
        new PurchaseGapRule(),
        new EventAnomalyRule(),
        new MetaPixelCapiRule(),
        new MetaDedupRule(),
        new LinkNoTextRule(),
        new HeadingSkipRule(),
        new TableHeaderRule(),
        new IframeTitleRule(),
        new FormLabelRule(),
        new LandmarksRule(),
        new NavLabelRule(),
        new ViewportRule(),
        new LangRule()
    ];

    public static IRule? Find(string code) => All.FirstOrDefault(r => r.Code == code);
}
