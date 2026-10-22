namespace MarketingOps.Domain.Crawls;

/// <summary>Taramanın aşamaları. Render, kural değerlendirme ve connector senkronu sonraki fazlarda eklenecek.</summary>
public enum CrawlStage
{
    /// <summary>robots.txt ve sitemap okunur, başlangıç URL'leri belirlenir.</summary>
    Discovery,
    /// <summary>Sayfalar indirilir, ayrıştırılır, iç linkler kuyruğa eklenir.</summary>
    Download,
    /// <summary>JavaScript ile oluşturulan sayfalar tarayıcıda açılır.</summary>
    Render,
    /// <summary>Kurallar çalıştırılır, bulgular güncellenir.</summary>
    RuleEvaluation,
    /// <summary>Bağlı veri kaynaklarından veri okunur.</summary>
    ConnectorSync
}
