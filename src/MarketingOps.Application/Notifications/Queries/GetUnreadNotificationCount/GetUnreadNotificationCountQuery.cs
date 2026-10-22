using MediatR;

namespace MarketingOps.Application.Notifications.Queries.GetUnreadNotificationCount;

public sealed record GetUnreadNotificationCountQuery : IRequest<IReadOnlyList<UnreadNotificationCountResponse>>;
