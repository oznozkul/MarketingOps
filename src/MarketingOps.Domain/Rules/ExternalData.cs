using MarketingOps.Domain.Rendering;

namespace MarketingOps.Domain.Rules;

/// <summary>
/// Taramadan başka kaynaklardan gelen veri. Anahtarlar normalize URL'dir (<see cref="Definitions.UrlKey"/>).
/// <see cref="Available"/>: güncel verisi olan kaynaklar; olmayan kaynağın kuralları çalıştırılmaz.
/// <see cref="Renders"/>: değerlendirilen taramanın render ölçümleri.
/// </summary>
public sealed record ExternalData(
    IReadOnlySet<RuleDataSource> Available,
    IReadOnlyDictionary<string, SearchMetrics> SearchByUrl,
    IReadOnlyDictionary<string, IndexInfo> IndexByUrl,
    IReadOnlyList<VitalsSample> Vitals,
    IReadOnlyList<RenderSample> Renders)
{
    public static ExternalData CrawlOnly { get; } = new(
        new HashSet<RuleDataSource> { RuleDataSource.Crawl },
        new Dictionary<string, SearchMetrics>(),
        new Dictionary<string, IndexInfo>(),
        [],
        []);
}
