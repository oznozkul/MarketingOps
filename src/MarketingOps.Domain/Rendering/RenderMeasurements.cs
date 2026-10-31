namespace MarketingOps.Domain.Rendering;

/// <summary>
/// Bir sayfanın bir cihazdaki ölçümü (render-audit.js çıktısı). Listeler en fazla <see cref="MaxElements"/> öğedir.
/// Eşiği ayarlanabilen ölçümler (küçük yazı, dokunma alanı) eşiğin en gevşek değerine kadar toplanır; kural kendi eşiğiyle süzer.
/// </summary>
public sealed record RenderMeasurements
{
    public const int MaxElements = 50;

    public int ViewportWidth { get; init; }
    public int DocumentWidth { get; init; }

    /// <summary>Viewport'un sağından ya da solundan taşan öğeler (kırpan bir atası olmayanlar).</summary>
    public IReadOnlyList<RenderElement> OverflowOffenders { get; init; } = [];

    /// <summary>Hesaplanan genişliği viewport'tan büyük öğeler. Yalnızca mobilde toplanır.</summary>
    public IReadOnlyList<RenderElement> WideElements { get; init; } = [];

    /// <summary>WCAG AA altında kalan metinler; Value oran, Limit gereken oran.</summary>
    public IReadOnlyList<RenderElement> ContrastFailures { get; init; } = [];

    /// <summary>16 px'ten küçük metinler; Value yazı boyutu.</summary>
    public IReadOnlyList<RenderElement> SmallText { get; init; } = [];

    /// <summary>48 px'ten küçük link ve butonlar (metin içi linkler hariç); Value kısa kenar. Yalnızca mobilde toplanır.</summary>
    public IReadOnlyList<RenderElement> SmallTapTargets { get; init; } = [];

    /// <summary>İlk ekranda görünen ama loading="lazy" olan görseller.</summary>
    public IReadOnlyList<RenderElement> LazyAboveFold { get; init; } = [];

    /// <summary>Kaydırınca görünen ama lazy olmayan görseller.</summary>
    public IReadOnlyList<RenderElement> EagerBelowFold { get; init; } = [];

    /// <summary>Doğal oranından %2'den fazla sapmış görseller; Value sapma yüzdesi.</summary>
    public IReadOnlyList<RenderElement> DistortedImages { get; init; } = [];

    public static RenderMeasurements Empty { get; } = new();
}
