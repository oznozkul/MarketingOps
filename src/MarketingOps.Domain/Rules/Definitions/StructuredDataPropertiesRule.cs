using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class StructuredDataPropertiesRule : RuleBase, IPageRule
{
    public override string Code => "seo.schema.properties";
    public override string Name => "Structured data'da zorunlu alan eksik";
    public override string Description => "Product, BreadcrumbList, Organization, WebSite, Article, FAQPage, ItemList ve LocalBusiness düğümlerinde Google'ın zengin sonuç için istediği alanların dolu olup olmadığını kontrol eder.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:structured-data";
    public override string Impact => "Zorunlu alanı eksik düğüm geçersiz sayılır; ürün sayfasında fiyat ve stok gibi bilgiler arama sonucunda gösterilmez.";
    protected override string TitleTemplate => "{n} sayfada structured data'da zorunlu alan eksik";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Bulguda yazan eksik alanlar JSON-LD'ye eklenmeli. Örnek, eksiksiz bir Product teklifidir.", "\"offers\": {\n  \"@type\": \"Offer\",\n  \"price\": \"3450.00\",\n  \"priceCurrency\": \"TRY\",\n  \"availability\": \"https://schema.org/InStock\",\n  \"url\": \"https://alanadi.com/urun\"\n}");
    public override string TeamCriterion => "Eksik alanlar şablona eklendi; Google Zengin Sonuçlar Testi uyarısız.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page) || !PageChecks.HasMarkupFacts(page)) yield break;
        var missing = page.Data.StructuredData.Nodes
            .Select(n => (n.Type, Missing: SchemaRequirements.Missing(n)))
            .Where(n => n.Missing.Count > 0)
            .DistinctBy(n => (n.Type, string.Join('|', n.Missing)))
            .ToList();
        if (missing.Count == 0) yield break;
        yield return Violation(page,
            string.Join(", ", missing.Select(m => $"{m.Type}: {m.Missing[0]}")),
            string.Join(" · ", missing.Select(m => $"{m.Type}: {string.Join(", ", m.Missing)}")));
    }
}
