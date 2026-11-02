using MarketingOps.Application.Notifications.Commands.MarkNotificationsRead;
using MarketingOps.Application.Notifications.Commands.OpenNotification;
using MarketingOps.Application.Notifications.Queries.GetNotifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

/// <summary>Üst bardaki zil: panel içi bildirim listesi. Liste partial olarak yenilenir; sayfası yoktur.</summary>
[Route("sites/{siteId:guid}/notifications")]
public class NotificationsController(ISender sender) : SiteControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Panel(Guid siteId, CancellationToken ct) =>
        PartialView("_NotificationPanel", await sender.Send(new GetNotificationsQuery(siteId), ct));

    [HttpPost("read")]
    public async Task<IActionResult> MarkRead(Guid siteId, CancellationToken ct)
    {
        await sender.Send(new MarkNotificationsReadCommand(siteId), ct);
        return await Panel(siteId, ct);
    }

    /// <summary>Bildirimi okundu sayar ve ilgili ekrana götürür.</summary>
    [HttpGet("{notificationId:guid}")]
    public async Task<IActionResult> Open(Guid siteId, Guid notificationId, CancellationToken ct)
    {
        var href = await sender.Send(new OpenNotificationCommand(siteId, notificationId), ct);
        // Bağlantı yalnızca uygulama içine gider; kayıtta dış adres olsa bile açık yönlendirme yapılmaz.
        return href is not null && Url.IsLocalUrl(href)
            ? LocalRedirect(href)
            : RedirectToAction("Index", "Overview", new { siteId });
    }
}
