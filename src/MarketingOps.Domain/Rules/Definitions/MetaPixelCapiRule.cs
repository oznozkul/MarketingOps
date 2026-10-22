using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class MetaPixelCapiRule : RuleBase
{
    public override string Code => "track.meta.pixel_capi";
    public override string Name => "Meta Pixel ve CAPI eksik";
    public override string Description => "Meta Pixel ile Conversions API'nin ikisi birden aktif mi kontrol eder. Bağlantı yoksa kural çalışmaz.";
    public override FindingCategory Category => FindingCategory.Tracking;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Site;
    public override RuleDataSource DataSource => RuleDataSource.Tracking;
    public override string ReferenceKey => "tracking:meta";
    protected override string TitleTemplate => "Meta Pixel ve CAPI birlikte değil";
    public override string Impact => "Yalnızca Pixel ya da yalnızca CAPI, tarayıcı engelinde Meta eşleşmesini düşürür.";
    public override Team Team => Team.Seo;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Pixel ve CAPI'nin ikisini de aynı pixel'e bağlayın.", "1. Pixel'in sitede yüklendiğini doğrulayın.\n2. Sunucu tarafı event'lerin aynı pixel kimliğine gittiğini doğrulayın.\n3. Events Manager'da iki kaynağın da göründüğünü kontrol edin.");
    public override string TeamCriterion => "Pixel ve CAPI birlikte aktif.";

    public IEnumerable<RuleViolation> Evaluate(string siteUrl, MetaSignals signals)
    {
        if (!signals.PixelActive)
            yield return new RuleViolation(Code, null, siteUrl, "Pixel yok", "Meta Pixel aktif görünmüyor.");
        else if (signals.CapiActive == false)
            yield return new RuleViolation(Code, null, siteUrl, "CAPI yok", "Conversions API bu pixel için aktif değil.");
    }
}
