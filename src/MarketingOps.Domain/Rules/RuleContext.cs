using MarketingOps.Domain.Sites;

namespace MarketingOps.Domain.Rules;

/// <summary>Kuralların değerlendirme sırasında ihtiyaç duyduğu, sayfadan bağımsız bilgiler.</summary>
public sealed class RuleContext(
    Site site,
    IReadOnlyDictionary<string, decimal> thresholds,
    IReadOnlySet<string> failedUrls,
    IReadOnlyList<AssetCheck> assets,
    ExternalData? external = null)
{
    public Site Site { get; } = site;

    /// <summary>Taramada 4xx/5xx dönen ya da yanıt vermeyen sayfaların normalize URL'leri.</summary>
    public IReadOnlySet<string> FailedUrls { get; } = failedUrls;

    public IReadOnlyList<AssetCheck> Assets { get; } = assets;

    /// <summary>Search Console, URL Inspection ve PageSpeed verisi; bağlantı yoksa boştur.</summary>
    public ExternalData External { get; } = external ?? ExternalData.CrawlOnly;

    public decimal Threshold(RuleThreshold threshold) =>
        thresholds.TryGetValue(threshold.Key, out var value) ? value : threshold.Default;

    public bool IsInternal(Uri url) => url.Host.Equals(Site.Domain, StringComparison.OrdinalIgnoreCase)
                                       || url.Host.Equals("www." + Site.Domain, StringComparison.OrdinalIgnoreCase);
}
