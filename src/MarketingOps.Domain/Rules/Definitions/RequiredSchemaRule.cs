using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Sites;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class RequiredSchemaRule : RuleBase, IPageRule
{
    public override string Code => "seo.schema.required_missing";
    public override string Name => "Beklenen schema eksik";
    public override string Description => "Sayfa tipinin koşullarında tanımlı schema tiplerinin sayfada bulunup bulunmadığını kontrol eder (ör. Ürün tipinde Product).";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:structured-data";
    public override string Impact => "Yapılandırılmış veri olmadan sayfa arama sonuçlarında fiyat, stok veya puan gibi zengin sonuçlarla gösterilemez.";
    protected override string TitleTemplate => "{n} sayfada beklenen schema eksik";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Sayfa tipinin beklenen yapılandırılmış verisi JSON-LD olarak eklenmeli. Örnek Product içindir; beklenen tür bulguda yazar.", "<script type=\"application/ld+json\">\n{\n  \"@context\": \"https://schema.org\",\n  \"@type\": \"Product\",\n  \"name\": \"Ürün adı\",\n  \"image\": \"https://alanadi.com/urun.jpg\",\n  \"brand\": { \"@type\": \"Brand\", \"name\": \"Marka\" },\n  \"offers\": {\n    \"@type\": \"Offer\",\n    \"priceCurrency\": \"TRY\",\n    \"price\": \"0.00\",\n    \"availability\": \"https://schema.org/InStock\"\n  }\n}\n</script>");
    public override string TeamCriterion => "JSON-LD şablona eklendi ve Google Zengin Sonuçlar Testi'nden hatasız geçti.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page) || page.PageTypeId is not { } pageTypeId) yield break;
        var pageType = context.Site.PageTypes.FirstOrDefault(p => p.Id == pageTypeId);
        if (pageType is null) yield break;
        var expected = pageType.Conditions.Where(c => c.Type == MatchConditionType.SchemaType).Select(c => c.Value).ToList();
        var missing = expected.Where(e => !page.Data.SchemaTypes.Contains(e, StringComparer.OrdinalIgnoreCase)).ToList();
        if (missing.Count > 0)
            yield return Violation(page, page.Data.SchemaTypes.Count == 0 ? "Schema yok" : string.Join(", ", page.Data.SchemaTypes), $"Beklenen: {string.Join(", ", missing)}");
    }
}
