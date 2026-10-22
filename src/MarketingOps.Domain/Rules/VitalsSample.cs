using MarketingOps.Domain.Metrics;

namespace MarketingOps.Domain.Rules;

/// <summary>Bir örnek sayfanın en güncel Core Web Vitals ölçümü.</summary>
public sealed record VitalsSample(string Url, Guid? PageTypeId, DeviceStrategy Strategy, int? LcpMs, int? InpMs, double? Cls, int? TtfbMs);
