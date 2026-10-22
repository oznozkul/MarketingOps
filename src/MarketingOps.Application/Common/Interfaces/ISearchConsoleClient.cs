using MarketingOps.Application.Connectors.Google;

namespace MarketingOps.Application.Common.Interfaces;

public interface ISearchConsoleClient
{
    Task<IReadOnlyList<SearchConsoleSite>> ListSitesAsync(string accessToken, CancellationToken cancellationToken);

    /// <summary>Sayfa ve gün bazında performans; tüm satırlar sayfalanarak çekilir.</summary>
    Task<IReadOnlyList<SearchPerformanceRow>> QueryPagesAsync(string accessToken, string siteUrl, DateOnly start, DateOnly end, CancellationToken cancellationToken);

    Task<IReadOnlyList<SitemapInfo>> ListSitemapsAsync(string accessToken, string siteUrl, CancellationToken cancellationToken);

    Task<UrlInspectionInfo> InspectUrlAsync(string accessToken, string siteUrl, string url, CancellationToken cancellationToken);
}
