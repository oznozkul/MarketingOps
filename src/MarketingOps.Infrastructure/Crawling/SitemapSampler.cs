using MarketingOps.Application.Common.Interfaces;

namespace MarketingOps.Infrastructure.Crawling;

/// <summary>
/// Sihirbazın sayfa tipi adımı için sitemap okuması. Tarama değildir: yalnızca robots.txt ve sitemap dosyaları indirilir,
/// alt sitemap sayısı ve toplanan adres sayısı sınırlıdır (öneri için örnek yeterli). Her sitemap dosyasından sınırlı
/// sayıda adres alınır; sitemap'i tiplere bölünmüş sitelerde (kategoriler.xml, urunler.xml) her tip temsil edilsin.
/// </summary>
public sealed class SitemapSampler(ICrawlerHttpClient http) : ISitemapSampler
{
    private const int MaxSitemaps = 20;
    private const int MaxUrls = 5000;
    private const int MaxUrlsPerSitemap = 300;
    private const int MaxRedirects = 5;

    public async Task<IReadOnlyList<string>> GetUrlsAsync(string domain, CancellationToken cancellationToken)
    {
        var origin = new Uri($"https://{domain}/");
        var robots = await FetchAsync(new Uri(origin, "/robots.txt"), cancellationToken);
        var sitemaps = robots is null ? [] : RobotsRules.Parse(robots, CrawlerHttpClient.UserAgent).Sitemaps;

        var pending = new Queue<Uri>(sitemaps.Select(s => Uri.TryCreate(s, UriKind.Absolute, out var u) ? u : null).OfType<Uri>());
        if (pending.Count == 0) pending.Enqueue(new Uri(origin, "/sitemap.xml"));
        var seen = new HashSet<string>();
        var urls = new List<string>();
        while (pending.Count > 0 && seen.Count < MaxSitemaps && urls.Count < MaxUrls)
        {
            var sitemap = pending.Dequeue();
            if (!seen.Add(sitemap.AbsoluteUri) || await FetchAsync(sitemap, cancellationToken) is not { } xml) continue;
            var content = SitemapParser.Parse(xml);
            foreach (var child in content.Sitemaps)
                if (Uri.TryCreate(child, UriKind.Absolute, out var u)) pending.Enqueue(u);
            urls.AddRange(content.Urls.Take(Math.Min(MaxUrlsPerSitemap, MaxUrls - urls.Count)));
        }
        return urls;
    }

    /// <summary>Yönlendirmeyi izler (ör. kök → www); başarısız yanıt ve ağ hatası null döner.</summary>
    private async Task<string?> FetchAsync(Uri url, CancellationToken cancellationToken)
    {
        var current = url;
        for (var hop = 0; hop <= MaxRedirects; hop++)
        {
            CrawlerResponse response;
            try
            {
                response = await http.GetAsync(current, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            if (response.StatusCode is >= 300 and < 400 && response.Location is not null)
            {
                current = response.Location;
                continue;
            }
            return response.StatusCode is >= 200 and < 300 ? response.Body : null;
        }
        return null;
    }
}
