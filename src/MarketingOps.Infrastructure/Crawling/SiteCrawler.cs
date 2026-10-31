using System.Collections.Concurrent;
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
/// <list type="bullet">
/// <item>İndirme sürekli çalışan bir işçi havuzudur; hız limiti (istek/sn) ve robots.txt Crawl-delay birlikte uygulanır.
/// Sunucu 429/503 dönerse Retry-After süresince tüm işçiler bekler.</item>
/// <item>Snapshot'lar toplu yazılır; her yazımda kısa ömürlü bir DbContext kullanılır. İptal, tarama kaydı okunarak fark edilir.</item>
/// <item>İşçi süreci kapanırsa tarama "hata" sayılmaz; iş yeniden çalıştığında yazılmış sayfalar ve linkleri
/// veritabanından geri yüklenip kalan sayfalarla devam edilir.</item>
/// </list>
/// </summary>
public sealed class SiteCrawler(IServiceScopeFactory scopes, ICrawlerHttpClient http, ICrawlScheduler scheduler, TimeProvider clock, ILogger<SiteCrawler> logger)
{
    private const int MaxRedirects = 5;
    private const int MaxSitemaps = 1000;
    private const int MaxConcurrency = 30;
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan DefaultBackoff = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    public async Task RunAsync(Guid crawlId, CancellationToken ct)
    {
        Site site;
        bool resuming;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var crawl = await db.Crawls.FirstOrDefaultAsync(c => c.Id == crawlId, ct);
            // Kuyrukta beklerken iptal edilmiş ya da silinmiş olabilir.
            if (crawl is null || crawl.Status is not (CrawlStatus.Queued or CrawlStatus.Running)) return;

            site = await db.Sites.AsNoTracking().Include(s => s.PageTypes).FirstAsync(s => s.Id == crawl.SiteId, ct);
            resuming = crawl.Status == CrawlStatus.Running;
            if (!resuming)
            {
                crawl.Start(clock.GetUtcNow());
                crawl.EnterStage(CrawlStage.Discovery, clock.GetUtcNow());
                await db.SaveChangesAsync(ct);
            }
        }

        var state = new CrawlState(site);
        try
        {
            if (resuming)
            {
                logger.LogInformation("Tarama {CrawlId} kaldığı yerden devam ediyor.", crawlId);
                await RestoreAsync(crawlId, state, ct);
            }
            var robots = await DiscoverAsync(state, ct);
            if (resuming) await EnqueueStoredLinksAsync(crawlId, state, ct);
            else if (!await UpdateCrawlAsync(crawlId, state, c => c.EnterStage(CrawlStage.Download, clock.GetUtcNow()), ct)) return;

            if (!await DownloadAsync(crawlId, state, robots, ct)) return;

            var assets = await CheckAssetsAsync(state, ct);
            var finished = await UpdateCrawlAsync(crawlId, state, c =>
            {
                foreach (var asset in assets) c.RecordAssetCheck(asset);
                c.Finish(clock.GetUtcNow());
            }, ct);
            if (finished)
            {
                if (state.Site.CrawlSettings.RenderEnabled) scheduler.EnqueueRender(crawlId);
                else scheduler.EnqueueEvaluation(crawlId);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // İşçi kapanıyor: tarama "çalışıyor" kalır, iş yeniden çalışınca kaldığı yerden devam eder.
            logger.LogInformation("İşçi kapanıyor; tarama {CrawlId} sonra devam edecek.", crawlId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tarama başarısız: {CrawlId}", crawlId);
            await UpdateCrawlAsync(crawlId, state, c => c.Fail(ex.Message, clock.GetUtcNow()), CancellationToken.None);
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

    /// <summary>
    /// Önceki çalışmanın yazdığı sayfalar "görüldü" sayılır. Sayaçlar da yazılmış sayfalardan hesaplanır: kapanış anında
    /// sayılıp yazılamamış bir sayfa yeniden taranacağı için tarama kaydındaki sayaç kesin değildir.
    /// </summary>
    private async Task RestoreAsync(Guid crawlId, CrawlState state, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pages = await db.PageSnapshots.AsNoTracking().Where(p => p.CrawlId == crawlId)
            .Select(p => new { p.NormalizedUrl, Failed = p.StatusCode == null || p.StatusCode >= 400 }).ToListAsync(ct);
        state.Restore(pages.Select(p => p.NormalizedUrl), pages.Count(p => !p.Failed), pages.Count(p => p.Failed));
    }

    /// <summary>Yazılmış sayfaların iç linkleri yeniden kuyruğa eklenir; sitemap'te olmayan sayfalara da böyle ulaşılır.</summary>
    private async Task EnqueueStoredLinksAsync(Guid crawlId, CrawlState state, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await foreach (var data in db.PageSnapshots.AsNoTracking().Where(p => p.CrawlId == crawlId).Select(p => p.Data).AsAsyncEnumerable().WithCancellation(ct))
        {
            foreach (var link in data.InternalLinks)
                if (Uri.TryCreate(link, UriKind.Absolute, out var u)) state.Enqueue(u);
            if (state.IsFull) break;
        }
    }

    /// <summary>İşçi havuzu. Tarama iptal edildiyse false döner.</summary>
    private async Task<bool> DownloadAsync(Guid crawlId, CrawlState state, RobotsRules robots, CancellationToken ct)
    {
        var settings = state.Site.CrawlSettings;
        // robots.txt daha yavaş bir hız istiyorsa ona uyulur.
        var interval = TimeSpan.FromSeconds(1.0 / settings.MaxRequestsPerSecond);
        if (robots.CrawlDelay is { } delay && delay > interval) interval = delay;
        var perSecond = Math.Max(1, (int)Math.Round(1 / interval.TotalSeconds));
        using var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = perSecond,
            TokensPerPeriod = perSecond,
            ReplenishmentPeriod = interval * perSecond,
            QueueLimit = int.MaxValue,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
        // Hız limiti darboğaz olsun diye eşzamanlılık hızdan türetilir: yavaş yanıt veren sitede de hedef hıza ulaşılır.
        var concurrency = Math.Clamp(perSecond * 3, 2, MaxConcurrency);

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var batch = new ConcurrentQueue<PageSnapshot>();
        // Sunucunun istediği bekleme bitişi (UTC tick); işçiler arasında paylaşılır.
        long pauseUntilTicks = 0;
        var cancelled = false;

        async Task WorkerAsync()
        {
            while (!stop.IsCancellationRequested)
            {
                if (!state.TryTake(out var url))
                {
                    if (state.IsDrained) return;
                    // Kuyruk geçici olarak boş: işlenen sayfalar yeni link ekleyebilir.
                    await Task.Delay(TimeSpan.FromMilliseconds(100), clock, stop.Token);
                    continue;
                }

                var wait = new DateTimeOffset(Interlocked.Read(ref pauseUntilTicks), TimeSpan.Zero) - clock.GetUtcNow();
                if (wait > TimeSpan.Zero) await Task.Delay(wait, clock, stop.Token);

                FetchResult result;
                using (await limiter.AcquireAsync(1, stop.Token))
                    result = await FetchFollowingRedirectsAsync(url, state, stop.Token);

                if (result.Response is { StatusCode: 429 or 503 } busy && state.Requeue(url))
                {
                    var backoff = busy.RetryAfter is { } ra && ra > TimeSpan.Zero ? (ra > MaxBackoff ? MaxBackoff : ra) : DefaultBackoff;
                    Interlocked.Exchange(ref pauseUntilTicks, (clock.GetUtcNow() + backoff).UtcTicks);
                    logger.LogWarning("{Site} yavaşlamamızı istedi ({Status}); {Seconds} sn bekleniyor.", state.Site.Domain, busy.StatusCode, backoff.TotalSeconds);
                    continue;
                }

                // Önce yazma kuyruğuna, sonra sayaca: sayılan her sayfa yazılacak partidedir.
                batch.Enqueue(BuildSnapshot(crawlId, url, result, state));
                state.Complete(failed: result.Response is null || result.Response.StatusCode >= 400);
            }
        }

        var workers = Enumerable.Range(0, concurrency).Select(_ => Task.Run(WorkerAsync, CancellationToken.None)).ToList();
        var all = Task.WhenAll(workers);

        // Yazıcı: ilerleme aralığında birikenleri yazar (ilerleme ekranı da bununla güncellenir); iptal edildiyse işçileri durdurur.
        try
        {
            while (!all.IsCompleted)
            {
                await Task.WhenAny(all, Task.Delay(ProgressInterval, clock, ct));
                if (!await FlushAsync(crawlId, state, Drain(batch), ct))
                {
                    cancelled = true;
                    await stop.CancelAsync();
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // İşçi kapanıyor: indirilmiş ama yazılmamış sayfalar kaybolmasın, devamda yeniden indirilmesin.
            await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(5), clock, CancellationToken.None));
            await FlushAsync(crawlId, state, Drain(batch), CancellationToken.None);
            throw;
        }

        try
        {
            await all;
        }
        catch (OperationCanceledException) when (cancelled && !ct.IsCancellationRequested)
        {
            // İptal edilen tarama: işçiler durduruldu.
        }
        if (cancelled) return false;
        ct.ThrowIfCancellationRequested();
        return await FlushAsync(crawlId, state, Drain(batch), ct);
    }

    private static List<PageSnapshot> Drain(ConcurrentQueue<PageSnapshot> queue)
    {
        var items = new List<PageSnapshot>();
        while (queue.TryDequeue(out var item)) items.Add(item);
        return items;
    }

    private PageSnapshot BuildSnapshot(Guid crawlId, Uri url, FetchResult result, CrawlState state)
    {
        var normalized = UrlNormalizer.Normalize(url);
        var now = clock.GetUtcNow();
        if (result.Response is null)
        {
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
            data = parsed.Data with { RobotsHeader = response.RobotsTag };
            foreach (var link in parsed.InternalLinks) state.Enqueue(link);
            foreach (var script in data.Scripts) state.AddAsset(script.Src);
            foreach (var stylesheet in data.Stylesheets) state.AddAsset(stylesheet);
        }

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
