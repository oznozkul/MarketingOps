namespace MarketingOps.Application.Crawls.Queries.GetCrawlById;

public sealed record CrawlDetailResponse(
    CrawlListItemResponse Crawl,
    string? ErrorMessage,
    IReadOnlyList<CrawlStageResponse> Stages,
    IReadOnlyList<PageTypeCountResponse> PageTypes,
    int FailedCount,
    IReadOnlyList<FailedPageResponse> FailedPages,
    int SkippedCount,
    IReadOnlyList<SkippedUrlResponse> SkippedUrls,
    DateTimeOffset? SnapshotsPrunedAt,
    RenderSummaryResponse Render);
