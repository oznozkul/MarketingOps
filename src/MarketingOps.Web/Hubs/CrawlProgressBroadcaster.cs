using MarketingOps.Application.Crawls.Queries.GetActiveCrawls;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace MarketingOps.Web.Hubs;

/// <summary>
/// Taramayı Worker yürütür, istemciler Web'e bağlıdır. İki süreç arasında Redis backplane ya da LISTEN/NOTIFY yerine
/// en basit yol seçildi: Web, aktif taramaları 2 saniyede bir veritabanından okuyup ilgili site grubuna yayınlar.
/// Biten taramalar için "crawlFinished" gönderilir.
/// </summary>
public sealed class CrawlProgressBroadcaster(IServiceScopeFactory scopes, IHubContext<CrawlHub> hub, ILogger<CrawlProgressBroadcaster> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var previous = new Dictionary<Guid, Guid>(); // crawlId → siteId
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var active = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new GetActiveCrawlsQuery(), stoppingToken);

                foreach (var crawl in active)
                    await hub.Clients.Group(CrawlHub.GroupFor(crawl.SiteId)).SendAsync("crawlProgress", crawl, stoppingToken);

                foreach (var (crawlId, siteId) in previous.Where(p => active.All(a => a.CrawlId != p.Key)))
                    await hub.Clients.Group(CrawlHub.GroupFor(siteId)).SendAsync("crawlFinished", new { crawlId }, stoppingToken);

                previous = active.ToDictionary(a => a.CrawlId, a => a.SiteId);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Geçici veritabanı hatası yayını durdurmasın.
                logger.LogWarning(ex, "Tarama ilerlemesi yayınlanamadı.");
            }
        }
    }
}
