using MarketingOps.Application.Common.Models;
using MarketingOps.Domain.Findings;

namespace MarketingOps.Application.Findings.Queries.GetFindingById;

public sealed record FindingDetailResponse(
    FindingListItemResponse Item,
    string RuleName,
    string RuleDescription,
    string Problem,
    string Impact,
    string ReferenceKey,
    string Fingerprint,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? SnoozedUntil,
    string? FalsePositiveReason,
    IReadOnlyList<FindingHistoryEntry> History,
    PagedResponse<FindingOccurrenceResponse> Occurrences,
    int StoredOccurrenceCount);
