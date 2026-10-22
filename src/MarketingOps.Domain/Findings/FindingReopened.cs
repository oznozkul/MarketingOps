using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Findings;

/// <summary>Çözülmüş bir bulgu yeniden görüldü (regresyon).</summary>
public sealed record FindingReopened(Guid FindingId, Guid SiteId, string RuleCode, Severity Severity, int CrawlNumber) : IDomainEvent;
