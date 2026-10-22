using MarketingOps.Domain.Crawls;

namespace MarketingOps.Application.Crawls.Queries.GetCrawlById;

public sealed record CrawlStageResponse(CrawlStage Stage, DateTimeOffset StartedAt, DateTimeOffset? FinishedAt)
{
    public TimeSpan? Duration => FinishedAt - StartedAt;
}
