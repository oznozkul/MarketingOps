namespace MarketingOps.Application.Notifications;

public sealed record SummaryReport(
    string SiteName,
    string PeriodLabel,
    IReadOnlyList<SummaryScoreLine> Scores,
    IReadOnlyList<SummaryIssueLine> Issues,
    IReadOnlyList<SummaryTaskLine> ClosedTasks,
    string OrderLine);
