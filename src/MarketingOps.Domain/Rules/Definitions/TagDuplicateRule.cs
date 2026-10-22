using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Tracking;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class TagDuplicateRule : RuleBase
{
    public override string Code => "track.tag.duplicate";
    public override string Name => "Tag iki kez yüklü";
    public override string Description => "Aynı sayfada bir satıcıya ait birden fazla farklı kimlik yükleniyor mu kontrol eder.";
    public override FindingCategory Category => FindingCategory.Tracking;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override RuleDataSource DataSource => RuleDataSource.Tracking;
    public override string ReferenceKey => "tracking:tags";
    protected override string TitleTemplate => "{n} sayfada tag iki kez yüklü";
    public override string Impact => "Aynı tag'in iki kimlikle yüklenmesi event'leri çift sayar.";
    public override Team Team => Team.Seo;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Sayfada satıcı başına tek kimlik bırakın.", "1. GTM ve sayfa kaynağındaki doğrudan snippet'i karşılaştırın.\n2. Aynı container'ın iki kez eklenmediğini kontrol edin.\n3. Eski ve yeni kimlikten yalnızca beklenen kalsın.");
    public override string TeamCriterion => "Sayfada satıcı başına tek kimlik var.";

    public IEnumerable<RuleViolation> Evaluate(IReadOnlyList<TagAuditResult> cells) =>
        cells.Where(c => c.Status == TagAuditStatus.Duplicate)
            .Select(c => new RuleViolation(Code, c.PageTypeId, c.Url, string.Join(", ", c.FoundIds), $"{c.Vendor}: birden fazla kimlik görüldü."));
}
