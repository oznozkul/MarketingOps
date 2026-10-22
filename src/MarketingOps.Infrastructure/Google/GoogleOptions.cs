namespace MarketingOps.Infrastructure.Google;

/// <summary>
/// "Google" yapılandırma bölümü. ClientId/ClientSecret, MarketingOps'un Google Cloud'daki OAuth istemcisidir (tüm müşteriler için tek);
/// kullanıcılar bununla kendi Google hesaplarıyla yetki verir. Boşsa Google bağlantısı başlatılamaz.
/// PageSpeedApiKey isteğe bağlıdır; yoksa PageSpeed düşük anonim kotayla çalışır.
/// </summary>
public sealed class GoogleOptions
{
    public const string Section = "Google";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? PageSpeedApiKey { get; set; }
}
