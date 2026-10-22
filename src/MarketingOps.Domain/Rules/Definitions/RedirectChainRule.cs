using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class RedirectChainRule : RuleBase, IPageRule
{
    public static readonly RuleThreshold MinHops = new("minHops", "En az adım", "yönlendirme", 3m, 2m, 10m);

    public override IReadOnlyList<RuleThreshold> Thresholds => [MinHops];

    public override string Code => "seo.redirect.chain";
    public override string Name => "Uzun yönlendirme zinciri";
    public override string Description => "Hedefe ulaşmadan önce birden fazla yönlendirmeden geçen URL'leri bulur.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:redirects";
    public override string Impact => "Her ek yönlendirme sayfanın açılmasını geciktirir ve arama motorunun hedefe ulaşmasını zorlaştırır.";
    protected override string TitleTemplate => "{n} URL'de uzun yönlendirme zinciri";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Her eski adres hedefe tek adımda yönlenmeli.", "1. Zincirdeki ilk adresi doğrudan son hedefe 301 ile yönlendirin.\n2. Site içi bağlantıları son hedef adrese güncelleyin.\n3. Ara adımlardaki eski yönlendirme kurallarını kaldırın.");
    public override string TeamCriterion => "Yönlendirme kuralları tek adıma indirildi.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        var hops = page.RedirectChain.Count;
        if (hops >= context.Threshold(MinHops))
            yield return Violation(page, $"{hops} adım", string.Join(" → ", page.RedirectChain.Select(h => $"{h.Url} ({h.StatusCode})")));
    }
}
