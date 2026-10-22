using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Notifications;

/// <summary>Gönderilmiş bir bildirim. Aynı bulgu için tekrar aralığı buna bakarak hesaplanır.</summary>
public class NotificationDelivery : Entity
{
    public Guid SiteId { get; private set; }
    public NotificationEventType EventType { get; private set; }
    public Guid? FindingId { get; private set; }
    public NotificationChannelKind Channel { get; private set; }
    public DateTimeOffset SentAt { get; private set; }

    private NotificationDelivery() { }

    public static NotificationDelivery Record(Guid siteId, NotificationEventType eventType, Guid? findingId, NotificationChannelKind channel, DateTimeOffset sentAt) =>
        new()
        {
            SiteId = siteId,
            EventType = eventType,
            FindingId = findingId,
            Channel = channel,
            SentAt = sentAt
        };
}
