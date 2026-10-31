namespace MarketingOps.Domain.Rules;

/// <summary>
/// Kuralın verisini nereden aldığı. Taramadan başka kaynak isteyen kural, o kaynağın güncel verisi yoksa çalıştırılmaz;
/// böylece bağlantı kopunca mevcut bulgular "sorun görülmedi" diye yanlışlıkla çözülmez.
/// </summary>
public enum RuleDataSource
{
    Crawl,
    /// <summary>Taramanın tamamını kapsayan kurallar (yetim sayfa); yalnızca tarama tamamlanmış ve sayfa limiti aşılmamışsa bulunur.</summary>
    FullCrawl,
    SearchConsole,
    UrlInspection,
    PageSpeed,
    Tracking,
    /// <summary>Taramanın örnek sayfalarının tarayıcıda ölçümü (Faz 8, render denetimi).</summary>
    Render
}
