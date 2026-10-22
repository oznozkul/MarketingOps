using MarketingOps.Domain.Notifications;

namespace MarketingOps.Application.Notifications;

public sealed record OutboundNotification(Guid SiteId, NotificationEventType EventType, string Title, string Body, string? Href, IReadOnlyList<string> Recipients, bool Html);
