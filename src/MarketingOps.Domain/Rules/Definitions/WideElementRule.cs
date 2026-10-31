using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rendering;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class WideElementRule : RenderRule
{
    public override string Code => "mobile.wide_element";
    public override string Name => "Mobilde ekrandan geniş öğe";
    public override string Description => "Telefonda (375 px) hesaplanan genişliği ekrandan büyük öğeleri bulur; sayfa kırpıldığı için yana kaymasa da içerik kesilir.";
    public override FindingCategory Category => FindingCategory.Accessibility;
    public override Severity DefaultSeverity => Severity.Medium;
    public override string ReferenceKey => "mobile:reflow";
    public override string Impact => "Ekrandan geniş öğenin bir kısmı telefonda görünmez ya da kesilir; tablo, banner ve formlarda bilgi kaybolur.";
    protected override string TitleTemplate => "{n} sayfada mobilde ekrandan geniş öğe var";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Sabit piksel genişlik yerine kapsayıcıya göre genişlik verilmeli.", ".oge { width: auto; max-width: 100%; }\n/* sabit genişlik gerekiyorsa */\n.banner { width: 600px; max-width: 100%; }");
    public override string TeamCriterion => "Şablonlarda sabit piksel genişlikler max-width: 100% ile sınırlandı.";

    protected override IReadOnlyCollection<RenderDevice> Devices => [RenderDevice.Mobile];

    protected override IReadOnlyList<RenderElement> Offenders(RenderMeasurements m, RuleContext context) => m.WideElements;

    protected override string Describe(RenderElement element) => $"{Quote(element)} ({element.Value:0} px)";
}
