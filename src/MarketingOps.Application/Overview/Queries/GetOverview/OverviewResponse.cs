using MarketingOps.Application.Tracking.Queries.GetOrderComparison;

namespace MarketingOps.Application.Overview.Queries.GetOverview;

public sealed record OverviewResponse(
    bool HasCompletedCrawl,
    bool Ga4Connected,
    OverviewIssueResponse? Critical,
    int ExtraCritical,
    IReadOnlyList<OverviewScoreResponse> Scores,
    OverviewTaskCounts Tasks,
    IReadOnlyList<OverviewIssueResponse> Issues,
    OrderComparisonResponse Orders,
    IReadOnlyList<OverviewTrendPoint> Trend,
    IReadOnlyList<OverviewTrendEvent> Events,
    IReadOnlyList<OverviewPageTypeRow> PageTypes);
