using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>
/// hreflang'ın işaret ettiği adres taramadaysa, hedefin kaynağa geri bağlantı vermesi ve çalışan bir sayfa olması beklenir:
/// geri bağlantı yok ya da hedef hata/yönlenme/noindex ise bulgu açılır. Taranmamış hedef değerlendirilmez.
/// </summary>
public sealed class HreflangReturnMissingRule : RuleBase, ISiteRule
{
    public override string Code => "seo.hreflang.return_missing";
    public override string Name => "hreflang geri bağlantısı eksik";
    public override string Description => "hreflang hedefinin kaynağa geri bağlantı vermesini ve hedefin çalışan bir sayfa olmasını denetler.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Site;
    public override string ReferenceKey => "google:hreflang";
    public override string Impact => "Geri bağlantı vermeyen dil sürümü hreflang'ı geçersiz kılar; arama motoru bölgesel sürümleri güvenilir bulmaz.";
    protected override string TitleTemplate => "{n} sayfada hreflang geri bağlantısı eksik";
    public override Team Team => Team.Seo;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Her hreflang hedefi kaynağa karşılıklı bağlantı vermeli ve çalışan bir sayfa olmalı.", "1. Her dil sürümünün hreflang kümesine diğer sürümleri ekleyin.\n2. Hedef adreslerin 200 döndüğünü, yönlenmediğini ve noindex olmadığını doğrulayın.\n3. Çift yönlü (karşılıklı) hreflang'i şablonda üretin.");
    public override string TeamCriterion => "hreflang kümeleri karşılıklı ve hedefler çalışır durumda.";

    public ISiteRuleRun Start(RuleContext context) => new Run(Code);

    private sealed class Run(string ruleCode) : ISiteRuleRun
    {
        private readonly Dictionary<string, PageInfo> _byUrl = new(StringComparer.Ordinal);
        private readonly List<PageInfo> _sources = [];

        public void Observe(PageSnapshot page)
        {
            var targets = page.Data.Hreflangs
                .Select(l => Normalize(l.Url))
                .Where(u => u is not null)
                .Select(u => u!)
                .ToHashSet(StringComparer.Ordinal);
            var info = new PageInfo(page.Url, page.NormalizedUrl, page.PageTypeId, targets, page.StatusCode, page.RedirectChain.Count, PageChecks.IsNoindex(page));
            _byUrl[info.Key] = info;
            if (PageChecks.IsIndexable(page) && PageChecks.HasSignals(page) && targets.Count > 0)
                _sources.Add(info);
        }

        public IEnumerable<RuleViolation> Complete()
        {
            foreach (var source in _sources)
            {
                var problems = new List<string>();
                foreach (var targetKey in source.Targets)
                {
                    if (!_byUrl.TryGetValue(targetKey, out var target)) continue; // Taranmamış hedef değerlendirilmez.
                    if (target.Redirects > 0) problems.Add($"{targetKey} yönleniyor");
                    else if (target.StatusCode is null) problems.Add($"{targetKey} yanıt vermiyor");
                    else if (target.StatusCode >= 400) problems.Add($"{targetKey} {target.StatusCode}");
                    else if (target.Noindex) problems.Add($"{targetKey} noindex");
                    else if (!target.Targets.Contains(source.Key)) problems.Add($"{targetKey} geri bağlantı vermiyor");
                }
                if (problems.Count > 0)
                    yield return new RuleViolation(ruleCode, source.PageTypeId, source.Url, problems[0], string.Join(" · ", problems.Take(5)));
            }
        }

        private static string? Normalize(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) ? UrlKey.Normalize(uri) : null;
    }

    private sealed record PageInfo(string Url, string Key, Guid? PageTypeId, HashSet<string> Targets, int? StatusCode, int Redirects, bool Noindex);
}
