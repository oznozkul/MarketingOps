using MediatR;

namespace MarketingOps.Application.Crawls.Queries.GetCrawlStatus;

/// <summary>Üst bar ve "Şimdi tara" butonu için: varsa aktif tarama ve en son başarıyla biten tarama.</summary>
public sealed record GetCrawlStatusQuery(Guid SiteId) : IRequest<CrawlStatusResponse>;
