using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Tracking;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class TagMissingRule : RuleBase
{
    public override string Code => "track.tag.missing";
    public override string Name => "Tag eksik";
    public override string Description => "Beklenen ölçüm kimliği sayfada hiç yükleniyor mu kontrol eder.";
    public override FindingCategory Category => FindingCategory.Tracking;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override RuleDataSource DataSource => RuleDataSource.Tracking;
    public override string ReferenceKey => "tracking:tags";
    protected override string TitleTemplate => "{n} sayfada beklenen tag yüklenmiyor";
    public override string Impact => "Beklenen tag yüklenmeyen sayfada o kanalın ölçümü boş kalır.";
    public override Team Team => Team.Seo;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Beklenen tag'in sayfada bir kez yüklendiğini doğrulayın.", "1. GTM ya da doğrudan snippet'in bu sayfa tipinde yayınlandığını kontrol edin.\n2. Tetikleyicinin sayfa tipini dışlamadığını kontrol edin.\n3. Kimliğin Ayarlar'daki beklenen değerle aynı olduğunu doğrulayın.");
    public override string TeamCriterion => "Beklenen tag sayfada yükleniyor.";

    public IEnumerable<RuleViolation> Evaluate(IReadOnlyList<TagAuditResult> cells) =>
        cells.Where(c => c.Status == TagAuditStatus.Missing)
            .Select(c => new RuleViolation(Code, c.PageTypeId, c.Url, "Eksik", $"{c.Vendor}: beklenen kimlik bu sayfada hiç görülmedi."));
}
