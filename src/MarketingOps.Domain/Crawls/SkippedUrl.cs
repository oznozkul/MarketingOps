namespace MarketingOps.Domain.Crawls;

public sealed record SkippedUrl(string Url, SkipReason Reason);
