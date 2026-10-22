using MediatR;

namespace MarketingOps.Application.Findings.Queries.GetFindingById;

/// <summary>Bulgu detayı; etkilenen sayfalar aranabilir ve sayfalıdır.</summary>
public sealed record GetFindingByIdQuery(Guid SiteId, Guid FindingId, string? OccurrenceSearch = null, int OccurrencePage = 1, int OccurrencePageSize = 25)
    : IRequest<FindingDetailResponse>;
