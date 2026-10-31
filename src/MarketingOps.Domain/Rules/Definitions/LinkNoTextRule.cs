using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class LinkNoTextRule : RuleBase, IPageRule
{
    public override string Code => "a11y.link.no_text";
    public override string Name => "Erişilebilir adı olmayan link";
    public override string Description => "Metni, aria-label'ı, alt metinli görseli ya da title'ı olmayan linkleri bulur (ikon linkler gibi).";
    public override FindingCategory Category => FindingCategory.Accessibility;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "a11y:links";
    public override string Impact => "Adı olmayan link ekran okuyucuda yalnızca \"bağlantı\" diye okunur; arama motoru da linkin neye gittiğini anlamaz.";
    protected override string TitleTemplate => "{n} sayfada adı olmayan link var";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "İkon ya da görsel linkler gittikleri yeri anlatan bir ad taşımalı.", "<a href=\"/sepet\" aria-label=\"Sepete git\">\n  <svg aria-hidden=\"true\">…</svg>\n</a>\n<a href=\"/urun\"><img src=\"/urun.jpg\" alt=\"Michelin Primacy 4 205/55 R16\" /></a>");
    public override string TeamCriterion => "İkon ve görsel linkler şablonlarda aria-label ya da alt metin taşıyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page) || !PageChecks.HasMarkupFacts(page) || page.Data.Links.NoText.Count == 0) yield break;
        // Render, sayfa tipinin örneklerinde görünür adsız link bulamazsa HTML'deki sorun gizli kopyadan geliyordur.
        if (PageChecks.RenderSuppressesHtmlFinding(context, page.PageTypeId, m => m.VisibleNoTextLinks)) yield break;
        yield return Violation(page, page.Data.Links.NoText);
    }
}
