using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class IframeLazyRule : RuleBase, IPageRule
{
    public override string Code => "perf.iframe.lazy_missing";
    public override string Name => "Iframe'lerde lazy load eksik";
    public override string Description => "loading=\"lazy\" olmayan iframe'leri bulur.";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "perf-doc:p18";
    public override string Impact => "Video ve harita gibi gömülü içerik kendi sayfasını, betiklerini ve görsellerini hemen indirir; ilk yüklemede ana içerikle bant genişliği yarışır.";
    protected override string TitleTemplate => "{n} sayfada iframe'lerde lazy load eksik";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "İlk ekranda olmayan iframe'ler tembel yüklenmeli.", "<iframe src=\"https://www.youtube.com/embed/…\" title=\"Tanıtım videosu\" loading=\"lazy\"></iframe>");
    public override string TeamCriterion => "Gömülü içerik bileşenleri ilk ekran dışında loading=\"lazy\" kullanıyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (PageChecks.IsHtml(page) && PageChecks.HasMarkupFacts(page) && page.Data.Iframes.NotLazy.Count > 0)
            yield return Violation(page, page.Data.Iframes.NotLazy);
    }
}
