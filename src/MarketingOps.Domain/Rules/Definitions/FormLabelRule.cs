using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class FormLabelRule : RuleBase, IPageRule
{
    public override string Code => "a11y.form.no_label";
    public override string Name => "Etiketi olmayan form alanı";
    public override string Description => "<label>, aria-label, aria-labelledby ya da title'ı olmayan form alanlarını bulur. Placeholder etiket sayılmaz.";
    public override FindingCategory Category => FindingCategory.Accessibility;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "a11y:forms";
    public override string Impact => "Etiketsiz alanın ne istediği ekran okuyucuda duyurulmaz; placeholder yazmaya başlayınca kaybolur. Arama, filtre, üyelik ve ödeme formları kullanılamaz hale gelir.";
    protected override string TitleTemplate => "{n} sayfada etiketi olmayan form alanı var";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Her alan görünür bir <label> ile, görünür etiket tasarıma uymuyorsa aria-label ile adlandırılmalı.", "<label for=\"email\">E-posta</label>\n<input id=\"email\" name=\"email\" type=\"email\" autocomplete=\"email\" />\n\n<input type=\"search\" name=\"q\" aria-label=\"Sitede ara\" placeholder=\"Ebat, marka ya da model\" />");
    public override string TeamCriterion => "Form bileşenleri her alana label ya da aria-label veriyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (PageChecks.IsHtml(page) && PageChecks.HasMarkupFacts(page) && page.Data.Forms.Unlabeled.Count > 0)
            yield return Violation(page, page.Data.Forms.Unlabeled);
    }
}
