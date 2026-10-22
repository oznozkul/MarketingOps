namespace MarketingOps.Application.Crawls.Queries.GetCrawlStatus;

public sealed record CrawlStatusResponse(CrawlListItemResponse? Active, CrawlListItemResponse? LastFinished);
