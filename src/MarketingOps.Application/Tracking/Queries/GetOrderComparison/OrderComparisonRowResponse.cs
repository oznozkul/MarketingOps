namespace MarketingOps.Application.Tracking.Queries.GetOrderComparison;

public sealed record OrderComparisonRowResponse(DateOnly Date, int? OrderCount, decimal? OrderRevenue, long? PurchaseCount, decimal? PurchaseValue, decimal? GapPercent);
