namespace MarketingOps.Application.Rendering;

/// <summary>Sitenin hız limiti render'a da uygulanır; onay seçicisi tracking denetimindekiyle aynıdır.</summary>
public sealed record RenderOptions(int MaxRequestsPerSecond, string? ConsentSelector);
