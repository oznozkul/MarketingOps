using MarketingOps.Domain.Findings;

namespace MarketingOps.Application.Overview.Queries.GetOverview;

public sealed record OverviewIssueResponse(Guid Id, string Title, string RuleCode, Severity Severity, FindingStatus Status, int AffectedCount, string? PageTypeName);
