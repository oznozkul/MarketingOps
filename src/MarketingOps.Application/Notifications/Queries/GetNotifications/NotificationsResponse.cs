namespace MarketingOps.Application.Notifications.Queries.GetNotifications;

public sealed record NotificationsResponse(int Unread, IReadOnlyList<NotificationItemResponse> Items);
