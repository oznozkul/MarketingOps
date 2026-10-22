using MediatR;

namespace MarketingOps.Application.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery(Guid SiteId, int Take = 20) : IRequest<NotificationsResponse>;
