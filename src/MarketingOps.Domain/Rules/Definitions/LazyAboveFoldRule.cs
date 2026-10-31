using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rendering;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class LazyAboveFoldRule : RenderRule
{
    public override string Code => "perf.image.lazy_above_fold";
    public override string Name => "İlk ekrandaki görsel lazy";
    public override string Description => "Masaüstü, tablet ya da telefonda kaydırmadan görünen ama loading=\"lazy\" olan görselleri bulur. Karuselde yalnızca görünen slayt ilk ekranda sayılır.";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.High;
    public override string ReferenceKey => "perf-doc:p18";
    public override string Impact => "İlk ekrandaki görsel tembel yüklenirse tarayıcı onu geç ister; en büyük içerik görselinin (LCP) gecikmesi sayfanın yavaş görünmesine yol açar.";
    protected override string TitleTemplate => "{n} sayfada ilk ekrandaki görsel lazy";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "İlk ekrandaki görseller açıkça eager yüklenmeli; en büyüğüne yüksek öncelik verilmeli.", "<img src=\"/banner.jpg\" alt=\"Kış kampanyası\" width=\"1200\" height=\"500\" loading=\"eager\" fetchpriority=\"high\" />");
    public override string TeamCriterion => "Şablonlarda ilk ekrandaki görseller loading=\"eager\" kullanıyor.";

    protected override bool NeedsClearView => true;

    protected override IReadOnlyList<RenderElement> Offenders(RenderMeasurements m, RuleContext context) => m.LazyAboveFold;

    protected override string Describe(RenderElement element) => Quote(element);
}
