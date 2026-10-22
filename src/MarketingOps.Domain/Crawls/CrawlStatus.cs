namespace MarketingOps.Domain.Crawls;

public enum CrawlStatus
{
    Queued,
    Running,
    Completed,
    PartiallyCompleted,
    Failed,
    Cancelled
}
