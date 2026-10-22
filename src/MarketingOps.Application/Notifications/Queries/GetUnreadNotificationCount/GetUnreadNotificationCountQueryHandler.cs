using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Notifications.Queries.GetUnreadNotificationCount;

public sealed class GetUnreadNotificationCountQueryHandler(IAppDbContext db) : IRequestHandler<GetUnreadNotificationCountQuery, IReadOnlyList<UnreadNotificationCountResponse>>
{
    public async Task<IReadOnlyList<UnreadNotificationCountResponse>> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken) =>
        await db.UserNotifications.AsNoTracking()
            .Where(n => n.ReadAt == null)
            .GroupBy(n => n.SiteId)
            .Select(g => new UnreadNotificationCountResponse(g.Key, g.Count()))
            .ToListAsync(cancellationToken);
}
