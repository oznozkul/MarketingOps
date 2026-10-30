using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class IframeTitleRule : RuleBase, IPageRule
{
    public override string Code => "a11y.iframe.no_title";
    public override string Name => "Başlığı olmayan iframe";
    public override string Description => "title özniteliği olmayan iframe'leri bulur (video, harita, ödeme formu gibi gömülü içerik).";
    public override FindingCategory Category => FindingCategory.Accessibility;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "a11y:iframes";
    public override string Impact => "Başlıksız iframe ekran okuyucuda adsız bir çerçeve olarak duyurulur; kullanıcı içine girmeden ne olduğunu bilemez.";
    protected override string TitleTemplate => "{n} sayfada başlığı olmayan iframe var";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Her iframe içeriğini anlatan bir title taşımalı.", "<iframe src=\"https://www.youtube.com/embed/…\" title=\"Michelin Primacy 4 tanıtım videosu\" loading=\"lazy\"></iframe>");
    public override string TeamCriterion => "Gömülü içerik bileşenleri iframe'e title veriyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (PageChecks.IsHtml(page) && PageChecks.HasMarkupFacts(page) && page.Data.Iframes.WithoutTitle.Count > 0)
            yield return Violation(page, page.Data.Iframes.WithoutTitle);
    }
}
