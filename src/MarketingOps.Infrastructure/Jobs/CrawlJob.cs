using Hangfire;
using MarketingOps.Infrastructure.Crawling;

namespace MarketingOps.Infrastructure.Jobs;

/// <summary>Kuyruktaki taramayı yürütür. Tekrar denenmez: yarıda kalan tarama "hata" olarak kapanır, yenisi başlatılır.</summary>
public sealed class CrawlJob(SiteCrawler crawler)
{
    [AutomaticRetry(Attempts = 0)]
    [Queue("crawls")]
    public Task RunAsync(Guid crawlId, CancellationToken cancellationToken) => crawler.RunAsync(crawlId, cancellationToken);
}
