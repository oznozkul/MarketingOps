using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class BrokenInternalLinksRule : RuleBase, IPageRule
{
    public override string Code => "seo.links.broken_internal";
    public override string Name => "Kırık iç bağlantı";
    public override string Description => "Sayfadaki iç bağlantılardan 4xx/5xx dönen ya da yanıt vermeyenleri bulur.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:links";
    public override string Impact => "Kırık bağlantılar kullanıcıyı hata sayfasına götürür ve tarama bütçesini boşa harcar.";
    protected override string TitleTemplate => "{n} sayfada kırık iç bağlantı var";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Site içi bağlantılar çalışan sayfalara gitmeli.", "1. Etkilenen sayfalardaki kırık bağlantıları (aşağıdaki tabloda) bulun.\n2. Bağlantıyı çalışan hedefe güncelleyin ya da kaldırın.\n3. Kaldırılan sayfalar için kalıcı (301) yönlendirme tanımlayın.");
    public override string TeamCriterion => "Kırık bağlantıların kaynağı (şablon ya da içerik) düzeltildi.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page)) yield break;
        var broken = page.Data.InternalLinks
            .Where(l => Uri.TryCreate(l, UriKind.Absolute, out var u) && context.FailedUrls.Contains(UrlKey.Normalize(u)))
            .Distinct()
            .ToList();
        if (broken.Count > 0) yield return Violation(page, broken[0], $"{broken.Count} kırık bağlantı: {string.Join(", ", broken.Take(5))}");
    }
}
