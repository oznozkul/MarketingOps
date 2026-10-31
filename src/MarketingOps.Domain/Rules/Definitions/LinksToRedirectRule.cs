using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>HTML sayfalardaki iç linklerden, taramada yönlendirme zinciri olan adreslere gidenler.</summary>
public sealed class LinksToRedirectRule : RuleBase, ISiteRule
{
    public override string Code => "seo.links.to_redirect";
    public override string Name => "Yönlenen adrese iç bağlantı";
    public override string Description => "İç bağlantıların yönlendirme yapan adreslere gitmesini bulur; bağlantı kalıcı hedefe değil eski adrese verilmiş olabilir.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Site;
    public override string ReferenceKey => "google:redirects";
    public override string Impact => "Yönlendirme yapan bir adrese verilen her iç bağlantı sayfayı yavaşlatır ve tarama bütçesini boşa harcar; bağlantı doğrudan kalıcı hedefe verilmelidir.";
    protected override string TitleTemplate => "{n} sayfada yönlenen adrese iç bağlantı var";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "İç bağlantılar doğrudan kalıcı (301 hedefi) adrese verilmeli.", "1. Aşağıdaki tablodaki bağlantıları yönlendirmenin son hedefine güncelleyin.\n2. Şablon ve menülerdeki eski adresleri bulun.\n3. Güncellenemeyen bağlantıyı kaldırın.");
    public override string TeamCriterion => "İç bağlantılar yönlendirmesiz kalıcı adreslere gidiyor.";

    public ISiteRuleRun Start(RuleContext context) => new Run(Code);

    private sealed class Run(string ruleCode) : ISiteRuleRun
    {
        private readonly HashSet<string> _redirecting = new(StringComparer.Ordinal);
        private readonly List<(string Url, Guid? PageTypeId, IReadOnlyList<string> Links)> _pages = [];

        public void Observe(PageSnapshot page)
        {
            if (page.RedirectChain.Count > 0) _redirecting.Add(page.NormalizedUrl);
            if (PageChecks.IsHtml(page) && page.Data.InternalLinks.Count > 0)
                _pages.Add((page.Url, page.PageTypeId, page.Data.InternalLinks));
        }

        public IEnumerable<RuleViolation> Complete()
        {
            foreach (var (url, pageTypeId, links) in _pages)
            {
                var bad = links.Where(l => Uri.TryCreate(l, UriKind.Absolute, out var u) && _redirecting.Contains(UrlKey.Normalize(u)))
                    .Distinct(StringComparer.Ordinal).ToList();
                if (bad.Count > 0)
                    yield return new RuleViolation(ruleCode, pageTypeId, url, bad[0], $"{bad.Count} bağlantı: {string.Join(", ", bad.Take(5))}");
            }
        }
    }
}
