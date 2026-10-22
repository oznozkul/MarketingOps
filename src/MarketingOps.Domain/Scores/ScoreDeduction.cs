using MarketingOps.Domain.Findings;

namespace MarketingOps.Domain.Scores;

public sealed record ScoreDeduction(Guid FindingId, string Title, Severity Severity, Guid? PageTypeId, int AffectedCount, int PageTypePageCount, decimal Points);
