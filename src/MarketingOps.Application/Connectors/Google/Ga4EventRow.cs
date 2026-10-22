namespace MarketingOps.Application.Connectors.Google;

public sealed record Ga4EventRow(DateOnly Date, string EventName, long Count, decimal Value);
