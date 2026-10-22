using MediatR;

namespace MarketingOps.Application.Findings.Queries.GetCrawlFindingChanges;

/// <summary>Tarama detayındaki "bulgu değişimleri": bu taramada açılan, çözülen ve tekrar açılan bulgular.</summary>
public sealed record GetCrawlFindingChangesQuery(Guid SiteId, int CrawlNumber) : IRequest<IReadOnlyList<FindingChangeResponse>>;
