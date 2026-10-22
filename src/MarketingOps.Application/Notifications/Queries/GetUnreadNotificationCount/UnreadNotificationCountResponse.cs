namespace MarketingOps.Application.Notifications.Queries.GetUnreadNotificationCount;

public sealed record UnreadNotificationCountResponse(Guid SiteId, int Unread);
