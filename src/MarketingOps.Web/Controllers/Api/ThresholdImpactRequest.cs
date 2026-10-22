namespace MarketingOps.Web.Controllers.Api;

public sealed record ThresholdImpactRequest(IReadOnlyDictionary<string, decimal> Thresholds);
