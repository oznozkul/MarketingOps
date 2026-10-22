using Hangfire;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Sites;
using MarketingOps.Infrastructure.Jobs;

namespace MarketingOps.Infrastructure.Crawling;

public sealed class HangfireCrawlScheduler(IBackgroundJobClient jobs, IRecurringJobManager recurring) : ICrawlScheduler
{
    /// <summary>Gece taramaları Türkiye saatiyle yapılır; siteler o saatlerde en az trafiği alır.</summary>
    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public void EnqueueCrawl(Guid crawlId) => jobs.Enqueue<CrawlJob>(job => job.RunAsync(crawlId, CancellationToken.None));

    public void EnqueueEvaluation(Guid crawlId) => jobs.Enqueue<EvaluateCrawlJob>(job => job.RunAsync(crawlId, CancellationToken.None));

    public void EnqueueConnectorSync(Guid siteId) => jobs.Enqueue<ConnectorSyncJob>(job => job.RunAsync(siteId, false, CancellationToken.None));

    public void EnqueueWebVitals(Guid siteId) => jobs.Enqueue<ConnectorSyncJob>(job => job.MeasureWebVitalsAsync(siteId, CancellationToken.None));

    /// <summary>Tarama sıklığa göre gece; connector senkronu her gün taramalardan sonra (05:00).</summary>
    public void ScheduleSite(Guid siteId, CrawlSchedule schedule)
    {
        var options = new RecurringJobOptions { TimeZone = TimeZone };
        recurring.AddOrUpdate<ScheduledCrawlJob>(RecurringId(siteId), job => job.RunAsync(siteId, CancellationToken.None), Cron(schedule), options);
        recurring.AddOrUpdate<ConnectorSyncJob>(SyncId(siteId), job => job.RunAsync(siteId, true, CancellationToken.None), "0 5 * * *", options);
    }

    public void RemoveSite(Guid siteId)
    {
        recurring.RemoveIfExists(RecurringId(siteId));
        recurring.RemoveIfExists(SyncId(siteId));
    }

    private static string RecurringId(Guid siteId) => $"site-crawl:{siteId}";

    private static string SyncId(Guid siteId) => $"site-sync:{siteId}";

    private static string Cron(CrawlSchedule schedule) => schedule switch
    {
        CrawlSchedule.Every12Hours => "0 2,14 * * *",
        CrawlSchedule.Weekly => "0 2 * * 1",
        _ => "0 2 * * *"
    };
}
