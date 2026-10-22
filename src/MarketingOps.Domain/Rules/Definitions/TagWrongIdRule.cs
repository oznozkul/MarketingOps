using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Tracking;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class TagWrongIdRule : RuleBase
{
    public override string Code => "track.tag.wrong_id";
    public override string Name => "Yanlış ölçüm kimliği";
    public override string Description => "Yüklenen kimlik Ayarlar'daki beklenen kimlikle aynı mı kontrol eder. Beklenen kimlik yoksa kural çalışmaz.";
    public override FindingCategory Category => FindingCategory.Tracking;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override RuleDataSource DataSource => RuleDataSource.Tracking;
    public override string ReferenceKey => "tracking:tags";
    protected override string TitleTemplate => "{n} sayfada yanlış ölçüm kimliği";
    public override string Impact => "Yanlış kimlik, veriyi başka bir mülke ya da hesaba yazar.";
    public override Team Team => Team.Seo;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Yüklenen kimliği Ayarlar'daki beklenen değerle eşitleyin.", "1. Sayfadaki kimliği Ayarlar'daki beklenen kimlikle karşılaştırın.\n2. GTM değişkenindeki kimliği güncelleyin.\n3. Eski mülke giden snippet'i kaldırın.");
    public override string TeamCriterion => "Yüklenen kimlik beklenen kimlikle aynı.";

    public IEnumerable<RuleViolation> Evaluate(IReadOnlyList<TagAuditResult> cells) =>
        cells.Where(c => c.Status == TagAuditStatus.WrongId)
            .Select(c => new RuleViolation(Code, c.PageTypeId, c.Url, string.Join(", ", c.FoundIds), $"{c.Vendor}: bulunan kimlik beklenenle uyuşmuyor."));
}
