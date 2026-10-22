using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Connectors;

public sealed record ConnectorAuthorizationExpired(Guid AccountId, Guid SiteId, ConnectorType Type) : IDomainEvent;
