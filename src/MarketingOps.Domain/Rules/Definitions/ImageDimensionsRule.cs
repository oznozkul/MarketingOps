using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class ImageDimensionsRule : RuleBase, IPageRule
{
    public override string Code => "perf.image.dimensions_missing";
    public override string Name => "Görsel boyutu belirtilmemiş";
    public override string Description => "width ve height öznitelikleri olmayan görselleri bulur.";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "perf-doc:p26";
    public override string Impact => "Boyutu bilinmeyen görseller yüklendikçe sayfa kayar (CLS); kullanıcı yanlış öğeye tıklayabilir.";
    protected override string TitleTemplate => "{n} sayfada boyutu belirtilmemiş görsel";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Görsellere width ve height verilmeli ki yüklenirken sayfa kaymasın.", "<img src=\"/gorsel.jpg\" alt=\"Açıklama\" width=\"800\" height=\"600\" />");
    public override string TeamCriterion => "Görsel şablonu width ve height öznitelikleriyle güncellendi.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page)) yield break;
        var missing = page.Data.Images.Where(i => string.IsNullOrWhiteSpace(i.Width) || string.IsNullOrWhiteSpace(i.Height)).ToList();
        if (missing.Count > 0) yield return Violation(page, $"{missing.Count} görsel", string.Join(", ", missing.Take(5).Select(i => i.Src)));
    }
}
