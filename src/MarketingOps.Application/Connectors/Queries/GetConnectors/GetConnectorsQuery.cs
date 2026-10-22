using MediatR;

namespace MarketingOps.Application.Connectors.Queries.GetConnectors;

/// <summary>Sitenin tüm connector türleri; kaydı olmayan tür "Bağlı değil" döner.</summary>
public sealed record GetConnectorsQuery(Guid SiteId) : IRequest<ConnectorsResponse>;
