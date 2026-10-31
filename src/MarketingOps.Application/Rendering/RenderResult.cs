using MarketingOps.Domain.Rendering;

namespace MarketingOps.Application.Rendering;

/// <summary>
/// Bir hedefin ölçümü. <see cref="Measurements"/> null ise <see cref="Error"/> nedenini söyler.
/// <see cref="ScreenshotJpeg"/>: ölçümde sorun bulunduysa, sorunlu öğeler çerçevelenmiş kanıt görüntüsü (yoksa null).
/// </summary>
public sealed record RenderResult(RenderTarget Target, RenderMeasurements? Measurements, bool Obstructed, string? Error, byte[]? ScreenshotJpeg = null, int ScreenshotHeight = 0);
