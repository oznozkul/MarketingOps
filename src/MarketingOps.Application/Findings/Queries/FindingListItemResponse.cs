using MarketingOps.Domain.Findings;

namespace MarketingOps.Application.Findings.Queries;

public sealed record FindingListItemResponse(
    Guid Id,
    string Title,
    string RuleCode,
    FindingCategory Category,
    Severity Severity,
    FindingStatus Status,
    Guid? PageTypeId,
    string? PageTypeName,
    int AffectedCount,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    bool IsNewInLastCrawl,
    Guid? TaskId,
    string? TaskKey);
