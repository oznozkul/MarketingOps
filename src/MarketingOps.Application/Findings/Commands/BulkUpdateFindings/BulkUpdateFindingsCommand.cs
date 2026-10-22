using MediatR;

namespace MarketingOps.Application.Findings.Commands.BulkUpdateFindings;

/// <summary>Seçili bulgulara toplu erteleme ya da yanlış alarm. Durumu uygun olmayanlar atlanır ve sayılır.</summary>
public sealed record BulkUpdateFindingsCommand(
    Guid SiteId,
    IReadOnlyList<Guid> FindingIds,
    BulkFindingAction Action,
    string? Reason,
    DateOnly? Until,
    string Actor) : IRequest<BulkUpdateResult>;
