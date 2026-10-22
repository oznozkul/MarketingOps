using MarketingOps.Application.Common.Models;
using MediatR;

namespace MarketingOps.Application.Crawls.Queries.GetCrawls;

public sealed record GetCrawlsQuery(Guid SiteId, int Page = 1, int PageSize = 25) : IRequest<PagedResponse<CrawlListItemResponse>>;
