using System.Threading.RateLimiting;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Sites;
using MarketingOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Infrastructure.Crawling;

/// <summary>
/// Bir taramayı baştan sona yürütür: robots.txt ve sitemap ile keşif, hız limitli indirme, ayrıştırma, sayfa tipi atama.
/// Snapshot'lar toplu yazılır; her yazımda ayrı, kısa ömürlü bir DbContext kullanılır ki change tracker şişmesin.
/// İptal, tarama kaydının durumu her yazımda okunarak fark edilir.
/// </summary>
public sealed class SiteCrawler(IServiceScopeFactory scopes, ICrawlerHttpClient http, ICrawlScheduler scheduler, TimeProvider clock, ILogger<SiteCrawler> logger)
{
    private const int MaxRedirects = 5;
    private const int BatchSize = 50;
    private const int MaxSitemaps = 50;
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromSeconds(2);

    public async Task RunAsync(Guid crawlId, CancellationToken ct)
    {
        Site site;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var crawl = await db.Crawls.FirstOrDefaultAsync(c => c.Id == crawlId, ct);
            // Kuyrukta beklerken iptal edilmiş ya da silinmiş olabilir.
            if (crawl is null || crawl.Status != CrawlStatus.Queued) return;

            site = await db.Sites.AsNoTracking().Include(s => s.PageTypes).FirstAsync(s => s.Id == crawl.SiteId, ct);
            crawl.Start(clock.GetUtcNow());
            crawl.EnterStage(CrawlStage.Discovery, clock.GetUtcNow());
            await db.SaveChangesAsync(ct);
        }

        var state = new CrawlState(site);
        try
        {
            var robots = await DiscoverAsync(state, ct);
            if (!await UpdateCrawlAsync(crawlId, state, c => c.EnterStage(CrawlStage.Download, clock.GetUtcNow()), ct)) return;
            await DownloadAsync(crawlId, state, robots, ct);
            var assets = await CheckAssetsAsync(state, ct);
            var finished = await UpdateCrawlAsync(crawlId, state, c =>
            {
                foreach (var asset in assets) c.RecordAssetCheck(asset);
                c.Finish(clock.GetUtcNow());
            }, ct);
            if (finished) scheduler.EnqueueEvaluation(crawlId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Tarama başarısız: {CrawlId}", crawlId);
            var message = ct.IsCancellationRequested ? "Tarama, işçi süreci kapanırken yarıda kaldı." : ex.Message;
            // İşçi kapanırken ct iptal edilmiş olabilir; son durumu yine de yaz.
            await UpdateCrawlAsync(crawlId, state, c => c.Fail(message, clock.GetUtcNow()), CancellationToken.None);
        }
    }

    private async Task<RobotsRules> DiscoverAsync(CrawlState state, CancellationToken ct)
    {
        var robots = RobotsRules.AllowAll;
        var robotsResult = await FetchFollowingRedirectsAsync(new Uri(state.Origin, "/robots.txt"), state, ct);
        if (robotsResult.Response is { StatusCode: >= 200 and < 300, Body: { } text })
            robots = RobotsRules.Parse(text, CrawlerHttpClient.UserAgent);
        state.Robots = robots;

        state.Enqueue(state.Origin);

        var pending = new Queue<Uri>(robots.Sitemaps.Select(s => Uri.TryCreate(s, UriKind.Absolute, out var u) ? u : null).OfType<Uri>());
        if (pending.Count == 0) pending.Enqueue(new Uri(state.Origin, "/sitemap.xml"));
        var seenSitemaps = new HashSet<string>();

        while (pending.Count > 0 && seenSitemaps.Count < MaxSitemaps && !state.IsFull)
        {
            var sitemapUrl = pending.Dequeue();
            if (!seenSitemaps.Add(sitemapUrl.AbsoluteUri)) continue;
            var result = await FetchFollowingRedirectsAsync(sitemapUrl, state, ct);
            if (result.Response is not { StatusCode: >= 200 and < 300, Body: { } xml }) continue;

            var content = SitemapParser.Parse(xml);
            foreach (var child in content.Sitemaps)
                if (Uri.TryCreate(child, UriKind.Absolute, out var u)) pending.Enqueue(u);
            foreach (var pageUrl in content.Urls)
                if (Uri.TryCreate(pageUrl, UriKind.Absolute, out var u))
                {
                    state.MarkInSitemap(u);
                    state.Enqueue(u);
                }
        }
        return robots;
    }

    private async Task DownloadAsync(Guid crawlId, CrawlState state, RobotsRules robots, CancellationToken ct)
    {
        var settings = state.Site.CrawlSettings;
        using var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = settings.MaxRequestsPerSecond,
            TokensPerPeriod = settings.MaxRequestsPerSecond,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            QueueLimit = int.MaxValue,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
        var parallelism = Math.Clamp(settings.MaxRequestsPerSecond, 1, 4);
        var batch = new List<PageSnapshot>();
        var lastFlush = clock.GetUtcNow();

        while (state.Frontier.Count > 0)
        {
            var wave = new List<Uri>();
            while (wave.Count < parallelism && state.Frontier.TryDequeue(out var next)) wave.Add(next);

            var results = await Task.WhenAll(wave.Select(async url =>
            {
                using var lease = await limiter.AcquireAsync(1, ct);
                return (Url: url, Result: await FetchFollowingRedirectsAsync(url, state, ct));
            }));

            foreach (var (url, result) in results)
                batch.Add(BuildSnapshot(crawlId, url, result, state));

            if (batch.Count >= BatchSize || clock.GetUtcNow() - lastFlush >= ProgressInterval || state.Frontier.Count == 0)
            {
                if (!await FlushAsync(crawlId, state, batch, ct)) return; // iptal edildi
                batch.Clear();
                lastFlush = clock.GetUtcNow();
            }
        }
    }

    private PageSnapshot BuildSnapshot(Guid crawlId, Uri url, FetchResult result, CrawlState state)
    {
        var normalized = UrlNormalizer.Normalize(url);
        var now = clock.GetUtcNow();
        if (result.Response is null)
        {
            state.Failed++;
            return PageSnapshot.Create(crawlId, state.Site.Id, url.AbsoluteUri, normalized, null, null, result.Chain, null,
                null, null, null, null, result.Error ?? "Yanıt alınamadı.", now, PageSnapshotData.Empty, state.IsInSitemap(normalized));
        }

        var response = result.Response;
        var data = PageSnapshotData.Empty;
        if (result.Chain.Count > 0)
        {
            // Yönlenen adres içerik taşımaz: içerik hedef sayfaya aittir ve hedef kendi snapshot'ıyla bir kez taranır.
            // Aksi halde aynı sayfa iki kez sayılır, kurallar (ör. title tekrarı) sahte bulgu üretir.
            state.Enqueue(result.FinalUrl);
        }
        else if (response.StatusCode is >= 200 and < 300 && response.Body is { } html && IsHtml(response.ContentType))
        {
            var parsed = HtmlSnapshotParser.Parse(html, url, state.IsInternal);
            data = parsed.Data;
            foreach (var link in parsed.InternalLinks) state.Enqueue(link);
            foreach (var script in data.Scripts) state.AddAsset(script.Src);
            foreach (var stylesheet in data.Stylesheets) state.AddAsset(stylesheet);
        }

        if (response.StatusCode >= 400) state.Failed++;
        else state.Crawled++;

        // Sayfa tipi, istenen adrese göre atanır (yönlenen adres hedefin tipini almaz).
        var pageType = state.Site.PageTypes.OrderBy(p => p.Id).FirstOrDefault(p => p.Matches(url, data.SchemaTypes));
        return PageSnapshot.Create(crawlId, state.Site.Id, url.AbsoluteUri, normalized, pageType?.Id, response.StatusCode, result.Chain,
            result.ResponseTimeMs, response.ContentType, response.ContentEncoding, response.CacheControl, response.BodySizeBytes,
            null, now, data, state.IsInSitemap(normalized));
    }

    /// <summary>
    /// Örneklenen CSS/JS dosyalarının sıkıştırma ve önbellek başlıkları. Sayfalardan sonra, sitenin hız limitine
    /// uyularak sırayla istenir; başarısız istek de (durum kodu boş) kaydedilir.
    /// </summary>
    private async Task<IReadOnlyList<AssetCheck>> CheckAssetsAsync(CrawlState state, CancellationToken ct)
    {
        var delay = TimeSpan.FromSeconds(1.0 / state.Site.CrawlSettings.MaxRequestsPerSecond);
        var checks = new List<AssetCheck>();
        foreach (var asset in state.Assets)
        {
            try
            {
                var response = await http.GetAsync(asset, ct);
                checks.Add(new AssetCheck(asset.AbsoluteUri, response.StatusCode, response.ContentType, response.CacheControl, response.ContentEncoding));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                checks.Add(new AssetCheck(asset.AbsoluteUri, null, null, null, null));
            }
            await Task.Delay(delay, clock, ct);
        }
        return checks;
    }

    private static bool IsHtml(string? contentType) =>
        contentType is null || contentType.Contains("html", StringComparison.OrdinalIgnoreCase);

    /// <summary>Snapshot'ları yazar, ilerlemeyi günceller. Tarama iptal edildiyse false döner.</summary>
    private async Task<bool> FlushAsync(Guid crawlId, CrawlState state, List<PageSnapshot> batch, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var crawl = await db.Crawls.FirstOrDefaultAsync(c => c.Id == crawlId, ct);
        if (crawl is null || crawl.Status != CrawlStatus.Running) return false;

        db.PageSnapshots.AddRange(batch);
        ApplyProgress(crawl, state);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<bool> UpdateCrawlAsync(Guid crawlId, CrawlState state, Action<Crawl> change, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var crawl = await db.Crawls.FirstOrDefaultAsync(c => c.Id == crawlId, ct);
        if (crawl is null || crawl.Status != CrawlStatus.Running) return false;

        ApplyProgress(crawl, state);
        change(crawl);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static void ApplyProgress(Crawl crawl, CrawlState state)
    {
        foreach (var skipped in state.TakePendingSkipped()) crawl.RecordSkipped(skipped.Url, skipped.Reason);
        crawl.ReportProgress(state.Discovered, state.Crawled, state.Failed);
    }

    private async Task<FetchResult> FetchFollowingRedirectsAsync(Uri url, CrawlState state, CancellationToken ct)
    {
        var chain = new List<RedirectHop>();
        var current = url;
        for (var hop = 0; ; hop++)
        {
            CrawlerResponse response;
            try
            {
                response = await http.GetAsync(current, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                return new FetchResult(null, chain, current, null, ex is TaskCanceledException ? "Zaman aşımı." : ex.Message);
            }

            var isRedirect = response.StatusCode is >= 300 and < 400 && response.Location is not null;
            if (!isRedirect || hop >= MaxRedirects || !state.IsInternal(response.Location!))
                return new FetchResult(response, chain, current, response.ResponseTimeMs, null);

            chain.Add(new RedirectHop(current.AbsoluteUri, response.StatusCode));
            current = response.Location!;
        }
    }

    private sealed record FetchResult(CrawlerResponse? Response, List<RedirectHop> Chain, Uri FinalUrl, int? ResponseTimeMs, string? Error);
}
