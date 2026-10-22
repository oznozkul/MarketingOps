using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>"Fırsat": arama sonuçlarında ilk sayfaya yakın (11–20. sıra) ve gösterimi yüksek sayfalar.</summary>
public sealed class OpportunityRule : RuleBase, IPageRule
{
    public static readonly RuleThreshold MinImpressions = new("minImpressions", "En az gösterim (28 gün)", "gösterim", 100m, 10m, 100000m);
    public static readonly RuleThreshold BestPosition = new("bestPosition", "En iyi ortalama sıra", "sıra", 11m, 2m, 50m);
    public static readonly RuleThreshold WorstPosition = new("worstPosition", "En kötü ortalama sıra", "sıra", 20m, 5m, 100m);

    public override IReadOnlyList<RuleThreshold> Thresholds => [MinImpressions, BestPosition, WorstPosition];
    public override RuleDataSource DataSource => RuleDataSource.SearchConsole;

    public override string Code => "seo.opportunity.striking_distance";
    public override string Name => "İlk sayfaya yakın sayfalar";
    public override string Description => "Ortalama sırası ilk sayfanın hemen arkasında olan ve gösterimi yüksek sayfaları bulur (Search Console, son 28 gün).";
    public override FindingCategory Category => FindingCategory.Content;
    public override Severity DefaultSeverity => Severity.Low;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:helpful-content";
    public override string Impact => "Bu sayfalar aranıyor ve görülüyor ama ilk sayfada değil; içerik ve title iyileştirmesiyle en az eforla trafik kazanılabilecek sayfalardır.";
    protected override string TitleTemplate => "{n} sayfa arama sonuçlarında ilk sayfaya yakın";
    public override Team Team => Team.Content;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Sayfanın aranan sorguya verdiği yanıtı güçlendirin.",
        "1. Search Console'da sayfanın gösterim aldığı sorguları inceleyin.\n2. Title ve H1'i en çok gösterim alan sorguyla uyumlu hale getirin.\n3. Sorguyu yanıtlayan özgün içerik ve site içi bağlantılar ekleyin.");
    public override string TeamCriterion => "Sayfaların title, H1 ve içeriği hedef sorgulara göre güncellendi.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page) || !context.External.SearchByUrl.TryGetValue(page.NormalizedUrl, out var metrics)) yield break;
        if (metrics.Impressions >= context.Threshold(MinImpressions)
            && (decimal)metrics.AveragePosition >= context.Threshold(BestPosition)
            && (decimal)metrics.AveragePosition <= context.Threshold(WorstPosition))
            yield return Violation(page, $"{metrics.AveragePosition.ToString("0.#", Tr)}. sıra", $"{metrics.Impressions.ToString("N0", Tr)} gösterim, {metrics.Clicks.ToString("N0", Tr)} tıklama");
    }
}
