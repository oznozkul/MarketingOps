using MarketingOps.Domain.Crawls;

namespace MarketingOps.Application.Crawls.Queries;

public sealed record CrawlListItemResponse(
    Guid Id,
    int Number,
    CrawlStatus Status,
    CrawlTrigger TriggeredBy,
    DateTimeOffset QueuedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    int PagesDiscovered,
    int PagesCrawled,
    int PagesFailed,
    DateTimeOffset? EvaluatedAt,
    int NewFindings,
    int ResolvedFindings,
    int ReopenedFindings)
{
    public TimeSpan? Duration => StartedAt is null ? null : (FinishedAt ?? DateTimeOffset.UtcNow) - StartedAt;
    public bool IsActive => Status is CrawlStatus.Queued or CrawlStatus.Running;
}
