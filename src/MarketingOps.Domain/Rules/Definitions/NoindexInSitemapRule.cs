using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class NoindexInSitemapRule : RuleBase, IPageRule
{
    public override string Code => "seo.noindex.in_sitemap";
    public override string Name => "Noindex sayfa sitemap'te";
    public override string Description => "Sitemap'te listelenip noindex dönen sayfaları bulur.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:sitemaps";
    public override string Impact => "Sitemap, indekslenmesi istenen sayfaları bildirir; noindex ile çelişen sinyal tarama bütçesini boşa harcar ve hatalı bir noindex'i gizleyebilir.";
    protected override string TitleTemplate => "{n} noindex sayfa sitemap'te yer alıyor";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Sitemap yalnızca indekslenmesi istenen sayfaları içermeli.", "1. Sayfanın indekslenmesi isteniyorsa noindex etiketini kaldırın.\n2. İndekslenmemesi isteniyorsa sayfayı sitemap üretiminden çıkarın.\n3. Sitemap'i yeniden üretip Search Console'a gönderin.");
    public override string TeamCriterion => "Sitemap üretimi noindex sayfaları dışarıda bırakacak şekilde güncellendi.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (page.InSitemap && PageChecks.IsHtml(page) && PageChecks.IsNoindex(page))
            yield return Violation(page, page.Data.MetaRobots);
    }
}
