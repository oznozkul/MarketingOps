using MarketingOps.Domain.Findings;

namespace MarketingOps.Application.Scores;

public sealed record ScoreFact(Guid Id, string Title, Severity Severity, FindingCategory Category, Guid? PageTypeId, int AffectedCount);
