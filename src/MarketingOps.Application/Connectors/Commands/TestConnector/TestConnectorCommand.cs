using MarketingOps.Domain.Connectors;
using MediatR;

namespace MarketingOps.Application.Connectors.Commands.TestConnector;

/// <summary>Yetkinin geçerli ve seçili mülkün hâlâ erişilebilir olduğunu kontrol eder. Sonuç bağlantıya da yazılır.</summary>
public sealed record TestConnectorCommand(Guid SiteId, ConnectorType Type) : IRequest<ConnectorTestResult>;
