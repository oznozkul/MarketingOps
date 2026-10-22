using MediatR;

namespace MarketingOps.Application.Connectors.Commands.CompleteGoogleConnect;

/// <summary>
/// Google dönüşü: kodu refresh token'a çevirir ve Search Console, GA4, GTM bağlantılarına yetkiyi yazar.
/// <see cref="Error"/> doluysa kullanıcı izni vermemiştir; yalnızca site bulunur ve sonuç başarısız döner.
/// </summary>
public sealed record CompleteGoogleConnectCommand(string State, string? Code, string? Error, string RedirectUri) : IRequest<GoogleConnectResult>;
