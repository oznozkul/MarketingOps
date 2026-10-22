using MarketingOps.Domain.Metrics;

namespace MarketingOps.Application.Connectors.Google;

public sealed record PageSpeedResult(VitalsSource Source, int? LcpMs, int? InpMs, double? Cls, int? TtfbMs);
