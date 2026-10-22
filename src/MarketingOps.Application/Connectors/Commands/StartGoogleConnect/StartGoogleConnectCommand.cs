using MediatR;

namespace MarketingOps.Application.Connectors.Commands.StartGoogleConnect;

/// <summary>Google izin ekranının adresini döner; kullanıcı oraya yönlendirilir.</summary>
public sealed record StartGoogleConnectCommand(Guid SiteId, string RedirectUri) : IRequest<string>;
