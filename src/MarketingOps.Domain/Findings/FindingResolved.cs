using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Findings;

/// <summary>Bulgu taramada görülmedi ve çözüldü; bağlı görevin otomatik kriterleri bununla sağlanır.</summary>
public sealed record FindingResolved(Guid FindingId, Guid SiteId, int CrawlNumber) : IDomainEvent;
