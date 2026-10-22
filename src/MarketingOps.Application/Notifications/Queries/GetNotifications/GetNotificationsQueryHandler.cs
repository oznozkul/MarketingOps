using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(IAppDbContext db) : IRequestHandler<GetNotificationsQuery, NotificationsResponse>
{
    public async Task<NotificationsResponse> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var unread = await db.UserNotifications.CountAsync(n => n.SiteId == request.SiteId && n.ReadAt == null, cancellationToken);
        var items = await db.UserNotifications.AsNoTracking()
            .Where(n => n.SiteId == request.SiteId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(request.Take)
            .Select(n => new NotificationItemResponse(n.Id, n.EventType, n.Title, n.Body, n.Href, n.CreatedAt, n.ReadAt == null))
            .ToListAsync(cancellationToken);
        return new NotificationsResponse(unread, items);
    }
}
