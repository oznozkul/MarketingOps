using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Google;
using MarketingOps.Domain.Metrics;

namespace MarketingOps.IntegrationTests.Connectors;

/// <summary>
/// Google yerine: OAuth, Search Console, GA4 ve PageSpeed. Sahte sitenin /urun/p1 sayfası 14. sırada ve çok gösterimli,
/// /urun/p2 dizinde değil; mobil LCP tüm örneklerde zayıf.
/// </summary>
public sealed class FakeGoogle(string domain, TimeProvider clock) : IGoogleAuthClient, ISearchConsoleClient, IGa4Client, IPageSpeedClient
{
    public const string Email = "pazarlama@ornek.com";
    public string Ga4PropertyId => "properties/4242";
    public string SiteProperty => $"sc-domain:{domain}";
    public int InspectionCount { get; private set; }

    public bool IsConfigured => true;

    public string BuildAuthorizationUrl(string state, string redirectUri) => $"https://accounts.google.test/auth?state={Uri.EscapeDataString(state)}";

    public Task<GoogleAuthorization> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken) =>
        Task.FromResult(new GoogleAuthorization("refresh-" + code, "access", Email, GoogleConnectors.Scopes));

    public Task<string> GetAccessTokenAsync(string refreshToken, CancellationToken cancellationToken) =>
        refreshToken.StartsWith("refresh-", StringComparison.Ordinal)
            ? Task.FromResult("access")
            : throw new GoogleApiException("geçersiz", isAuthorizationError: true);

    public Task<IReadOnlyList<SearchConsoleSite>> ListSitesAsync(string accessToken, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SearchConsoleSite>>([new(SiteProperty, "siteOwner"), new("https://baska-site.com/", "siteFullUser")]);

    public Task<IReadOnlyList<SearchPerformanceRow>> QueryPagesAsync(string accessToken, string siteUrl, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        var rows = new List<SearchPerformanceRow>();
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            rows.Add(new(day, $"https://{domain}/urun/p1", 1, 30, 0.033, 14));
            rows.Add(new(day, $"https://{domain}/kategori/yaz", 20, 200, 0.1, 3));
        }
        return Task.FromResult<IReadOnlyList<SearchPerformanceRow>>(rows);
    }

    public Task<IReadOnlyList<SitemapInfo>> ListSitemapsAsync(string accessToken, string siteUrl, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SitemapInfo>>([new($"https://{domain}/sitemap.xml", clock.GetUtcNow().AddDays(-10), clock.GetUtcNow().AddDays(-1), false, 0, 1, 12, 11)]);

    public Task<UrlInspectionInfo> InspectUrlAsync(string accessToken, string siteUrl, string url, CancellationToken cancellationToken)
    {
        InspectionCount++;
        return Task.FromResult(url.EndsWith("/urun/p2", StringComparison.Ordinal)
            ? new UrlInspectionInfo("NEUTRAL", "Tarandı - şu anda dizine eklenmemiş", "INDEXING_ALLOWED", null)
            : new UrlInspectionInfo("PASS", "Gönderildi ve dizine eklendi", "INDEXING_ALLOWED", clock.GetUtcNow().AddDays(-2)));
    }

    public Task<IReadOnlyList<Ga4Property>> ListPropertiesAsync(string accessToken, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Ga4Property>>([new(Ga4PropertyId, "Web", "Örnek Mağaza")]);

    public Task<IReadOnlyList<Ga4EventRow>> RunEventReportAsync(string accessToken, string propertyId, DateOnly start, DateOnly end,
        IReadOnlyList<string> eventNames, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Ga4EventRow>>([new(end, "purchase", 41, 38250.5m), new(end, "add_to_cart", 300, 0)]);

    public Task<PageSpeedResult> RunAsync(string url, DeviceStrategy strategy, CancellationToken cancellationToken) =>
        Task.FromResult(strategy == DeviceStrategy.Mobile
            ? new PageSpeedResult(VitalsSource.UrlField, 4200, 150, 0.04, 700)
            : new PageSpeedResult(VitalsSource.OriginField, 1800, 80, 0.01, 300));
}
