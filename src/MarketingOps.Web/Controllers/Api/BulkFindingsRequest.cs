using MarketingOps.Application.Findings.Commands.BulkUpdateFindings;

namespace MarketingOps.Web.Controllers.Api;

public sealed record BulkFindingsRequest(IReadOnlyList<Guid> FindingIds, BulkFindingAction Action, string? Reason, DateOnly? Until);
