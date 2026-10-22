using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Queries;

public sealed record TaskListItemResponse(
    Guid Id,
    string Key,
    string Title,
    Guid FindingId,
    Severity Severity,
    FindingStatus FindingStatus,
    string? PageTypeName,
    Team Team,
    WorkTaskStatus Status,
    int CriteriaMet,
    int CriteriaTotal,
    string? ExternalTrackerKey,
    string? ExternalUrl,
    DateTimeOffset UpdatedAt);
