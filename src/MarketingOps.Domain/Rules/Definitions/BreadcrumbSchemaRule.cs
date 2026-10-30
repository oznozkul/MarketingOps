using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class BreadcrumbSchemaRule : RuleBase, IPageRule
{
    public override string Code => "seo.schema.breadcrumb";
    public override string Name => "BreadcrumbList yok";
    public override string Description => "Ana sayfa dışındaki indekslenebilir sayfalarda BreadcrumbList yapılandırılmış verisinin bulunup bulunmadığını kontrol eder.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:breadcrumbs";
    public override string Impact => "BreadcrumbList, sayfanın site içindeki yerini arama sonucunda adres yerine kategori yoluyla gösterir ve site yapısını anlatır.";
    protected override string TitleTemplate => "{n} sayfada BreadcrumbList yok";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Sayfadaki görünen breadcrumb, aynı adımlarla BreadcrumbList olarak işaretlenmeli.", "{\n  \"@type\": \"BreadcrumbList\",\n  \"itemListElement\": [\n    { \"@type\": \"ListItem\", \"position\": 1, \"name\": \"Ana sayfa\", \"item\": \"https://alanadi.com/\" },\n    { \"@type\": \"ListItem\", \"position\": 2, \"name\": \"Kategori\", \"item\": \"https://alanadi.com/kategori\" },\n    { \"@type\": \"ListItem\", \"position\": 3, \"name\": \"Sayfa adı\" }\n  ]\n}");
    public override string TeamCriterion => "Ana sayfa dışındaki şablonlar BreadcrumbList üretiyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page) || !PageChecks.HasMarkupFacts(page) || PageChecks.IsHomePage(page)) yield break;
        var data = page.Data.StructuredData;
        if (!data.Nodes.Any(n => n.Type.Equals("BreadcrumbList", StringComparison.OrdinalIgnoreCase))
            && !data.MicrodataTypes.Contains("BreadcrumbList", StringComparer.OrdinalIgnoreCase))
            yield return Violation(page, "BreadcrumbList yok");
    }
}
