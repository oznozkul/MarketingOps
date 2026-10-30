using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class StructuredDataMissingRule : RuleBase, IPageRule
{
    public override string Code => "seo.schema.missing";
    public override string Name => "Structured data yok";
    public override string Description => "Hiç yapılandırılmış verisi (geçerli JSON-LD ya da microdata) olmayan indekslenebilir sayfaları bulur. Sayfa tipine beklenen schema tanımlanmamış olsa da çalışır.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:structured-data";
    public override string Impact => "Yapılandırılmış veri olmayan sayfa, arama motoruna ne olduğunu (ürün, kategori, makale) açıkça söylemez ve zengin sonuçlarda gösterilemez.";
    protected override string TitleTemplate => "{n} sayfada structured data yok";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Her sayfa şablonu, sayfanın türünü anlatan JSON-LD taşımalı. En az WebPage ve BreadcrumbList; ürün sayfasında Product.", "<script type=\"application/ld+json\">\n{\n  \"@context\": \"https://schema.org\",\n  \"@graph\": [\n    { \"@type\": \"WebPage\", \"@id\": \"https://alanadi.com/sayfa#webpage\", \"url\": \"https://alanadi.com/sayfa\", \"name\": \"Sayfa başlığı\" },\n    { \"@type\": \"BreadcrumbList\", \"itemListElement\": [\n      { \"@type\": \"ListItem\", \"position\": 1, \"name\": \"Ana sayfa\", \"item\": \"https://alanadi.com/\" },\n      { \"@type\": \"ListItem\", \"position\": 2, \"name\": \"Sayfa\" }\n    ] }\n  ]\n}\n</script>");
    public override string TeamCriterion => "Şablon JSON-LD üretiyor ve Google Zengin Sonuçlar Testi'nden hatasız geçiyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (PageChecks.IsIndexable(page) && PageChecks.HasMarkupFacts(page) && !page.Data.StructuredData.IsPresent())
            yield return Violation(page, page.Data.StructuredData.Invalid.Count > 0 ? "Yalnızca bozuk JSON-LD var" : "Structured data yok");
    }
}
