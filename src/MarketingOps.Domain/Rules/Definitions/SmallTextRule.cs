using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rendering;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class SmallTextRule : RenderRule
{
    public static readonly RuleThreshold MinFontSize = new("minPx", "En küçük yazı boyutu", "px", 12m, 9m, 16m);

    public override IReadOnlyList<RuleThreshold> Thresholds => [MinFontSize];
    public override string Code => "a11y.text.small";
    public override string Name => "Çok küçük yazı";
    public override string Description => "Ekranda görünen ve yazı boyutu eşiğin altında kalan metinleri bulur.";
    public override FindingCategory Category => FindingCategory.Accessibility;
    public override Severity DefaultSeverity => Severity.Medium;
    public override string ReferenceKey => "a11y:text-size";
    public override string Impact => "Küçük yazı telefonda yakınlaştırmadan okunamaz; karar verdiren bilgiler (fiyat, kargo, stok) gözden kaçar.";
    protected override string TitleTemplate => "{n} sayfada çok küçük yazı var";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Okunması gereken metin eşik ve üstü boyutta olmalı.", "body { font-size: 16px; }\n.yardimci-metin { font-size: 13px; } /* eşiğin altına inmeyin */");
    public override string TeamCriterion => "Tasarım sistemindeki en küçük yazı boyutu eşiğe çıkarıldı.";

    protected override IReadOnlyList<RenderElement> Offenders(RenderMeasurements m, RuleContext context)
    {
        var min = (double)context.Threshold(MinFontSize);
        return m.SmallText.Where(e => e.Value < min).ToList();
    }

    protected override string Describe(RenderElement element) => $"{Quote(element)} ({element.Value.ToString("0.#", Number)} px)";
}
