namespace MarketingOps.Domain.Rendering;

/// <summary>
/// Render'da sorunlu bulunan bir öğe. <see cref="Selector"/>: ekibin öğeyi bulacağı kısa CSS yolu; <see cref="Value"/>: ölçülen
/// değer (kontrast oranı, px cinsinden boyut, genişlik); <see cref="Limit"/>: aşılmaması gereken değer, kural sabitse (kontrast).
/// </summary>
public sealed record RenderElement(string Selector, string? Text, double Value, double? Limit = null);
