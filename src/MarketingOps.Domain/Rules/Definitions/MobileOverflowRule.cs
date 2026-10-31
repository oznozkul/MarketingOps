using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rendering;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class MobileOverflowRule : RenderRule
{
    private const int Tolerance = 4;

    public override string Code => "mobile.overflow";
    public override string Name => "Mobilde yatay kaydırma";
    public override string Description => "Sayfanın telefonda (375 px) ekran genişliğinden taşıp yana kayıp kaymadığını ölçer; taşmaya yol açan öğeleri listeler.";
    public override FindingCategory Category => FindingCategory.Accessibility;
    public override Severity DefaultSeverity => Severity.High;
    public override string ReferenceKey => "mobile:reflow";
    public override string Impact => "Yana kayan sayfada içerik ekrandan taşar; ziyaretçi metni okumak için iki yönde kaydırmak zorunda kalır ve mobil kullanılabilirlik bozulur.";
    protected override string TitleTemplate => "{n} sayfa mobilde yana kayıyor";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Taşan öğelerin genişliği ekranla sınırlanmalı.", "img, video, iframe, table { max-width: 100%; }\n.tablo-kapsayici { overflow-x: auto; }\n.uzun-metin { overflow-wrap: anywhere; }");
    public override string TeamCriterion => "Şablonlar 375 px genişlikte yana kaymadan açılıyor.";

    protected override IReadOnlyCollection<RenderDevice> Devices => [RenderDevice.Mobile];

    protected override IReadOnlyList<RenderElement> Offenders(RenderMeasurements m, RuleContext context) =>
        m.DocumentWidth <= m.ViewportWidth + Tolerance ? []
        : m.OverflowOffenders.Count > 0 ? m.OverflowOffenders
        : [new RenderElement("html", null, m.DocumentWidth)];

    protected override string Current(IReadOnlyList<(RenderSample Sample, IReadOnlyList<RenderElement> Offenders)> hits)
    {
        var m = hits[0].Sample.Measurements;
        return $"{m.ViewportWidth} px'de {m.DocumentWidth} px";
    }

    protected override string Describe(RenderElement element) => $"{Quote(element)} ({element.Value:0} px)";
}
