using Microsoft.AspNetCore.SignalR;

namespace MarketingOps.Web.Hubs;

/// <summary>
/// Açık sitenin canlı olayları: tarama ilerlemesi (CrawlProgressBroadcaster) ve zildeki okunmamış sayısı
/// (NotificationCountBroadcaster). İstemci açtığı sitenin grubuna katılır.
/// </summary>
public sealed class CrawlHub : Hub
{
    public const string Path = "/hubs/crawls";

    public static string GroupFor(Guid siteId) => $"site:{siteId}";

    public Task JoinSite(Guid siteId) => Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(siteId));
}
