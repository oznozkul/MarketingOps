using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class LazyLoadMissingRule : RuleBase, IPageRule
{
    public static readonly RuleThreshold AboveFoldImages = new("aboveFoldImages", "İlk görünümdeki görsel sayısı", "görsel", 3m, 0m, 20m);

    public override IReadOnlyList<RuleThreshold> Thresholds => [AboveFoldImages];

    public override string Code => "perf.image.lazy_missing";
    public override string Name => "Ekran altı görsellerde lazy load eksik";
    public override string Description => "İlk görünümdeki görsellerden sonra gelen ve loading='lazy' olmayan görselleri bulur.";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.Low;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "perf-doc:p18";
    public override string Impact => "Ekranda görünmeyen görsellerin hemen indirilmesi ilk yüklemede bant genişliğini ana içerikle paylaştırır.";
    protected override string TitleTemplate => "{n} sayfada ekran altı görsellerde lazy load eksik";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "İlk görünümün altındaki görseller tembel yüklenmeli; ilk görünümdekiler hemen yüklenmeli.", "<img src=\"/gorsel.jpg\" alt=\"Açıklama\" width=\"800\" height=\"600\" loading=\"lazy\" />");
    public override string TeamCriterion => "Liste ve ürün şablonlarında ekran altı görseller loading=\"lazy\" ile yükleniyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page)) yield break;
        var aboveFold = (int)context.Threshold(AboveFoldImages);
        var eager = page.Data.Images.Skip(aboveFold).Where(i => !string.Equals(i.Loading, "lazy", StringComparison.OrdinalIgnoreCase)).ToList();
        if (eager.Count > 0) yield return Violation(page, $"{eager.Count} görsel", string.Join(", ", eager.Take(5).Select(i => i.Src)));
    }
}
