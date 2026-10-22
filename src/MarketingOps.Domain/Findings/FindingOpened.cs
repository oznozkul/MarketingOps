using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Findings;

/// <summary>Görev üretimi (Faz 4) ve bildirimler (Faz 7) bu olayı dinler.</summary>
public sealed record FindingOpened(Guid FindingId, Guid SiteId, string RuleCode, Severity Severity) : IDomainEvent;
