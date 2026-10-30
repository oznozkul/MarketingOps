using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class BlankNoopenerRule : RuleBase, IPageRule
{
    public override string Code => "sec.link.blank_noopener";
    public override string Name => "Yeni sekme linkinde noopener yok";
    public override string Description => "target=\"_blank\" olup rel'inde noopener ya da noreferrer olmayan linkleri bulur.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.Low;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "security:links";
    public override string Impact => "noopener'sız yeni sekme, açılan sayfanın açan sekmeyi yönlendirebilmesine izin verir; eski tarayıcılarda kimlik avına kapı açar.";
    protected override string TitleTemplate => "{n} sayfada yeni sekme linkinde noopener yok";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Yeni sekmede açılan dış linkler rel=\"noopener noreferrer\" taşımalı.", "<a href=\"https://ornek.com\" target=\"_blank\" rel=\"noopener noreferrer\">Örnek</a>");
    public override string TeamCriterion => "target=\"_blank\" üreten bileşenler rel=\"noopener noreferrer\" ekliyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (PageChecks.IsHtml(page) && PageChecks.HasMarkupFacts(page) && page.Data.Links.BlankWithoutNoopener.Count > 0)
            yield return Violation(page, page.Data.Links.BlankWithoutNoopener);
    }
}
