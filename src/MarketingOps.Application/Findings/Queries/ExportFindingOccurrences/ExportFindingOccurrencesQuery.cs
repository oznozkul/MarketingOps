using MarketingOps.Application.Findings.Queries.GetFindingById;
using MediatR;

namespace MarketingOps.Application.Findings.Queries.ExportFindingOccurrences;

/// <summary>Bulgunun saklanan tüm etkilenen sayfaları (CSV indirme için).</summary>
public sealed record ExportFindingOccurrencesQuery(Guid SiteId, Guid FindingId) : IRequest<IReadOnlyList<FindingOccurrenceResponse>>;
