using MarketingOps.Domain.Metrics;

namespace MarketingOps.Application.Metrics.Queries.GetWebVitals;

/// <summary>Son ölçüm gününde örnek sayfaların ortancası. <see cref="Trend"/>: gün gün ortancalar (eskiden yeniye).</summary>
public sealed record PageTypeVitalsResponse(
    Guid? PageTypeId,
    string Name,
    int SampleCount,
    VitalsSource Source,
    int? LcpMs,
    int? InpMs,
    double? Cls,
    int? TtfbMs,
    IReadOnlyList<VitalsTrendPoint> Trend);
