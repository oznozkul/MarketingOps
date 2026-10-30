using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class SiteIdentitySchemaRule : RuleBase, IPageRule
{
    public override string Code => "seo.schema.site_identity";
    public override string Name => "Ana sayfada site kimliği yok";
    public override string Description => "Ana sayfada sitenin kim olduğunu anlatan Organization (ya da OnlineStore, LocalBusiness gibi alt tipi) ve WebSite düğümlerinin bulunup bulunmadığını kontrol eder.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:organization";
    public override string Impact => "Organization ve WebSite, arama motorunun markayı, logoyu ve sitenin adını doğru göstermesini sağlar.";
    protected override string TitleTemplate => "Ana sayfada site kimliği eksik";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Ana sayfa Organization ve WebSite düğümlerini birlikte taşımalı.", "{ \"@type\": \"Organization\", \"@id\": \"https://alanadi.com/#organization\", \"name\": \"Marka\", \"url\": \"https://alanadi.com/\", \"logo\": \"https://alanadi.com/logo.png\" },\n{ \"@type\": \"WebSite\", \"@id\": \"https://alanadi.com/#website\", \"name\": \"Marka\", \"url\": \"https://alanadi.com/\", \"publisher\": { \"@id\": \"https://alanadi.com/#organization\" } }");
    public override string TeamCriterion => "Ana sayfa şablonu Organization ve WebSite düğümlerini üretiyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page) || !PageChecks.HasMarkupFacts(page) || !PageChecks.IsHomePage(page)) yield break;
        var data = page.Data.StructuredData;
        var types = data.Nodes.Select(n => n.Type).Concat(data.MicrodataTypes).ToList();
        var missing = new List<string>();
        if (!types.Any(SchemaRequirements.IsOrganization)) missing.Add("Organization");
        if (!types.Contains("WebSite", StringComparer.OrdinalIgnoreCase)) missing.Add("WebSite");
        if (missing.Count > 0) yield return Violation(page, $"Eksik: {string.Join(", ", missing)}");
    }
}
