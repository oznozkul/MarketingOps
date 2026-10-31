using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rendering;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class DistortedImageRule : RenderRule
{
    public override string Code => "content.image.distorted";
    public override string Name => "Görsel orantısız gösteriliyor";
    public override string Description => "Ekranda doğal en-boy oranından %2'den fazla farklı çizilen (gerilmiş ya da basılmış) görselleri bulur. object-fit ile kırpılan görseller ve SVG sayılmaz.";
    public override FindingCategory Category => FindingCategory.Content;
    public override Severity DefaultSeverity => Severity.Medium;
    public override string ReferenceKey => "google:images";
    public override string Impact => "Gerilmiş ya da basılmış ürün görseli ürünü olduğundan farklı gösterir; güveni ve dönüşümü düşürür.";
    protected override string TitleTemplate => "{n} sayfada görsel orantısız gösteriliyor";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Görsel kutusu dosyanın oranını korumalı ya da object-fit ile kırpmalı.", ".urun-gorseli { width: 100%; height: auto; }\n/* sabit kutu gerekiyorsa */\n.kart-gorseli { width: 100%; height: 240px; object-fit: cover; }");
    public override string TeamCriterion => "Görsel bileşenleri oranı koruyor ya da object-fit kullanıyor.";

    protected override IReadOnlyList<RenderElement> Offenders(RenderMeasurements m, RuleContext context) => m.DistortedImages;

    protected override string Describe(RenderElement element) => $"{Quote(element)} (%{element.Value.ToString("0", Number)} sapma)";
}
