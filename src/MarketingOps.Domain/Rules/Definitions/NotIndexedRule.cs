using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>İndekslenebilir olduğu halde Google'ın indekslemediği sayfalar (Search Console URL Inspection).</summary>
public sealed class NotIndexedRule : RuleBase, IPageRule
{
    public override RuleDataSource DataSource => RuleDataSource.UrlInspection;

    public override string Code => "seo.index.not_indexed";
    public override string Name => "İndekslenmemiş sayfa";
    public override string Description => "Taramada indekslenebilir görünen ama Search Console'a göre Google dizininde olmayan sayfaları bulur.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:sitemaps";
    public override string Impact => "Dizinde olmayan sayfa arama sonuçlarında hiç görünmez; bu sayfalardan organik trafik gelmez.";
    protected override string TitleTemplate => "{n} sayfa Google dizininde değil";
    public override Team Team => Team.Seo;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Google'ın sayfayı neden dizine almadığını Search Console'daki kapsam durumuna göre giderin.",
        "1. Mevcut değerdeki kapsam durumunu Search Console'daki URL denetimiyle karşılaştırın.\n2. Sayfa yinelenen ya da zayıf içerik olarak görülüyorsa canonical ve içeriği düzeltin.\n3. Sayfanın sitemap'te ve site içi bağlantılarda yer aldığından emin olun, ardından dizine eklenmesini isteyin.");
    public override string TeamCriterion => "Sayfaların kapsam sorunu giderildi ve dizine ekleme istendi.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page) || !context.External.IndexByUrl.TryGetValue(page.NormalizedUrl, out var index) || index.IsIndexed) yield break;
        yield return Violation(page, index.CoverageState ?? index.Verdict, $"URL denetimi sonucu: {index.Verdict}");
    }
}
