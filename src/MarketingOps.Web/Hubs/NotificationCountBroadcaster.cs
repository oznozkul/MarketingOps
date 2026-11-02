using MarketingOps.Application.Notifications.Queries.GetUnreadNotificationCount;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace MarketingOps.Web.Hubs;

/// <summary>
/// Panel içi bildirimleri Worker yazar; zildeki okunmamış sayısı tarama ilerlemesiyle aynı yoldan, veritabanı okunarak
/// site grubuna yayınlanır. Yalnızca sayısı değişen sitelere "notificationCount" gönderilir.
/// </summary>
public sealed class NotificationCountBroadcaster(IServiceScopeFactory scopes, IHubContext<CrawlHub> hub, ILogger<NotificationCountBroadcaster> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var previous = new Dictionary<Guid, int>(); // siteId → okunmamış
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var counts = (await scope.ServiceProvider.GetRequiredService<ISender>().Send(new GetUnreadNotificationCountQuery(), stoppingToken))
                    .ToDictionary(c => c.SiteId, c => c.Unread);

                // Listeden düşen site artık okunmamış bildirimi kalmamış sitedir.
                foreach (var siteId in counts.Keys.Union(previous.Keys))
                {
                    var unread = counts.GetValueOrDefault(siteId);
                    if (previous.GetValueOrDefault(siteId) != unread)
                        await hub.Clients.Group(CrawlHub.GroupFor(siteId)).SendAsync("notificationCount", new { unread }, stoppingToken);
                }

                previous = counts;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Bildirim sayısı yayınlanamadı.");
            }
        }
    }
}
