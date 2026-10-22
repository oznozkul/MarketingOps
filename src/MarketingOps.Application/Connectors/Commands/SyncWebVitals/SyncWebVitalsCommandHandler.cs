using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Google;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Application.Connectors.Commands.SyncWebVitals;

public sealed class SyncWebVitalsCommandHandler(IAppDbContext db, IPageSpeedClient pageSpeed, GoogleSyncOptions options, TimeProvider clock,
    ILogger<SyncWebVitalsCommandHandler> logger) : IRequestHandler<SyncWebVitalsCommand>
{
    public async Task Handle(SyncWebVitalsCommand request, CancellationToken cancellationToken)
    {
        var crawlId = await db.Crawls.Where(c => c.SiteId == request.SiteId && (c.Status == CrawlStatus.Completed || c.Status == CrawlStatus.PartiallyCompleted))
            .OrderByDescending(c => c.Number).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(cancellationToken);
        if (crawlId is null) return;

        // Örnekler: her sayfa tipinden en kısa URL'li sayfalar (genellikle listenin "temsilci" sayfaları).
        var pages = (await db.PageSnapshots.Where(p => p.CrawlId == crawlId && p.PageTypeId != null && p.StatusCode >= 200 && p.StatusCode < 300)
                .Select(p => new { p.Url, p.PageTypeId, p.RedirectChain }).ToListAsync(cancellationToken))
            .Where(p => p.RedirectChain.Count == 0).ToList();
        var samples = pages.GroupBy(p => p.PageTypeId).SelectMany(g => g.OrderBy(p => p.Url.Length).Take(options.WebVitalsSamplesPerPageType)).ToList();

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var existing = (await db.WebVitals.Where(v => v.SiteId == request.SiteId && v.Date == today).ToListAsync(cancellationToken))
            .ToDictionary(v => (v.Url, v.Strategy));
        foreach (var sample in samples)
        foreach (var strategy in Enum.GetValues<DeviceStrategy>())
        {
            try
            {
                var result = await pageSpeed.RunAsync(sample.Url, strategy, cancellationToken);
                if (!existing.TryGetValue((sample.Url, strategy), out var row))
                    db.WebVitals.Add(row = WebVitalsDaily.Create(request.SiteId, today, sample.Url, sample.PageTypeId, strategy));
                row.Update(sample.PageTypeId, result.Source, result.LcpMs, result.InpMs, result.Cls, result.TtfbMs);
            }
            catch (GoogleApiException ex)
            {
                // Tek sayfanın ölçülememesi diğerlerini durdurmaz.
                logger.LogWarning("PageSpeed ölçümü başarısız ({Url}, {Strategy}): {Message}", sample.Url, strategy, ex.Message);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
