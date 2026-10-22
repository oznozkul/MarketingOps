using MarketingOps.Domain.Crawls;

namespace MarketingOps.Application.Crawls.Queries.GetActiveCrawls;

public sealed record ActiveCrawlResponse(
    Guid SiteId,
    Guid CrawlId,
    int Number,
    CrawlStatus Status,
    CrawlStage? CurrentStage,
    DateTimeOffset? StartedAt,
    int PagesDiscovered,
    int PagesCrawled,
    int PagesFailed);
