using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class EventAnomalyRule : RuleBase
{
    public static readonly RuleThreshold MaxDrop = new("maxDropPercent", "Kabul edilen düşüş", "%", 50m, 0m, 100m);

    public override string Code => "track.event.anomaly";
    public override string Name => "Event sayısı düştü";
    public override string Description => "Dünün event sayısı, önceki 7 günün ortalamasına göre eşiğin üstünde düştü mü ya da sıfıra indi mi kontrol eder.";
    public override FindingCategory Category => FindingCategory.Tracking;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Site;
    public override RuleDataSource DataSource => RuleDataSource.Tracking;
    public override string ReferenceKey => "tracking:orders";
    public override RuleThreshold[] Thresholds => [MaxDrop];
    protected override string TitleTemplate => "{n} event'in sayısı düştü";
    public override string Impact => "Event sayısındaki ani düşüş, ölçümün bir sayfada durduğunu gösterir.";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Düşen event'in tetikleyicisinin hâlâ çalıştığını doğrulayın.", "1. Düşen event'in hangi sayfada gönderildiğini GA4'te kontrol edin.\n2. Tetikleyicinin sayfa değişikliğinden sonra hâlâ eşleştiğini doğrulayın.\n3. Onay veya tag değişikliğinin bu event'i kesmediğini kontrol edin.");
    public override string TeamCriterion => "Event sayısı 7 günlük ortalamasına yakın.";

    public IEnumerable<RuleViolation> Evaluate(string siteUrl, IReadOnlyList<EventDay> events, decimal maxDropPercent)
    {
        foreach (var item in events)
        {
            var droppedToZero = item.Yesterday == 0 && item.Average > 0;
            var dropped = item.Average > 0 && item.Yesterday < item.Average * (1m - maxDropPercent / 100m);
            if (!droppedToZero && !dropped) continue;
            var current = item.Average == 0 ? "0" : $"%{((item.Yesterday - item.Average) / item.Average * 100m).ToString("N0", Tr)}";
            yield return new RuleViolation(Code, null, siteUrl, current, $"{item.Name}: dün {item.Yesterday}, 7 gün ortalaması {item.Average.ToString("N1", Tr)}");
        }
    }
}
