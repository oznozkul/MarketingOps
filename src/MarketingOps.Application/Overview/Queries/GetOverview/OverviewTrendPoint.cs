namespace MarketingOps.Application.Overview.Queries.GetOverview;

public sealed record OverviewTrendPoint(DateOnly Date, int? Tracking, int? Seo, int? Performance, int? Accessibility);
