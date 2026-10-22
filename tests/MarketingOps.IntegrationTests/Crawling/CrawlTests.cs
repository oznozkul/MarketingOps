using System.Net;
using System.Net.Http.Json;
using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Crawls.Queries.GetCrawlById;
using MarketingOps.Application.Crawls.Queries.GetPages;
using MarketingOps.Domain.Crawls;
using MarketingOps.Infrastructure.Crawling;
using MarketingOps.Infrastructure.Jobs;
using MarketingOps.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingOps.IntegrationTests.Crawling;

[Collection(IntegrationTestCollection.Name)]
public class CrawlTests(IntegrationTestContext context)
{
    private (HttpClient Client, SiteCrawler Crawler, FakeSite Site) Arrange()
    {
        var site = new FakeSite(UniqueDomain.Next());
        var factory = context.Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<ICrawlerHttpClient>(site)));
        return (factory.CreateClient(), factory.Services.GetRequiredService<SiteCrawler>(), site);
    }

    [Fact]
    public async Task Crawls_the_site_assigns_page_types_and_respects_robots_and_exclusions()
    {
        var (client, crawler, site) = Arrange();
        var siteId = await TestSites.CreateAsync(client, site.Domain);
        var crawlId = await TestSites.StartCrawlAsync(client, siteId);

        await crawler.RunAsync(crawlId, CancellationToken.None);

        var detail = await client.GetFromJsonAsync<CrawlDetailResponse>($"/api/sites/{siteId}/crawls/{crawlId}", IntegrationTestContext.Json);
        detail!.Crawl.Status.ShouldBe(CrawlStatus.Completed);
        detail.Crawl.PagesCrawled.ShouldBe(20); // ana sayfa + 3 kategori + 12 ürün + 2 blog + hakkımızda + yönlenen /eski-urun
        detail.Crawl.PagesFailed.ShouldBe(1);   // 404
        detail.Stages.Select(s => s.Stage).ShouldBe([CrawlStage.Discovery, CrawlStage.Download]);
        detail.Stages.ShouldAllBe(s => s.FinishedAt != null);

        detail.PageTypes.Single(p => p.Name == "Ürün").Count.ShouldBe(12);
        detail.PageTypes.Single(p => p.Name == "Kategori").Count.ShouldBe(3);
        detail.PageTypes.Single(p => p.Name == "Blog").Count.ShouldBe(2);
        detail.PageTypes.Single(p => p.PageTypeId == null).Count.ShouldBe(4); // ana sayfa, hakkımızda, 404, /eski-urun

        detail.SkippedUrls.ShouldContain(s => s.Url.EndsWith("/hesabim/siparisler") && s.Reason == SkipReason.RobotsTxt);
        detail.SkippedUrls.ShouldContain(s => s.Url.EndsWith("/arama/lastik") && s.Reason == SkipReason.ExcludedPath);
        site.Requested.ShouldNotContain("/hesabim/siparisler");
        site.Requested.ShouldNotContain("/arama/lastik");
        detail.FailedPages.ShouldHaveSingleItem().StatusCode.ShouldBe(404);

        // Yönlendirme zinciri kaydedilir; yönlenen adres hedefin içeriğini ve tipini almaz, hedef tek snapshot'tır.
        var redirected = await client.GetFromJsonAsync<PagedResponse<PageListItemResponse>>(
            $"/api/sites/{siteId}/crawls/{crawlId}/pages?search=eski-urun", IntegrationTestContext.Json);
        var hop = redirected!.Items.ShouldHaveSingleItem();
        hop.RedirectCount.ShouldBe(1);
        hop.StatusCode.ShouldBe(200);
        hop.PageTypeName.ShouldBeNull();
        hop.Title.ShouldBeNull();
        var target = await client.GetFromJsonAsync<PagedResponse<PageListItemResponse>>(
            $"/api/sites/{siteId}/crawls/{crawlId}/pages?search=/urun/p1", IntegrationTestContext.Json);
        target!.Items.Count(p => p.Url.EndsWith("/urun/p1")).ShouldBe(1);
    }

    [Fact]
    public async Task Max_pages_limits_the_crawl_and_records_skips()
    {
        var (client, crawler, site) = Arrange();
        var siteId = await TestSites.CreateAsync(client, site.Domain, maxPages: 5);
        var crawlId = await TestSites.StartCrawlAsync(client, siteId);

        await crawler.RunAsync(crawlId, CancellationToken.None);

        var detail = await client.GetFromJsonAsync<CrawlDetailResponse>($"/api/sites/{siteId}/crawls/{crawlId}", IntegrationTestContext.Json);
        (detail!.Crawl.PagesCrawled + detail.Crawl.PagesFailed).ShouldBe(5);
        detail.SkippedUrls.ShouldContain(s => s.Reason == SkipReason.MaxPagesReached);
    }

    [Fact]
    public async Task Second_crawl_while_one_is_active_is_a_conflict()
    {
        var (client, _, site) = Arrange();
        var siteId = await TestSites.CreateAsync(client, site.Domain);
        await TestSites.StartCrawlAsync(client, siteId);

        var second = await client.PostAsync($"/api/sites/{siteId}/crawls", null);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cancelled_crawl_is_not_run()
    {
        var (client, crawler, site) = Arrange();
        var siteId = await TestSites.CreateAsync(client, site.Domain);
        var crawlId = await TestSites.StartCrawlAsync(client, siteId);

        (await client.PostAsync($"/api/sites/{siteId}/crawls/{crawlId}/cancel", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await crawler.RunAsync(crawlId, CancellationToken.None);

        site.Requested.ShouldBeEmpty();
        var detail = await client.GetFromJsonAsync<CrawlDetailResponse>($"/api/sites/{siteId}/crawls/{crawlId}", IntegrationTestContext.Json);
        detail!.Crawl.Status.ShouldBe(CrawlStatus.Cancelled);
        // İptalden sonra yeni tarama başlatılabilir.
        (await client.PostAsync($"/api/sites/{siteId}/crawls", null)).StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Screens_show_the_crawl_its_pages_and_page_type_counts()
    {
        var (client, crawler, site) = Arrange();
        var siteId = await TestSites.CreateAsync(client, site.Domain);
        var crawlId = await TestSites.StartCrawlAsync(client, siteId);

        var whileQueued = await client.GetStringAsync($"/sites/{siteId}/crawls");
        whileQueued.ShouldContain("crawlLive(");          // canlı ilerleme kartı
        whileQueued.ShouldNotContain("Şimdi tara");         // aktif tarama varken başlatılamaz

        await crawler.RunAsync(crawlId, CancellationToken.None);

        var list = await client.GetStringAsync($"/sites/{siteId}/crawls");
        list.ShouldContain("Şimdi tara");
        list.ShouldContain("Tamamlandı");
        list.ShouldContain($"/sites/{siteId}/crawls/{crawlId}");

        var detail = await client.GetStringAsync($"/sites/{siteId}/crawls/{crawlId}");
        detail.ShouldContain("Aşamalar");
        detail.ShouldContain("Keşif");
        detail.ShouldContain("Bu taramada çalışmadı");      // render/kural/connector aşamaları
        detail.ShouldContain("robots.txt engelli");
        detail.ShouldContain($"https://{site.Domain}/urun/p1");

        var pageTypes = await client.GetStringAsync($"/sites/{siteId}/settings/page-types");
        pageTypes.ShouldContain($"Eşleşen sayfa sayıları tarama #1 verisidir");
        pageTypes.ShouldContain(">12</a>");                  // Ürün tipine 12 sayfa

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sites/{siteId}/crawls/{crawlId}?status=ClientError");
        request.Headers.Add("HX-Request", "true");
        var partial = await (await client.SendAsync(request)).Content.ReadAsStringAsync();
        partial.ShouldStartWith("<div id=\"pages-region\"");
        partial.ShouldContain("yok-boyle-sayfa");
        partial.ShouldNotContain("/urun/p1");

        var topbar = await client.GetStringAsync($"/sites/{siteId}/overview");
        topbar.ShouldContain("Son tarama");
    }

    [Fact]
    public async Task Scheduled_job_does_not_crawl_unverified_sites()
    {
        var (client, _, site) = Arrange();
        var siteId = await TestSites.CreateAsync(client, site.Domain);

        await using var scope = context.Factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ScheduledCrawlJob>().RunAsync(siteId, CancellationToken.None);

        var crawls = await client.GetFromJsonAsync<PagedResponse<MarketingOps.Application.Crawls.Queries.CrawlListItemResponse>>(
            $"/api/sites/{siteId}/crawls", IntegrationTestContext.Json);
        crawls!.TotalCount.ShouldBe(0);
        site.Requested.ShouldBeEmpty();
    }
}
