using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class NavLabelRule : RuleBase, IPageRule
{
    public override string Code => "a11y.nav.unlabeled";
    public override string Name => "Birden fazla menü etiketsiz";
    public override string Description => "Sayfada birden fazla <nav> varken aria-label ya da aria-labelledby'sı olmayan menüleri bulur.";
    public override FindingCategory Category => FindingCategory.Accessibility;
    public override Severity DefaultSeverity => Severity.Low;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "a11y:landmarks";
    public override string Impact => "Etiketsiz menüler ekran okuyucuda hepsi \"gezinme\" diye listelenir; kullanıcı ana menüyü alt menüden ayıramaz.";
    protected override string TitleTemplate => "{n} sayfada menüler etiketsiz";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Her menü ne olduğunu anlatan bir aria-label taşımalı.", "<nav aria-label=\"Ana menü\">…</nav>\n<nav aria-label=\"Kategori menüsü\">…</nav>\n<nav aria-label=\"Alt bilgi\">…</nav>");
    public override string TeamCriterion => "Şablondaki bütün nav öğeleri aria-label taşıyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (PageChecks.IsHtml(page) && PageChecks.HasMarkupFacts(page) && page.Data.Landmarks.UnlabeledNavs.Count > 0)
            yield return Violation(page, page.Data.Landmarks.UnlabeledNavs);
    }
}
