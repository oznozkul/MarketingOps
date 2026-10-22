using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Notifications;
using MarketingOps.Domain.Notifications;

namespace MarketingOps.Infrastructure.Notifications;

public sealed class InAppNotificationChannel(IAppDbContext db, TimeProvider clock) : INotificationChannel
{
    public NotificationChannelKind Kind => NotificationChannelKind.InApp;

    public Task SendAsync(OutboundNotification message, string? secret, CancellationToken cancellationToken)
    {
        db.UserNotifications.Add(UserNotification.Create(message.SiteId, message.EventType, message.Title, message.Body, message.Href, clock.GetUtcNow()));
        return Task.CompletedTask;
    }
}
