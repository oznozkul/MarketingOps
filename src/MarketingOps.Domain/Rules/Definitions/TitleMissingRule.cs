using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class TitleMissingRule : RuleBase, IPageRule
{
    public override string Code => "seo.title.missing";
    public override string Name => "Title eksik";
    public override string Description => "İndekslenebilir sayfalarda title etiketinin varlığını kontrol eder.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:title-links";
    public override string Impact => "Title, arama sonuçlarındaki başlık bağlantısının ana kaynağıdır; yoksa arama motoru sayfa içeriğinden kendi başlığını üretir.";
    protected override string TitleTemplate => "{n} sayfada title eksik";
    public override Team Team => Team.Seo;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Text, "Title, sayfanın ana konusunu ve markayı içermeli; şablonda her sayfa için doldurulmalı.", "{Sayfanın ana konusu} | {Marka}\nŞablondaki title alanı boş kalıyorsa CMS'teki alanın şablona bağlandığını kontrol edin.");
    public override string TeamCriterion => "Title alanı sayfa tipinin şablonunda tanımlı ve boş bırakılamıyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (PageChecks.IsIndexable(page) && string.IsNullOrWhiteSpace(page.Data.Title))
            yield return Violation(page, "—");
    }
}
