using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class MetaDedupRule : RuleBase
{
    public override string Code => "track.meta.dedup";
    public override string Name => "Meta event tekilleştirmesi yok";
    public override string Description => "Pixel ve CAPI event'leri event_id ile tekilleştiriliyor mu kontrol eder. Kanıt yoksa kural çalışmaz.";
    public override FindingCategory Category => FindingCategory.Tracking;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Site;
    public override RuleDataSource DataSource => RuleDataSource.Tracking;
    public override string ReferenceKey => "tracking:meta";
    protected override string TitleTemplate => "Meta event'leri tekilleşmiyor";
    public override string Impact => "event_id olmadan aynı dönüşüm Pixel ve CAPI'den iki kez sayılabilir.";
    public override Team Team => Team.Seo;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Pixel ve CAPI event'lerine aynı event_id'yi verin.", "1. Tarayıcı event'ine event_id ekleyin.\n2. Aynı değeri sunucu event'ine de koyun.\n3. Events Manager'da tekilleştirmenin göründüğünü kontrol edin.");
    public override string TeamCriterion => "Pixel ve CAPI event'leri event_id ile tekilleşiyor.";

    public IEnumerable<RuleViolation> Evaluate(string siteUrl, MetaSignals signals) =>
        signals.EventIdDedup == false
            ? [new RuleViolation(Code, null, siteUrl, "event_id yok", "Pixel ve CAPI aynı event_id ile tekilleşmiyor.")]
            : [];
}
