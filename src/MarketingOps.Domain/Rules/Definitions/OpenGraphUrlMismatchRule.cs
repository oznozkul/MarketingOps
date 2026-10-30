using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class OpenGraphUrlMismatchRule : RuleBase, IPageRule
{
    public override string Code => "social.og.url_mismatch";
    public override string Name => "Paylaşım adresi canonical'la uyuşmuyor";
    public override string Description => "og:url ile canonical'ın aynı adresi gösterip göstermediğini kontrol eder (büyük/küçük harf, # sonrası ve sondaki / yok sayılır).";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "social:open-graph";
    public override string Impact => "og:url başka bir adresi gösterirse paylaşımlar ve etkileşimler yanlış sayfada toplanır; sayfa taşındıysa paylaşım eski adrese gider.";
    protected override string TitleTemplate => "{n} sayfada paylaşım adresi canonical'la uyuşmuyor";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "og:url, canonical'ı üreten aynı değerden beslenmeli.", "<link rel=\"canonical\" href=\"{Canonical adres}\" />\n<meta property=\"og:url\" content=\"{Canonical adres}\" />");
    public override string TeamCriterion => "og:url ve canonical şablonda tek kaynaktan üretiliyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page) || !PageChecks.HasMarkupFacts(page)) yield break;
        var ogUrl = page.Data.OpenGraph?.Url;
        var canonical = page.Data.Canonical;
        if (string.IsNullOrWhiteSpace(ogUrl) || string.IsNullOrWhiteSpace(canonical)) yield break;
        if (Normalize(ogUrl) != Normalize(canonical)) yield return Violation(page, ogUrl, $"Canonical: {canonical}");
    }

    private static string Normalize(string url) => url.Trim().Split('#')[0].TrimEnd('/').ToLowerInvariant();
}
