using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class TableHeaderRule : RuleBase, IPageRule
{
    public override string Code => "a11y.table.no_th";
    public override string Name => "Başlık hücresi olmayan tablo";
    public override string Description => "Hiç <th> hücresi olmayan veri tablolarını bulur. role=\"presentation\" ya da role=\"none\" olan düzen tabloları sayılmaz.";
    public override FindingCategory Category => FindingCategory.Accessibility;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "a11y:tables";
    public override string Impact => "Başlık hücresi olmayan tabloda ekran okuyucu hücrenin hangi sütuna ait olduğunu söyleyemez; ebat ve teknik özellik tabloları anlaşılmaz olur.";
    protected override string TitleTemplate => "{n} sayfada başlık hücresi olmayan tablo var";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Veri tablolarının ilk satırı ya da sütunu <th scope> ile işaretlenmeli.", "<table>\n  <tr><th scope=\"row\">Ebat</th><td>205/55 R16</td></tr>\n  <tr><th scope=\"row\">Yük endeksi</th><td>91</td></tr>\n</table>");
    public override string TeamCriterion => "Teknik özellik ve fiyat tabloları <th scope> kullanıyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (PageChecks.IsHtml(page) && PageChecks.HasMarkupFacts(page) && page.Data.Tables.WithoutHeader.Count > 0)
            yield return Violation(page, page.Data.Tables.WithoutHeader);
    }
}
