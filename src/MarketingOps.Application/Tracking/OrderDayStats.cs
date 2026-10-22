namespace MarketingOps.Application.Tracking;

public sealed record OrderDayStats(DateOnly Date, int Count, decimal Revenue);
