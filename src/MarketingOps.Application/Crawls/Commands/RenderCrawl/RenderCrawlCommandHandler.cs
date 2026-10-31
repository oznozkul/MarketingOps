using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Rendering;
using MarketingOps.Domain.Rendering;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Application.Crawls.Commands.RenderCrawl;

/// <summary>
/// Render aşaması. Her sayfa tipinden ayardaki sayıda örnek ve ana sayfa, üç cihazda ölçülür. Render başarısız olsa da
/// tarama başarısız sayılmaz: aşama kaydedilir, değerlendirme yine çalışır ve render kuralları o taramada çalışmaz.
/// İş yeniden çalışırsa ölçülmüş (sayfa, cihaz) ikilileri atlanır.
/// </summary>
public sealed class RenderCrawlCommandHandler(
    IAppDbContext db,
    IRenderAuditor auditor,
    ICrawlScheduler scheduler,
    TimeProvider clock,
    ILogger<RenderCrawlCommandHandler> logger) : IRequestHandler<RenderCrawlCommand>
{
    private static readonly RenderDevice[] Devices = [RenderDevice.Desktop, RenderDevice.Tablet, RenderDevice.Mobile];

    public async Task Handle(RenderCrawlCommand request, CancellationToken cancellationToken)
    {
        var crawl = await db.Crawls.FirstOrDefaultAsync(c => c.Id == request.CrawlId, cancellationToken);
        if (crawl is null || !crawl.CanBeEvaluated || crawl.IsRendered) return;
        var site = await db.Sites.AsNoTracking().FirstAsync(s => s.Id == crawl.SiteId, cancellationToken);
        if (!site.CrawlSettings.RenderEnabled)
        {
            scheduler.EnqueueEvaluation(crawl.Id);
            return;
        }

        var startedAt = clock.GetUtcNow();
        try
        {
            var pages = await RenderSampling.LoadCandidatesAsync(db, crawl.Id, cancellationToken);
            var measured = (await db.RenderSamples.AsNoTracking().Where(r => r.CrawlId == crawl.Id)
                .Select(r => new { r.Url, r.Device }).ToListAsync(cancellationToken))
                .Select(r => (r.Url, r.Device)).ToHashSet();
            var targets = RenderSampling.Select(pages, site.CrawlSettings.RenderSamplesPerPageType)
                .SelectMany(p => Devices.Select(d => new RenderTarget(p.Url, p.PageTypeId, d)))
                .Where(t => !measured.Contains((t.Url, t.Device)))
                .ToList();

            var results = await auditor.AuditAsync(targets, new RenderOptions(site.CrawlSettings.MaxRequestsPerSecond, site.Journey.ConsentButtonSelector), cancellationToken);
            var now = clock.GetUtcNow();
            foreach (var r in results)
            {
                db.RenderSamples.Add(r.Measurements is { } m
                    ? RenderSample.Measured(crawl.Id, site.Id, r.Target.Url, r.Target.PageTypeId, r.Target.Device, m, r.Obstructed, now)
                    : RenderSample.Failed(crawl.Id, site.Id, r.Target.Url, r.Target.PageTypeId, r.Target.Device, r.Error ?? "Ölçüm alınamadı.", now));
                if (r.ScreenshotJpeg is { Length: > 0 })
                    db.RenderScreenshots.Add(RenderScreenshot.Create(crawl.Id, site.Id, r.Target.Url, r.Target.Device, r.ScreenshotHeight, r.ScreenshotJpeg, now));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Tarayıcı açılamadı ya da beklenmeyen bir hata: tarama ve değerlendirme render olmadan devam eder.
            logger.LogWarning(ex, "Render aşaması tamamlanamadı. Tarama {CrawlId}.", crawl.Id);
        }

        crawl.RecordRender(startedAt, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        scheduler.EnqueueEvaluation(crawl.Id);
    }
}
