namespace MarketingOps.Application.Connectors.Google;

public sealed record SearchPerformanceRow(DateOnly Date, string Page, int Clicks, int Impressions, double Ctr, double Position);
