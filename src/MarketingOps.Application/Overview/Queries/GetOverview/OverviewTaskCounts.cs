namespace MarketingOps.Application.Overview.Queries.GetOverview;

public sealed record OverviewTaskCounts(int Open, int Critical, int High, int Medium, int Low, int ClosedThisWeek, int ClosedLastWeek);
