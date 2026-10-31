using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>
/// Sitemap'te listelenip taranan hiçbir sayfadan link almayan indekslenebilir sayfalar (ana sayfa hariç).
/// Kaynak <see cref="RuleDataSource.FullCrawl"/>'tır: yalnızca tarama tamamlanmış ve sayfa limiti aşılmamışsa çalışır;
/// yarım taramada bulgusu yanlışlıkla çözülmez.
/// </summary>
public sealed class OrphanPageRule : RuleBase, ISiteRule
{
    public override RuleDataSource DataSource => RuleDataSource.FullCrawl;
    public override string Code => "seo.links.orphan";
    public override string Name => "Yetim sayfa";
    public override string Description => "Sitemap'te olup hiçbir sayfadan iç bağlantı almayan indekslenebilir sayfaları bulur.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Site;
    public override string ReferenceKey => "google:sitemaps";
    public override string Impact => "Hiçbir sayfadan bağlantı almayan sayfaya arama motoru zor ulaşır ve içerik değerini aktaramaz; ziyaretçi de bu sayfayı gezinmeden bulamaz.";
    protected override string TitleTemplate => "{n} yetim sayfa";
    public override Team Team => Team.Seo;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Yetim sayfaya ana içerikten bağlantı verilmeli ya da sayfa sitemap'ten çıkarılmalı.", "1. Sayfaya menüden, listelerden ya da ilgili içerikten iç bağlantı verin.\n2. Sayfa indekslenmeyecekse sitemap'ten çıkarın.\n3. Site içi bağlantı yapısını gözden geçirin.");
    public override string TeamCriterion => "Yetim sayfalar iç bağlantı aldı ya da bilinçli olarak sitemap'ten çıkarıldı.";

    public ISiteRuleRun Start(RuleContext context) => new Run(Code);

    private sealed class Run(string ruleCode) : ISiteRuleRun
    {
        private readonly HashSet<string> _linked = new(StringComparer.Ordinal);
        private readonly List<(string Url, string Key, Guid? PageTypeId)> _candidates = [];

        public void Observe(PageSnapshot page)
        {
            if (PageChecks.IsHtml(page))
                foreach (var link in page.Data.InternalLinks)
                    if (Uri.TryCreate(link, UriKind.Absolute, out var url)) _linked.Add(UrlKey.Normalize(url));
            if (page.InSitemap && PageChecks.IsIndexable(page) && !PageChecks.IsHomePage(page))
                _candidates.Add((page.Url, page.NormalizedUrl, page.PageTypeId));
        }

        public IEnumerable<RuleViolation> Complete()
        {
            foreach (var (url, key, pageTypeId) in _candidates)
                if (!_linked.Contains(key))
                    yield return new RuleViolation(ruleCode, pageTypeId, url, "Bağlantı yok", "Sitemap'te listeleniyor ama taranan hiçbir sayfadan link almıyor.");
        }
    }
}
