using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>
/// Başka bir adresi canonical gösteren indekslenebilir sayfada, hedef bu taramada taranmışsa hedefin durumu denetlenir:
/// yanıt yok / hata, yönleniyor, noindex ya da hedefin canonical'ı başka bir adres (zincir). Taranmamış hedef değerlendirilmez.
/// </summary>
public sealed class CanonicalTargetRule : RuleBase, ISiteRule
{
    public override string Code => "seo.canonical.target";
    public override string Name => "Canonical hedefi geçersiz";
    public override string Description => "Canonical'ın gösterdiği adres taramadaysa hedefin durumunu denetler: yanıt vermiyor, hata dönüyor, yönleniyor, noindex ya da başka adrese canonical gösteriyor.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Site;
    public override string ReferenceKey => "google:canonical";
    public override string Impact => "Canonical yönlenen, noindex ya da başka adrese işaret eden bir hedefe gösterirse arama motoru asıl sayfayı seçemez; içerik dizinden düşebilir.";
    protected override string TitleTemplate => "{n} sayfada canonical hedefi geçersiz";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Canonical sayfanın kendisini ya da 200 dönen, noindex olmayan ve kendini gösteren kalıcı bir adresi göstermeli.", "1. Canonical'ı sayfanın kendi kalıcı adresine ya da doğrudan çalışan bir hedefe yöneltin.\n2. Hedefin noindex ya da yönlendirme dönmediğini doğrulayın.\n3. Zincir oluşturan canonical'ları kaldırın.");
    public override string TeamCriterion => "Canonical hedefleri 200 dönen, noindex olmayan ve kendini gösteren adreslere işaret ediyor.";

    public ISiteRuleRun Start(RuleContext context) => new Run(Code);

    private sealed class Run(string ruleCode) : ISiteRuleRun
    {
        private readonly Dictionary<string, Observed> _byUrl = new(StringComparer.Ordinal);
        private readonly List<Observed> _sources = [];

        public void Observe(PageSnapshot page)
        {
            var canonicalKey = page.Data.Canonical is { } canonical && Uri.TryCreate(canonical, UriKind.Absolute, out var url)
                ? UrlKey.Normalize(url) : null;
            var observed = new Observed(page.Url, page.NormalizedUrl, page.PageTypeId, page.StatusCode, page.RedirectChain.Count, PageChecks.IsNoindex(page), canonicalKey);
            _byUrl[observed.Key] = observed;
            if (PageChecks.IsIndexable(page) && canonicalKey is not null && canonicalKey != observed.Key)
                _sources.Add(observed);
        }

        public IEnumerable<RuleViolation> Complete()
        {
            foreach (var source in _sources)
            {
                if (!_byUrl.TryGetValue(source.CanonicalKey!, out var target)) continue; // Taranmamış hedef değerlendirilmez.
                var (current, detail) = Describe(source, target);
                if (current is null) continue;
                yield return new RuleViolation(ruleCode, source.PageTypeId, source.Url, current, detail);
            }
        }

        private static (string? Current, string? Detail) Describe(Observed source, Observed target)
        {
            if (target.Redirects > 0)
                return ("Hedef yönleniyor", $"canonical → {source.CanonicalKey} ({target.Redirects} yönlendirme)");
            if (target.StatusCode is null)
                return ("Hedef yanıt vermiyor", $"canonical → {source.CanonicalKey}");
            if (target.StatusCode >= 400)
                return ($"Hedef {target.StatusCode} dönüyor", $"canonical → {source.CanonicalKey}");
            if (target.Noindex)
                return ("Hedef noindex", $"canonical → {source.CanonicalKey}");
            if (target.CanonicalKey is { } targetCanonical && targetCanonical != target.Key)
                return ("Hedefin canonical'ı başka adres", $"canonical → {source.CanonicalKey} → {targetCanonical}");
            return (null, null);
        }
    }

    private sealed record Observed(string Url, string Key, Guid? PageTypeId, int? StatusCode, int Redirects, bool Noindex, string? CanonicalKey);
}
