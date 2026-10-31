using MarketingOps.Domain.Crawls;
using MarketingOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Infrastructure.Jobs;

/// <summary>
/// Sayfa verisi (snapshot) büyük sitelerde tarama başına gigabaytlara ulaşır. Her sitede yalnızca son N bitmiş taramanın
/// sayfaları tutulur (varsayılan 3; "Crawling:KeepSnapshotsForCrawls"). Daha eskilerin özeti, aşamaları ve bulgu değişimleri kalır.
/// </summary>
public sealed class SnapshotRetentionJob(AppDbContext db, IConfiguration configuration, TimeProvider clock, ILogger<SnapshotRetentionJob> logger)
{
    public const string RecurringId = "snapshot-retention";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var keep = Math.Max(1, configuration.GetValue("Crawling:KeepSnapshotsForCrawls", 3));
        var finished = await db.Crawls
            .Where(c => c.SnapshotsPrunedAt == null && c.Status != CrawlStatus.Queued && c.Status != CrawlStatus.Running)
            .Select(c => new { c.Id, c.SiteId, c.Number, c.Status })
            .ToListAsync(cancellationToken);

        // Son N başarılı tarama ve onlardan yeni olan (hata/iptal) taramalar tutulur.
        var prune = finished.GroupBy(c => c.SiteId).SelectMany(site =>
        {
            var kept = site.Where(c => c.Status is CrawlStatus.Completed or CrawlStatus.PartiallyCompleted)
                .OrderByDescending(c => c.Number).Take(keep).ToList();
            var oldestKept = kept.Count < keep ? int.MinValue : kept.Min(c => c.Number);
            return site.Where(c => c.Number < oldestKept);
        }).Select(c => c.Id).ToList();

        foreach (var crawlId in prune)
        {
            var deleted = await db.PageSnapshots.Where(p => p.CrawlId == crawlId).ExecuteDeleteAsync(cancellationToken);
            await db.RenderSamples.Where(r => r.CrawlId == crawlId).ExecuteDeleteAsync(cancellationToken);
            await db.RenderScreenshots.Where(r => r.CrawlId == crawlId).ExecuteDeleteAsync(cancellationToken);
            var crawl = await db.Crawls.FirstAsync(c => c.Id == crawlId, cancellationToken);
            crawl.MarkSnapshotsPruned(clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Tarama {CrawlId}: {Count} sayfa kaydı saklama süresi dolduğu için silindi.", crawlId, deleted);
        }
    }
}
