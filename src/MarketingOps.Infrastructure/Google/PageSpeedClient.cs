using System.Globalization;
using System.Text.Json;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Google;
using MarketingOps.Domain.Metrics;

namespace MarketingOps.Infrastructure.Google;

/// <summary>
/// PageSpeed Insights v5. Gerçek kullanıcı verisi (CrUX 75. yüzdelik) önce sayfa düzeyinde, yoksa site geneli (origin) olarak alınır;
/// ikisi de yoksa Lighthouse laboratuvar ölçümü kullanılır (INP labda ölçülmez).
/// </summary>
public sealed class PageSpeedClient(HttpClient http, GoogleOptions options) : IPageSpeedClient
{
    public async Task<PageSpeedResult> RunAsync(string url, DeviceStrategy strategy, CancellationToken cancellationToken)
    {
        var query = $"https://www.googleapis.com/pagespeedonline/v5/runPagespeed?url={Uri.EscapeDataString(url)}&strategy={strategy.ToString().ToLowerInvariant()}&category=performance"
                    + (string.IsNullOrWhiteSpace(options.PageSpeedApiKey) ? "" : $"&key={Uri.EscapeDataString(options.PageSpeedApiKey)}");
        var json = await GoogleHttp.SendAsync(http, HttpMethod.Get, query, null, null, cancellationToken);

        if (Field(json, "loadingExperience") is { } page && page.Source(VitalsSource.UrlField) is { } fromPage) return fromPage;
        if (Field(json, "originLoadingExperience") is { } origin && origin.Source(VitalsSource.OriginField) is { } fromOrigin) return fromOrigin;
        return Lab(json);
    }

    private static FieldMetrics? Field(JsonElement json, string name) =>
        json.TryGetProperty(name, out var experience) && experience.TryGetProperty("metrics", out var metrics) ? new FieldMetrics(metrics) : null;

    private static PageSpeedResult Lab(JsonElement json)
    {
        if (!json.TryGetProperty("lighthouseResult", out var lighthouse)) throw new GoogleApiException("PageSpeed ölçüm sonucu dönmedi.");
        var audits = lighthouse.GetProperty("audits");
        double? Audit(string id) => audits.TryGetProperty(id, out var a) && a.TryGetProperty("numericValue", out var v) ? v.GetDouble() : null;
        return new PageSpeedResult(VitalsSource.Lab, Round(Audit("largest-contentful-paint")), null, Audit("cumulative-layout-shift"), Round(Audit("server-response-time")));
    }

    private static int? Round(double? value) => value is { } v ? (int)Math.Round(v) : null;

    private sealed class FieldMetrics(JsonElement metrics)
    {
        public PageSpeedResult? Source(VitalsSource source)
        {
            var lcp = Percentile("LARGEST_CONTENTFUL_PAINT_MS");
            var inp = Percentile("INTERACTION_TO_NEXT_PAINT");
            var cls = Percentile("CUMULATIVE_LAYOUT_SHIFT_SCORE");
            var ttfb = Percentile("EXPERIMENTAL_TIME_TO_FIRST_BYTE");
            if (lcp is null && inp is null && cls is null) return null;
            // CrUX CLS'i 100 ile çarpılmış tam sayı olarak verir.
            return new PageSpeedResult(source, Round(lcp), Round(inp), cls / 100, Round(ttfb));
        }

        private double? Percentile(string name) =>
            metrics.TryGetProperty(name, out var metric) && metric.TryGetProperty("percentile", out var p) ? p.GetDouble() : null;
    }
}
