using System.IO.Compression;
using System.Net;
using System.Text;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Sites;
using MarketingOps.Infrastructure.Crawling;

namespace MarketingOps.UnitTests.Crawling;

public class ProductionCrawlUnitTests
{
    [Fact]
    public void Robots_crawl_delay_is_read_for_our_group_and_capped()
    {
        RobotsRules.Parse("User-agent: *\nCrawl-delay: 2.5\nDisallow: /x", CrawlerHttpClient.UserAgent).CrawlDelay.ShouldBe(TimeSpan.FromSeconds(2.5));
        RobotsRules.Parse("User-agent: MarketingOpsBot\nCrawl-delay: 5\n\nUser-agent: *\nCrawl-delay: 1", CrawlerHttpClient.UserAgent).CrawlDelay.ShouldBe(TimeSpan.FromSeconds(5));
        RobotsRules.Parse("User-agent: *\nCrawl-delay: 600", CrawlerHttpClient.UserAgent).CrawlDelay.ShouldBe(TimeSpan.FromSeconds(60));
        RobotsRules.Parse("User-agent: *\nDisallow:", CrawlerHttpClient.UserAgent).CrawlDelay.ShouldBeNull();
    }

    [Fact]
    public async Task Gzipped_sitemaps_and_retry_after_are_read()
    {
        var xml = "<urlset><url><loc>https://ornek.com/a</loc></url></urlset>";
        using var compressed = new MemoryStream();
        await using (var gzip = new GZipStream(compressed, CompressionLevel.Fastest, leaveOpen: true)) gzip.Write(Encoding.UTF8.GetBytes(xml));
        var http = new HttpClient(new GzipFileHandler(compressed.ToArray()));

        var sitemap = await new CrawlerHttpClient(http).GetAsync(new Uri("https://ornek.com/sitemap.xml.gz"), CancellationToken.None);
        sitemap.Body.ShouldBe(xml);

        var busy = await new CrawlerHttpClient(new HttpClient(new RetryAfterHandler())).GetAsync(new Uri("https://ornek.com/a"), CancellationToken.None);
        busy.StatusCode.ShouldBe(429);
        busy.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Large_sites_can_be_configured_and_pruning_requires_a_finished_crawl()
    {
        new CrawlSettings(CrawlSchedule.Weekly, 5, 200_000, []).MaxPages.ShouldBe(200_000);
        Should.Throw<DomainException>(() => new CrawlSettings(CrawlSchedule.Weekly, 5, 600_000, []));

        var crawl = Crawl.Queue(Guid.CreateVersion7(), true, 1, CrawlTrigger.Manual, DateTimeOffset.UtcNow);
        Should.Throw<DomainException>(() => crawl.MarkSnapshotsPruned(DateTimeOffset.UtcNow));
    }

    private sealed class GzipFileHandler(byte[] body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = new ByteArrayContent(body);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-gzip");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }

    private sealed class RetryAfterHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("") };
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return Task.FromResult(response);
        }
    }
}
