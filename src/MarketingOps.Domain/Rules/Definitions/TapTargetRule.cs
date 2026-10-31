using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rendering;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class TapTargetRule : RenderRule
{
    public static readonly RuleThreshold MinSize = new("minPx", "En küçük dokunma alanı", "px", 24m, 16m, 48m);

    public override IReadOnlyList<RuleThreshold> Thresholds => [MinSize];
    public override string Code => "mobile.tap_target.small";
    public override string Name => "Küçük dokunma alanı";
    public override string Description => "Telefonda (375 px) link ve butonların dokunma alanının kısa kenarını ölçer; metin içindeki linkler sayılmaz (WCAG 2.5.8).";
    public override FindingCategory Category => FindingCategory.Accessibility;
    public override Severity DefaultSeverity => Severity.Medium;
    public override string ReferenceKey => "a11y:tap-targets";
    public override string Impact => "Küçük ve birbirine yakın hedeflerde ziyaretçi yanlış öğeye dokunur; sepete ekleme, filtre ve sayfalama gibi işlemler zorlaşır.";
    protected override string TitleTemplate => "{n} sayfada küçük dokunma alanı var";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Dokunma alanı padding ile büyütülmeli; ikon küçük kalabilir.", ".ikon-buton { min-width: 44px; min-height: 44px; display: inline-flex; align-items: center; justify-content: center; }");
    public override string TeamCriterion => "İkon buton ve linkler mobilde eşik ve üstü dokunma alanı veriyor.";

    protected override IReadOnlyCollection<RenderDevice> Devices => [RenderDevice.Mobile];

    protected override IReadOnlyList<RenderElement> Offenders(RenderMeasurements m, RuleContext context)
    {
        var min = (double)context.Threshold(MinSize);
        return m.SmallTapTargets.Where(e => e.Value < min).ToList();
    }

    protected override string Describe(RenderElement element) => $"{Quote(element)} ({element.Value:0} px)";
}
