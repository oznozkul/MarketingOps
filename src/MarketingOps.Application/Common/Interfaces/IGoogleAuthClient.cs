using MarketingOps.Application.Connectors.Google;

namespace MarketingOps.Application.Common.Interfaces;

/// <summary>
/// Google OAuth (tek akış, salt okunur scope'lar). <see cref="IsConfigured"/> false ise uygulamanın OAuth istemcisi
/// (Client ID/Secret) ayarlı değildir ve bağlama başlatılamaz.
/// </summary>
public interface IGoogleAuthClient
{
    bool IsConfigured { get; }

    string BuildAuthorizationUrl(string state, string redirectUri);

    Task<GoogleAuthorization> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken);

    /// <summary>Refresh token'dan kısa ömürlü erişim token'ı. Yetki geri alınmışsa <see cref="GoogleApiException"/> (yetki hatası).</summary>
    Task<string> GetAccessTokenAsync(string refreshToken, CancellationToken cancellationToken);
}
