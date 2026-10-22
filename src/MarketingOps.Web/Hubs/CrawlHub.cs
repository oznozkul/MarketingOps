using Microsoft.AspNetCore.SignalR;

namespace MarketingOps.Web.Hubs;

/// <summary>Tarama ilerlemesi. İstemci açtığı sitenin grubuna katılır; yayını CrawlProgressBroadcaster yapar.</summary>
public sealed class CrawlHub : Hub
{
    public const string Path = "/hubs/crawls";

    public static string GroupFor(Guid siteId) => $"site:{siteId}";

    public Task JoinSite(Guid siteId) => Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(siteId));
}
