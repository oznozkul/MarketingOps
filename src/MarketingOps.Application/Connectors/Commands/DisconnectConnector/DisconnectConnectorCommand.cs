using MarketingOps.Domain.Connectors;
using MediatR;

namespace MarketingOps.Application.Connectors.Commands.DisconnectConnector;

/// <summary>Bağlantıyı ve şifreli yetkiyi siler. Toplanmış geçmiş veri silinmez; sahiplik doğrulaması korunur.</summary>
public sealed record DisconnectConnectorCommand(Guid SiteId, ConnectorType Type) : IRequest;
