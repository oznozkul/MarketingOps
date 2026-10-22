using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Connectors;

/// <summary>
/// Bir sitenin dış sisteme bağlantısı (Search Console, GA4, GTM...). Kimlik bilgisi (refresh token)
/// altyapıda şifrelenir; domain yalnızca şifreli değeri taşır. Site başına her türden tek bağlantı.
/// Google bağlantılarında yetki verildikten sonra mülk seçilene kadar durum <see cref="ConnectorStatus.PendingApproval"/>'dır.
/// </summary>
public class ConnectorAccount : Entity
{
    public const int ErrorMaxLength = 1000;

    private List<string> _scopes = [];

    public Guid SiteId { get; private set; }
    public ConnectorType Type { get; private set; }
    public ConnectorStatus Status { get; private set; }

    /// <summary>Seçili mülk: Search Console'da site URL'i ("sc-domain:ornek.com"), GA4'te "properties/123".</summary>
    public string? ExternalAccountId { get; private set; }
    public string? ExternalAccountName { get; private set; }

    /// <summary>Yetkiyi veren hesap (Google e-postası).</summary>
    public string? AccountEmail { get; private set; }
    public IReadOnlyList<string> Scopes => _scopes;

    public string? ProtectedSecret { get; private set; }
    public DateTimeOffset? LastSyncAt { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool HasSecret => ProtectedSecret is not null;

    private ConnectorAccount() { }

    public static ConnectorAccount Create(Guid siteId, ConnectorType type, DateTimeOffset now) =>
        new() { SiteId = siteId, Type = type, Status = ConnectorStatus.NotConnected, UpdatedAt = now };

    /// <summary>OAuth yetkisi alındı. Mülk gerektiren bağlantılar mülk seçilene kadar beklemede kalır.</summary>
    public void Authorize(string protectedSecret, string accountEmail, IEnumerable<string> scopes, bool requiresProperty, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(protectedSecret)) throw new DomainException("Yetki bilgisi boş olamaz.");
        // Başka bir Google hesabıyla yetki verildiyse eski hesabın mülkü bu hesapta olmayabilir; seçim sıfırlanır.
        if (AccountEmail is not null && !string.Equals(AccountEmail, accountEmail, StringComparison.OrdinalIgnoreCase))
        {
            ExternalAccountId = null;
            ExternalAccountName = null;
        }
        ProtectedSecret = protectedSecret;
        AccountEmail = accountEmail;
        _scopes = [.. scopes];
        // Aynı hesapla yeniden yetkilendirmede seçili mülk korunur.
        Status = requiresProperty && ExternalAccountId is null ? ConnectorStatus.PendingApproval : ConnectorStatus.Connected;
        LastError = null;
        UpdatedAt = now;
    }

    public void SelectProperty(string externalId, string displayName, DateTimeOffset now)
    {
        if (ProtectedSecret is null) throw new DomainException("Önce bağlantıya yetki verilmeli.");
        if (string.IsNullOrWhiteSpace(externalId)) throw new DomainException("Mülk seçilmeli.");
        ExternalAccountId = externalId;
        ExternalAccountName = displayName;
        Status = ConnectorStatus.Connected;
        LastError = null;
        UpdatedAt = now;
    }

    /// <summary>Mülk seçimini kaldırır; yetki kalır ve bağlantı yeniden mülk seçimi bekler.</summary>
    public void ClearProperty(DateTimeOffset now)
    {
        if (ExternalAccountId is null) return;
        ExternalAccountId = null;
        ExternalAccountName = null;
        Status = ProtectedSecret is null ? ConnectorStatus.NotConnected : ConnectorStatus.PendingApproval;
        LastError = null;
        UpdatedAt = now;
    }

    /// <summary>Meta gibi token ile bağlanan salt okunur kaynak. Mülk seçimi yoktur.</summary>
    public void ConnectToken(string protectedSecret, string? externalId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(protectedSecret)) throw new DomainException("Yetki bilgisi boş olamaz.");
        ProtectedSecret = protectedSecret;
        ExternalAccountId = string.IsNullOrWhiteSpace(externalId) ? null : externalId.Trim();
        Status = ConnectorStatus.Connected;
        LastError = null;
        UpdatedAt = now;
    }

    public void Disconnect(DateTimeOffset now)
    {
        Status = ConnectorStatus.NotConnected;
        ProtectedSecret = null;
        ExternalAccountId = null;
        ExternalAccountName = null;
        AccountEmail = null;
        _scopes = [];
        LastError = null;
        UpdatedAt = now;
    }

    public void RecordSync(DateTimeOffset now)
    {
        LastSyncAt = now;
        if (Status is ConnectorStatus.Error or ConnectorStatus.Expired) Status = ConnectorStatus.Connected;
        LastError = null;
        UpdatedAt = now;
    }

    /// <summary>Senkron ya da test başarısız. Yetki geri alınmış/süresi dolmuşsa yeniden bağlanmak gerekir.</summary>
    public void RecordFailure(string error, bool authorizationExpired, DateTimeOffset now)
    {
        Status = authorizationExpired ? ConnectorStatus.Expired : ConnectorStatus.Error;
        LastError = error.Length > ErrorMaxLength ? error[..ErrorMaxLength] : error;
        UpdatedAt = now;
        if (authorizationExpired) Raise(new ConnectorAuthorizationExpired(Id, SiteId, Type));
    }

    public void RecordSuccessfulTest(DateTimeOffset now)
    {
        if (Status is ConnectorStatus.Error or ConnectorStatus.Expired)
            Status = ExternalAccountId is null && Type is ConnectorType.SearchConsole or ConnectorType.Ga4 ? ConnectorStatus.PendingApproval : ConnectorStatus.Connected;
        LastError = null;
        UpdatedAt = now;
    }
}
