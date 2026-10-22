using System.Net.Http.Json;
using MarketingOps.Application.Crawls.Queries.GetCrawlById;
using MarketingOps.Domain.Crawls;
using MarketingOps.Infrastructure.Crawling;
using MarketingOps.Infrastructure.Jobs;
using MarketingOps.Infrastructure.Persistence;
using MarketingOps.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingOps.IntegrationTests.Crawling;

/// <summary>Büyük sitelerde gereken davranışlar: geri çekilme, kaldığı yerden devam, sayfa verisi saklama.</summary>
[Collection(IntegrationTestCollection.Name)]
public class ProductionCrawlTests(IntegrationTestContext context)
{
    private (WebApplicationFactory<Program> Factory, HttpClient Client, SiteCrawler Crawler, FakeSite Site) Arrange()
    {
        var site = new FakeSite(UniqueDomain.Next());
        var factory = context.Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<ICrawlerHttpClient>(site)));
        return (factory, factory.CreateClient(), factory.Services.GetRequiredService<SiteCrawler>(), site);
    }

    private static async Task<CrawlDetailResponse> DetailAsync(HttpClient client, Guid siteId, Guid crawlId) =>
        (await client.GetFromJsonAsync<CrawlDetailResponse>($"/api/sites/{siteId}/crawls/{crawlId}", IntegrationTestContext.Json))!;

    [Fact]
    public async Task Server_asking_to_slow_down_is_respected_and_the_page_is_retried()
    {
        var (_, client, crawler, site) = Arrange();
        var siteId = await TestSites.CreateAsync(client, site.Domain);
        var busy = 0;
        site.Intercept = path => path == "/urun/p3" && Interlocked.Increment(ref busy) == 1
            ? new CrawlerResponse(429, null, "text/html", null, null, null, null, 5, TimeSpan.FromSeconds(1))
            : null;

        var crawlId = await TestSites.StartCrawlAsync(client, siteId);
        await crawler.RunAsync(crawlId, CancellationToken.None);

        var detail = await DetailAsync(client, siteId, crawlId);
        detail.Crawl.Status.ShouldBe(CrawlStatus.Completed);
        detail.Crawl.PagesCrawled.ShouldBe(20);              // 429 alan sayfa sonra başarıyla tarandı
        site.Requested.Count(r => r == "/urun/p3").ShouldBe(2);
        detail.FailedPages.ShouldNotContain(p => p.Url.EndsWith("/urun/p3"));
    }

    [Fact]
    public async Task Interrupted_crawl_resumes_without_recrawling_or_duplicating_pages()
    {
        var (factory, client, crawler, site) = Arrange();
        var siteId = await TestSites.CreateAsync(client, site.Domain);
        var crawlId = await TestSites.StartCrawlAsync(client, siteId);

        // İşçi 12. istekten sonra kapanıyor.
        using (var shutdown = new CancellationTokenSource())
        {
            site.AfterRequest = count => { if (count == 12) shutdown.Cancel(); };
            await Should.ThrowAsync<OperationCanceledException>(() => crawler.RunAsync(crawlId, shutdown.Token));
        }
        var interrupted = await DetailAsync(client, siteId, crawlId);
        interrupted.Crawl.Status.ShouldBe(CrawlStatus.Running);   // hata sayılmadı

        List<string> savedBefore;
        await using (var scope = factory.Services.CreateAsyncScope())
            savedBefore = (await scope.ServiceProvider.GetRequiredService<AppDbContext>().PageSnapshots.Where(p => p.CrawlId == crawlId)
                .Select(p => p.Url).ToListAsync()).Select(u => new Uri(u).AbsolutePath).ToList();
        savedBefore.ShouldNotBeEmpty();

        site.AfterRequest = null;
        var requestsBefore = site.Requested.Count;
        await crawler.RunAsync(crawlId, CancellationToken.None);   // iş yeniden çalıştı

        var detail = await DetailAsync(client, siteId, crawlId);
        detail.Crawl.Status.ShouldBe(CrawlStatus.Completed);
        detail.Crawl.PagesCrawled.ShouldBe(20);
        await using (var scope = factory.Services.CreateAsyncScope())
            (await scope.ServiceProvider.GetRequiredService<AppDbContext>().PageSnapshots.CountAsync(p => p.CrawlId == crawlId)).ShouldBe(21); // 20 + 404
        // Yazılmış sayfalar yeniden indirilmedi. İstisna: /eski-urun → /urun/p1 yönlendirmesi izlenirken hedefe istek gider
        // (hedefin içeriği yönlenen adres için kaydedilmez).
        var redirectTargets = new[] { "/", "/urun/p1" };
        site.Requested.Skip(requestsBefore).Intersect(savedBefore.Except(redirectTargets)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Old_crawls_lose_page_data_but_keep_their_summary()
    {
        var (factory, client, crawler, site) = Arrange();
        var siteId = await TestSites.CreateAsync(client, site.Domain);
        var crawlIds = new List<Guid>();
        for (var i = 0; i < 4; i++)
        {
            var id = await TestSites.StartCrawlAsync(client, siteId);
            await crawler.RunAsync(id, CancellationToken.None);
            crawlIds.Add(id);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<SnapshotRetentionJob>().RunAsync(CancellationToken.None);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.PageSnapshots.CountAsync(p => p.CrawlId == crawlIds[0])).ShouldBe(0);
            (await db.PageSnapshots.CountAsync(p => p.CrawlId == crawlIds[1])).ShouldBeGreaterThan(0);
            (await db.PageSnapshots.CountAsync(p => p.CrawlId == crawlIds[3])).ShouldBeGreaterThan(0);
        }
        var oldest = await DetailAsync(client, siteId, crawlIds[0]);
        oldest.SnapshotsPrunedAt.ShouldNotBeNull();
        oldest.Crawl.PagesCrawled.ShouldBe(20);
        (await client.GetStringAsync($"/sites/{siteId}/crawls/{crawlIds[0]}")).ShouldContain("saklama süresi dolduğu için silindi");
    }
}
