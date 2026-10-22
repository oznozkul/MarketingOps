using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Crawls;

public sealed record CrawlFailed(Guid CrawlId, Guid SiteId, int Number) : IDomainEvent;
