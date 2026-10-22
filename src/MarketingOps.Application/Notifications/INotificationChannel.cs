using MarketingOps.Domain.Notifications;

namespace MarketingOps.Application.Notifications;

public interface INotificationChannel
{
    NotificationChannelKind Kind { get; }

    Task SendAsync(OutboundNotification message, string? secret, CancellationToken cancellationToken);
}
