namespace MarketingOps.Application.Tracking.Queries.GetOrderComparison;

public sealed record OrderComparisonResponse(decimal ThresholdPercent, bool HasOrders, bool HasPurchases, IReadOnlyList<OrderComparisonRowResponse> Days);
