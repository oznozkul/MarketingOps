using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Google;
using MarketingOps.Domain.Connectors;

namespace MarketingOps.Application.Connectors;

/// <summary>Bağlantının şifreli refresh token'ından erişim token'ı alır; yetki hatasında bağlantıyı "süresi doldu" işaretler.</summary>
public sealed class GoogleTokenProvider(IGoogleAuthClient auth, ConnectorSecretProtector protector, TimeProvider clock)
{
    public async Task<string> AccessTokenAsync(ConnectorAccount account, CancellationToken ct)
    {
        if (account.ProtectedSecret is null) throw new GoogleApiException("Bağlantının yetkisi yok.", isAuthorizationError: true);
        try
        {
            return await auth.GetAccessTokenAsync(protector.Unprotect(account.ProtectedSecret), ct);
        }
        catch (GoogleApiException ex) when (ex.IsAuthorizationError)
        {
            account.RecordFailure("Google yetkisi geri alınmış ya da süresi dolmuş. Yeniden bağlanın.", authorizationExpired: true, clock.GetUtcNow());
            throw;
        }
    }
}
