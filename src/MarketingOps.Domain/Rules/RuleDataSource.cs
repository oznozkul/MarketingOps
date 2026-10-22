namespace MarketingOps.Domain.Rules;

/// <summary>
/// Kuralın verisini nereden aldığı. Taramadan başka kaynak isteyen kural, o kaynağın güncel verisi yoksa çalıştırılmaz;
/// böylece bağlantı kopunca mevcut bulgular "sorun görülmedi" diye yanlışlıkla çözülmez.
/// </summary>
public enum RuleDataSource
{
    Crawl,
    SearchConsole,
    UrlInspection,
    PageSpeed
}
