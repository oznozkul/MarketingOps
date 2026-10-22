namespace MarketingOps.Application.Connectors.Google;

public sealed record SitemapInfo(string Path, DateTimeOffset? LastSubmitted, DateTimeOffset? LastDownloaded, bool IsPending, int Errors, int Warnings,
    long? Submitted, long? Indexed);
