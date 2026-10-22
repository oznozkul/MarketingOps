using System.Globalization;
using System.Text.Json;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Google;

namespace MarketingOps.Infrastructure.Google;

/// <summary>Search Console API (webmasters v3 ve URL Inspection v1).</summary>
public sealed class SearchConsoleClient(HttpClient http) : ISearchConsoleClient
{
    private const string Base = "https://www.googleapis.com/webmasters/v3";
    private const int RowLimit = 25000;

    public async Task<IReadOnlyList<SearchConsoleSite>> ListSitesAsync(string accessToken, CancellationToken cancellationToken)
    {
        var json = await GoogleHttp.SendAsync(http, HttpMethod.Get, $"{Base}/sites", accessToken, null, cancellationToken);
        return json.Array("siteEntry").Select(s => new SearchConsoleSite(s.String("siteUrl")!, s.String("permissionLevel") ?? "")).ToList();
    }

    public async Task<IReadOnlyList<SearchPerformanceRow>> QueryPagesAsync(string accessToken, string siteUrl, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        var rows = new List<SearchPerformanceRow>();
        for (var startRow = 0; ; startRow += RowLimit)
        {
            var json = await GoogleHttp.SendAsync(http, HttpMethod.Post, $"{Base}/sites/{Uri.EscapeDataString(siteUrl)}/searchAnalytics/query", accessToken, new
            {
                startDate = start.ToString("yyyy-MM-dd"), endDate = end.ToString("yyyy-MM-dd"), dimensions = new[] { "date", "page" }, rowLimit = RowLimit, startRow
            }, cancellationToken);
            var page = json.Array("rows").Select(r =>
            {
                var keys = r.GetProperty("keys");
                return new SearchPerformanceRow(DateOnly.ParseExact(keys[0].GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture), keys[1].GetString()!,
                    (int)r.GetProperty("clicks").GetDouble(), (int)r.GetProperty("impressions").GetDouble(), r.GetProperty("ctr").GetDouble(), r.GetProperty("position").GetDouble());
            }).ToList();
            rows.AddRange(page);
            if (page.Count < RowLimit) return rows;
        }
    }

    public async Task<IReadOnlyList<SitemapInfo>> ListSitemapsAsync(string accessToken, string siteUrl, CancellationToken cancellationToken)
    {
        var json = await GoogleHttp.SendAsync(http, HttpMethod.Get, $"{Base}/sites/{Uri.EscapeDataString(siteUrl)}/sitemaps", accessToken, null, cancellationToken);
        return json.Array("sitemap").Select(s =>
        {
            var contents = s.Array("contents").ToList();
            return new SitemapInfo(s.String("path")!, Date(s.String("lastSubmitted")), Date(s.String("lastDownloaded")),
                s.TryGetProperty("isPending", out var pending) && pending.GetBoolean(), Int(s.String("errors")), Int(s.String("warnings")),
                contents.Count == 0 ? null : contents.Sum(c => Long(c.String("submitted"))),
                // "indexed" alanı Google tarafından kullanımdan kaldırıldı ve hep 0 döner; dizin durumu URL Inspection'dan gelir.
                null);
        }).ToList();
    }

    public async Task<UrlInspectionInfo> InspectUrlAsync(string accessToken, string siteUrl, string url, CancellationToken cancellationToken)
    {
        var json = await GoogleHttp.SendAsync(http, HttpMethod.Post, "https://searchconsole.googleapis.com/v1/urlInspection/index:inspect", accessToken,
            new { inspectionUrl = url, siteUrl, languageCode = "tr-TR" }, cancellationToken);
        var status = json.GetProperty("inspectionResult").GetProperty("indexStatusResult");
        return new UrlInspectionInfo(status.String("verdict") ?? "VERDICT_UNSPECIFIED", status.String("coverageState"), status.String("indexingState"),
            Date(status.String("lastCrawlTime")));
    }

    private static DateTimeOffset? Date(string? value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d) ? d : null;
    private static int Int(string? value) => int.TryParse(value, CultureInfo.InvariantCulture, out var n) ? n : 0;
    private static long Long(string? value) => long.TryParse(value, CultureInfo.InvariantCulture, out var n) ? n : 0;
}
