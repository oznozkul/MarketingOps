using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class MetaDescriptionMissingRule : RuleBase, IPageRule
{
    public override string Code => "seo.meta_description.missing";
    public override string Name => "Meta description eksik";
    public override string Description => "İndekslenebilir sayfalarda meta description etiketinin varlığını kontrol eder.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:snippets";
    public override string Impact => "Meta description sıralamayı doğrudan etkilemez ama arama sonucundaki açıklamayı ve tıklama oranını etkiler.";
    protected override string TitleTemplate => "{n} sayfada meta description eksik";
    public override Team Team => Team.Content;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Text, "Her sayfanın içeriğini özetleyen, tıklamaya davet eden bir meta description olmalı.", "{Sayfada ne bulunduğu}, {öne çıkan fayda}. {Harekete çağrı}.\n120–155 karakter hedefleyin; ürün ve kategori şablonlarında dinamik alanlarla üretilebilir.");
    public override string TeamCriterion => "Meta description alanı sayfa tipinin şablonunda tanımlı.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (PageChecks.IsIndexable(page) && string.IsNullOrWhiteSpace(page.Data.MetaDescription))
            yield return Violation(page, "—");
    }
}
