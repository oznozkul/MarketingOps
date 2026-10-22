using MarketingOps.Domain.Findings;

namespace MarketingOps.Application.Scores.Queries.GetScoreBreakdown;

public sealed record ScoreDeductionResponse(
    Guid FindingId,
    string Title,
    Severity Severity,
    string? PageTypeName,
    int AffectedCount,
    int PageTypePageCount,
    decimal Points);
