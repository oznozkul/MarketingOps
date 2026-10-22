using MediatR;

namespace MarketingOps.Application.Crawls.Queries.GetCrawlById;

public sealed record GetCrawlByIdQuery(Guid SiteId, Guid CrawlId) : IRequest<CrawlDetailResponse>;
