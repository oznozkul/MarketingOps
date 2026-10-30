using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class EmptyHrefRule : RuleBase, IPageRule
{
    public override string Code => "seo.link.empty_href";
    public override string Name => "Hedefi olmayan link";
    public override string Description => "href'i olmayan, boş, \"#\" ya da javascript: olan linkleri bulur. role=\"button\" olanlar ve breadcrumb'ın son adımı sayılmaz.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:links";
    public override string Impact => "Hedefi olmayan link taranamaz; bir işlemi tetikleyen link de klavye ve ekran okuyucu kullanıcısına buton gibi davranmaz.";
    protected override string TitleTemplate => "{n} sayfada hedefi olmayan link var";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Bir yere giden öğe gerçek adres taşıyan link, işlem yapan öğe buton olmalı.", "<a href=\"/kategori/kis-lastikleri\">Kış lastikleri</a>\n<button type=\"button\" aria-expanded=\"false\">Filtrele</button>");
    public override string TeamCriterion => "Hedefsiz linkler gerçek adrese ya da <button> öğesine çevrildi.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (PageChecks.IsHtml(page) && PageChecks.HasMarkupFacts(page) && page.Data.Links.EmptyHref.Count > 0)
            yield return Violation(page, page.Data.Links.EmptyHref);
    }
}
