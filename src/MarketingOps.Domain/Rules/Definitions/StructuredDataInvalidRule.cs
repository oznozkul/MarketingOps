using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class StructuredDataInvalidRule : RuleBase, IPageRule
{
    public override string Code => "seo.schema.invalid";
    public override string Name => "Structured data bozuk";
    public override string Description => "JSON olarak okunamayan JSON-LD bloklarını bulur.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:structured-data";
    public override string Impact => "Okunamayan JSON-LD bloğu arama motorları tarafından tamamen yok sayılır; içindeki bilgilerin hiçbiri kullanılmaz.";
    protected override string TitleTemplate => "{n} sayfada structured data bozuk";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "JSON-LD şablonu geçerli JSON üretmeli.", "1. Bloğu üreten şablonu bulun (örnekteki ilk karakterler bloğun başıdır).\n2. Değerleri elle birleştirmek yerine JSON serileştirici kullanın; tırnak ve satır sonları kaçırılmalı.\n3. Sondaki virgülleri ve kapanmamış parantezleri kontrol edin.\n4. Çıktıyı Google Zengin Sonuçlar Testi'nden geçirin.");
    public override string TeamCriterion => "JSON-LD bir JSON serileştiriciyle üretiliyor ve test aracından hatasız geçiyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page) || !PageChecks.HasMarkupFacts(page)) yield break;
        var invalid = page.Data.StructuredData.Invalid;
        if (invalid.Count > 0) yield return Violation(page, $"{invalid.Count} bozuk blok", string.Join(" · ", invalid.Samples));
    }
}
