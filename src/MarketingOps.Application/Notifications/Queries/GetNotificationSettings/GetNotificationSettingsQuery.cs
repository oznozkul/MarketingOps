using MediatR;

namespace MarketingOps.Application.Notifications.Queries.GetNotificationSettings;

public sealed record GetNotificationSettingsQuery(Guid SiteId) : IRequest<NotificationSettingsResponse>;
