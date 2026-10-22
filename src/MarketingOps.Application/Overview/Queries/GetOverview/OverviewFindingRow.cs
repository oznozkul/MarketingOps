using MarketingOps.Domain.Findings;

namespace MarketingOps.Application.Overview.Queries.GetOverview;

internal sealed record OverviewFindingRow(Guid Id, string Title, string RuleCode, Severity Severity, FindingStatus Status, Guid? PageTypeId, int AffectedCount);
