using MarketingOps.Application.Connectors.Google;
using Microsoft.AspNetCore.DataProtection;

namespace MarketingOps.Application.Connectors;

/// <summary>
/// Connector kimlik bilgileri veritabanına Data Protection ile şifreli yazılır. OAuth "state" değeri de buradan üretilir:
/// hangi siteye bağlandığını taşır, 15 dakika geçerlidir ve sahte dönüşleri reddeder.
/// </summary>
public sealed class ConnectorSecretProtector(IDataProtectionProvider provider, TimeProvider clock)
{
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(15);
    private readonly IDataProtector _secrets = provider.CreateProtector("MarketingOps.Connectors.Secret.v1");
    private readonly IDataProtector _state = provider.CreateProtector("MarketingOps.Connectors.GoogleState.v1");

    public string Protect(string secret) => _secrets.Protect(secret);

    public string Unprotect(string protectedSecret) => _secrets.Unprotect(protectedSecret);

    /// <summary>Site, tek kullanımlık rastgele değer ve son geçerlilik zamanı şifreli olarak taşınır.</summary>
    public string CreateState(Guid siteId) =>
        _state.Protect($"{siteId:N}|{Guid.NewGuid():N}|{clock.GetUtcNow().Add(StateLifetime).ToUnixTimeSeconds()}");

    public Guid ReadState(string state)
    {
        try
        {
            var parts = _state.Unprotect(state).Split('|');
            if (DateTimeOffset.FromUnixTimeSeconds(long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)) < clock.GetUtcNow())
                throw new FormatException("Süresi dolmuş state.");
            return Guid.ParseExact(parts[0], "N");
        }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or FormatException or IndexOutOfRangeException)
        {
            throw new GoogleApiException("Google dönüşü doğrulanamadı ya da süresi doldu. Bağlamayı yeniden başlatın.", inner: ex);
        }
    }
}
