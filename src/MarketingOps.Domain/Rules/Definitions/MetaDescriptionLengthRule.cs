using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class MetaDescriptionLengthRule : RuleBase, IPageRule
{
    public static readonly RuleThreshold MinLength = new("minLength", "En kısa meta description", "karakter", 70m, 20m, 200m);
    public static readonly RuleThreshold MaxLength = new("maxLength", "En uzun meta description", "karakter", 160m, 60m, 400m);

    public override IReadOnlyList<RuleThreshold> Thresholds => [MinLength, MaxLength];
    public override string Code => "seo.meta_description.length";
    public override string Name => "Meta description uzunluğu";
    public override string Description => "Meta description'ın eşik aralığında olup olmadığını kontrol eder.";
    public override FindingCategory Category => FindingCategory.Content;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:snippets";
    public override string Impact => "Çok kısa açıklama sayfanın vaadini anlatmaz ve arama motoru kendi metnini seçer; çok uzun açıklama sonuçta kesilir.";
    protected override string TitleTemplate => "{n} sayfada meta description uzunluğu aralık dışında";
    public override Team Team => Team.Content;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Text, "Açıklama sayfanın vaadini aralık içinde, sayfaya özgü bilgiyle anlatmalı.", "{Ürün/kategori adı}: {ayırt edici özellik}, {fiyat, kargo ya da stok gibi fayda}. {Kısa çağrı}.");
    public override string TeamCriterion => "Açıklama şablonu aralık içinde, sayfaya özgü metin üretiyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page) || string.IsNullOrWhiteSpace(page.Data.MetaDescription)) yield break;
        var length = page.Data.MetaDescription.Trim().Length;
        if (length < context.Threshold(MinLength) || length > context.Threshold(MaxLength))
            yield return Violation(page, $"{length} karakter", page.Data.MetaDescription);
    }
}
