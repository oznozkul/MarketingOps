using MarketingOps.Domain.Findings;

namespace MarketingOps.Application.Rules.Queries.GetRule;

public sealed record RuleFindingResponse(Guid Id, string Title, Severity Severity, FindingStatus Status, string? PageTypeName, int AffectedCount);
