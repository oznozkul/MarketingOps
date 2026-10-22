using MarketingOps.Domain.Findings;

namespace MarketingOps.Application.Findings.Queries.GetCrawlFindingChanges;

public sealed record FindingChangeResponse(Guid FindingId, string Title, Severity Severity, FindingEventType Change);
