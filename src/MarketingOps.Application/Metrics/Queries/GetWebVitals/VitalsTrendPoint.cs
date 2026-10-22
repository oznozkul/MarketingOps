namespace MarketingOps.Application.Metrics.Queries.GetWebVitals;

public sealed record VitalsTrendPoint(DateOnly Date, int? LcpMs, int? InpMs, double? Cls, int? TtfbMs);
