namespace MarketingOps.Application.Connectors;

/// <summary>"Google" yapılandırma bölümündeki senkron sınırları.</summary>
public sealed class GoogleSyncOptions
{
    /// <summary>Site başına günlük URL Inspection sayısı (Google kotası mülk başına günde 2.000).</summary>
    public int UrlInspectionDailyLimit { get; set; } = 100;

    /// <summary>Bir URL bu kadar gün geçmeden yeniden denetlenmez.</summary>
    public int UrlInspectionRefreshDays { get; set; } = 7;

    /// <summary>Web Vitals için sayfa tipi başına ölçülecek örnek sayfa sayısı.</summary>
    public int WebVitalsSamplesPerPageType { get; set; } = 2;
}
