using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Tracking;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class ConsentPrefireRule : RuleBase
{
    public override string Code => "track.consent.prefire";
    public override string Name => "Onay öncesi tetiklenen tag";
    public override string Description => "Analitik veya reklam isteği çerez onayı verilmeden önce gidiyor mu kontrol eder. GTM'in kendisi onay sayılmaz.";
    public override FindingCategory Category => FindingCategory.Tracking;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override RuleDataSource DataSource => RuleDataSource.Tracking;
    public override string ReferenceKey => "tracking:tags";
    protected override string TitleTemplate => "{n} sayfada tag onaydan önce gidiyor";
    public override string Impact => "Onaydan önce giden ölçüm, rıza verilmemiş ziyaretleri de kaydeder.";
    public override Team Team => Team.Seo;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Analitik ve reklam tag'lerini onay sonrası tetikleyin.", "1. GA4, Google Ads ve Meta tag'lerine consent gerektiren tetikleyici bağlayın.\n2. GTM consent mode'da reklam ve analitik depolamasını varsayılan olarak kapalı tutun.\n3. Onay tıklanınca ilgili depolamanın açıldığını doğrulayın.");
    public override string TeamCriterion => "Analitik ve reklam tag'leri yalnızca onaydan sonra gidiyor.";

    public IEnumerable<RuleViolation> Evaluate(IReadOnlyList<TagAuditResult> cells) =>
        cells.Where(c => c.Status == TagAuditStatus.FiresBeforeConsent)
            .Select(c => new RuleViolation(Code, c.PageTypeId, c.Url, "Onay öncesi", $"{c.Vendor}: onay verilmeden istek gitti."));
}
