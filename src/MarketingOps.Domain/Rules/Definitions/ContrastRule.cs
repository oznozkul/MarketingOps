using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rendering;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class ContrastRule : RenderRule
{
    public override string Code => "a11y.contrast";
    public override string Name => "Yetersiz renk kontrastı";
    public override string Description => "Metin ile arka planı arasındaki kontrastı WCAG AA'ya göre ölçer: normal metin 4,5:1, büyük metin (24 px ya da 18,66 px kalın) 3:1. Arka planı görsel olan metin ölçülmez.";
    public override FindingCategory Category => FindingCategory.Accessibility;
    public override Severity DefaultSeverity => Severity.High;
    public override string ReferenceKey => "a11y:contrast";
    public override string Impact => "Düşük kontrastlı metin az gören kullanıcılar ve güneş altında telefon kullananlar için okunmaz; fiyat, stok ve buton metinlerinde satışa doğrudan etki eder.";
    protected override string TitleTemplate => "{n} sayfada renk kontrastı yetersiz";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Metin rengi ile arka plan arasındaki oran WCAG AA eşiğine çıkarılmalı.", "1. Bulgudaki öğelerin metin ve arka plan renklerini tasarım sisteminde bulun.\n2. Metni koyulaştırın ya da arka planı açın; oranı bir kontrast denetleyiciyle doğrulayın.\n3. Görsel üstündeki metne yarı saydam bir zemin ekleyin.\n4. Değişikliği tasarım sisteminin renk değişkenlerinde yapın ki bütün şablonlara yansısın.");
    public override string TeamCriterion => "Renk paleti AA kontrastını sağlayacak şekilde güncellendi.";

    protected override bool NeedsClearView => true;

    protected override IReadOnlyList<RenderElement> Offenders(RenderMeasurements m, RuleContext context) => m.ContrastFailures;

    protected override string Describe(RenderElement element) =>
        $"{Quote(element)} {element.Value.ToString("0.0", Number)}:1 (gereken {element.Limit?.ToString("0.0", Number)}:1)";
}
