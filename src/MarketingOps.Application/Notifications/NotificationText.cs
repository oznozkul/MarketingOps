using MarketingOps.Domain.Notifications;
using MarketingOps.Domain.Scores;

namespace MarketingOps.Application.Notifications;

public static class NotificationText
{
    public static (string Title, string Body) Compose(NotificationEventType eventType, string? findingTitle, int? magnitude, string? detail) => eventType switch
    {
        NotificationEventType.NewCriticalFinding => (findingTitle ?? "Yeni kritik bulgu", "Yeni bir kritik bulgu açıldı."),
        NotificationEventType.FindingReopened => (findingTitle ?? "Bulgu tekrar açıldı", "Çözülmüş bir bulgu yeniden görüldü."),
        NotificationEventType.PurchaseGap => (findingTitle ?? "Purchase farkı eşiği aştı", "GA4 purchase sayısı ile sipariş sayısı arasındaki fark eşiği aştı."),
        NotificationEventType.ScoreDropped => ($"{CategoryName(detail)} skoru {magnitude} puan düştü", "Son değerlendirmede skor bir önceki güne göre düştü."),
        NotificationEventType.CrawlFailed => ($"Tarama #{detail} başarısız", "Tarama hata ile kapandı. Ayrıntı tarama kaydında."),
        _ => ($"{ConnectorName(detail)} yetkisi doldu", "Bağlantı yeniden kurulmadan veri okunamaz.")
    };

    public static string CategoryName(string? value) => Enum.TryParse<ScoreCategory>(value, out var category) ? category switch
    {
        ScoreCategory.Tracking => "Tracking",
        ScoreCategory.Seo => "SEO",
        ScoreCategory.Performance => "Performans",
        _ => "Erişilebilirlik ve mobil"
    } : "Skor";

    public static string ConnectorName(string? value) => value switch
    {
        "SearchConsole" => "Search Console",
        "Ga4" => "GA4",
        "GoogleAds" => "Google Ads",
        "Meta" => "Meta",
        _ => value ?? "Bağlantı"
    };

    public static string Href(Guid siteId, NotificationEventType eventType, Guid? findingId) => eventType switch
    {
        NotificationEventType.CrawlFailed => $"/sites/{siteId}/crawls",
        NotificationEventType.ConnectorAuthorizationExpired => $"/sites/{siteId}/settings/sites#baglantilar",
        NotificationEventType.ScoreDropped => $"/sites/{siteId}/overview",
        _ when findingId is Guid id => $"/sites/{siteId}/findings/{id}",
        _ => $"/sites/{siteId}/overview"
    };
}
