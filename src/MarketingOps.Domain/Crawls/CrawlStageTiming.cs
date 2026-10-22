namespace MarketingOps.Domain.Crawls;

public sealed record CrawlStageTiming(CrawlStage Stage, DateTimeOffset StartedAt, DateTimeOffset? FinishedAt);
