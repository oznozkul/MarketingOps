using MediatR;

namespace MarketingOps.Application.Crawls.Queries.GetActiveCrawls;

/// <summary>Canlı ilerleme yayını için tüm sitelerdeki aktif taramalar.</summary>
public sealed record GetActiveCrawlsQuery : IRequest<IReadOnlyList<ActiveCrawlResponse>>;
