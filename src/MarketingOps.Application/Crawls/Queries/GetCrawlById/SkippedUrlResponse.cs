using MarketingOps.Domain.Crawls;

namespace MarketingOps.Application.Crawls.Queries.GetCrawlById;

public sealed record SkippedUrlResponse(string Url, SkipReason Reason);
