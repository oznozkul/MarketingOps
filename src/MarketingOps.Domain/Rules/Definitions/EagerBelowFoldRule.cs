using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rendering;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class EagerBelowFoldRule : RenderRule
{
    public override string Code => "perf.image.eager_below_fold";
    public override string Name => "Ekran altındaki görsel hemen yükleniyor";
    public override string Description => "Masaüstü, tablet ya da telefonda ancak kaydırınca görünen ama lazy olmayan görselleri bulur (loading özniteliği olmayan görsel de hemen yüklenir).";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.Medium;
    public override string ReferenceKey => "perf-doc:p18";
    public override string Impact => "Ekran altındaki görsellerin hemen indirilmesi ilk yüklemede bant genişliğini ilk ekranla paylaştırır; mobil veride ziyaretçinin görmediği görseller indirilir.";
    protected override string TitleTemplate => "{n} sayfada ekran altındaki görseller hemen yükleniyor";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Kaydırınca görünen görseller tembel yüklenmeli.", "<img src=\"/urun.jpg\" alt=\"Ürün adı\" width=\"400\" height=\"400\" loading=\"lazy\" />");
    public override string TeamCriterion => "Liste ve galeri şablonlarında ekran altındaki görseller loading=\"lazy\" kullanıyor.";

    protected override bool NeedsClearView => true;

    protected override IReadOnlyList<RenderElement> Offenders(RenderMeasurements m, RuleContext context) => m.EagerBelowFold;

    protected override string Describe(RenderElement element) => Quote(element);
}
