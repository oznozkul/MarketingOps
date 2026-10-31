using MarketingOps.Domain.Rendering;

namespace MarketingOps.Application.Rendering;

/// <summary>Bir hedefin ölçümü. <see cref="Measurements"/> null ise <see cref="Error"/> nedenini söyler.</summary>
public sealed record RenderResult(RenderTarget Target, RenderMeasurements? Measurements, bool Obstructed, string? Error);
