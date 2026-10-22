using MarketingOps.Domain.Notifications;

namespace MarketingOps.Application.Notifications.Queries.GetNotifications;

public sealed record NotificationItemResponse(Guid Id, NotificationEventType EventType, string Title, string Body, string? Href, DateTimeOffset CreatedAt, bool Unread);
