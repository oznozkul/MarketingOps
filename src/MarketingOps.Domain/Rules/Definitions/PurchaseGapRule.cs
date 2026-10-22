using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Metrics;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class PurchaseGapRule : RuleBase
{
    public static readonly RuleThreshold MaxGap = new("maxGapPercent", "Kabul edilen fark", "%", 5m, 0m, 100m);

    public override string Code => "track.purchase.gap";
    public override string Name => "Purchase ve sipariş farkı";
    public override string Description => "GA4 purchase sayısı ile sipariş veritabanındaki günlük sipariş sayısı arasındaki fark eşiği aşıyor mu kontrol eder.";
    public override FindingCategory Category => FindingCategory.Tracking;
    public override Severity DefaultSeverity => Severity.Critical;
    public override RuleScope Scope => RuleScope.Site;
    public override RuleDataSource DataSource => RuleDataSource.Tracking;
    public override string ReferenceKey => "tracking:orders";
    public override RuleThreshold[] Thresholds => [MaxGap];
    protected override string TitleTemplate => "{n} günde purchase sayısı siparişten sapıyor";
    public override string Impact => "Purchase ile sipariş arasındaki fark, gelir raporunun sipariş veritabanından saptığını gösterir.";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Purchase event'inin teşekkür sayfasında sipariş başına bir kez gittiğini doğrulayın.", "1. Teşekkür sayfasındaki purchase event'inin transaction_id ile gittiğini kontrol edin.\n2. Sipariş oluşup teşekkür sayfası açılmayan akışları (havale, ödeme yönlendirmesi) ayrıca ölçün.\n3. Çift purchase gönderen tetikleyiciyi kaldırın.");
    public override string TeamCriterion => "Purchase sayısı sipariş sayısıyla kabul edilen farkın içinde.";

    public IEnumerable<RuleViolation> Evaluate(string siteUrl, IReadOnlyList<OrderComparisonDaily> rows, decimal maxGapPercent) =>
        rows.Where(r => r.GapPercent > maxGapPercent).Select(r => new RuleViolation(
            Code, null, siteUrl,
            $"%{r.GapPercent!.Value.ToString("N1", Tr)}",
            $"{r.Date.ToString("d MMM", Tr)}: sipariş {r.OrderCount}, purchase {r.Ga4PurchaseCount}"));
}
