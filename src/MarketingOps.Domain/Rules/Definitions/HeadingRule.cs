using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class HeadingRule : RuleBase, IPageRule
{
    public override string Code => "seo.h1.missing_or_multiple";
    public override string Name => "H1 eksik veya birden fazla";
    public override string Description => "Sayfada tek bir H1 başlığı olup olmadığını kontrol eder.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:headings";
    public override string Impact => "H1, sayfanın konusunu arama motoruna ve ekran okuyuculara bildirir; birden fazla H1 sayfanın ana konusunu belirsizleştirir.";
    protected override string TitleTemplate => "{n} sayfada H1 eksik veya birden fazla";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Her sayfada sayfanın ana konusunu anlatan tek bir H1 olmalı.", "1. Sayfa şablonunda ana başlığı tek bir <h1> olarak işaretleyin.\n2. Logo, menü ve yan bileşenlerdeki <h1> etiketlerini <h2> ya da <div> yapın.\n3. H1 metninin sayfanın ana konusunu içerdiğini kontrol edin.");
    public override string TeamCriterion => "Şablonda sayfa başına tek H1 kalacak şekilde düzenleme yapıldı.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page)) yield break;
        var count = page.Data.H1.Count;
        if (count == 0) yield return Violation(page, "H1 yok");
        else if (count > 1) yield return Violation(page, $"{count} H1", string.Join(" | ", page.Data.H1));
    }
}
