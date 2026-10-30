using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class TitleLengthRule : RuleBase, IPageRule
{
    public static readonly RuleThreshold MinLength = new("minLength", "En kısa title", "karakter", 30m, 10m, 100m);
    public static readonly RuleThreshold MaxLength = new("maxLength", "En uzun title", "karakter", 60m, 30m, 150m);

    public override IReadOnlyList<RuleThreshold> Thresholds => [MinLength, MaxLength];

    public override string Code => "seo.title.length";
    public override string Name => "Title uzunluğu";
    public override string Description => "Title uzunluğunun önerilen aralıkta olup olmadığını kontrol eder.";
    public override FindingCategory Category => FindingCategory.Content;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:title-links";
    public override string Impact => "Çok uzun title'lar arama sonuçlarında kesilir; çok kısa title'lar sayfayı yeterince tanımlamaz.";
    protected override string TitleTemplate => "{n} sayfada title uzunluğu önerilen aralık dışında";
    public override Team Team => Team.Seo;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Text, "Title önerilen uzunlukta olmalı; önemli kelimeler başta, marka sonda.", "{Ana anahtar kelime} {Ayırt edici bilgi} | {Marka}\nUzun title'larda tekrar eden ekleri kısaltın; kısa olanlara sayfayı tanımlayan bilgiyi ekleyin.");
    public override string TeamCriterion => "Title şablonu önerilen uzunluğa göre düzenlendi.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page) || page.Data.Title is not { } title) yield break;
        var length = title.Trim().Length;
        if (length < context.Threshold(MinLength) || length > context.Threshold(MaxLength))
            yield return Violation(page, $"{length} karakter", title);
    }
}
