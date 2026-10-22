using MarketingOps.Domain.Notifications;

namespace MarketingOps.Application.Notifications;

public interface INotificationJobScheduler
{
    void Enqueue(Guid siteId, NotificationEventType eventType, Guid? findingId, int? magnitude, string? detail);

    void Schedule(Guid siteId, NotificationEventType eventType, Guid? findingId, int? magnitude, string? detail, DateTimeOffset at);
}
