using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Notifications.Commands.MarkNotificationsRead;

public sealed class MarkNotificationsReadCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<MarkNotificationsReadCommand>
{
    public async Task Handle(MarkNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var unread = await db.UserNotifications.Where(n => n.SiteId == request.SiteId && n.ReadAt == null).ToListAsync(cancellationToken);
        var now = clock.GetUtcNow();
        foreach (var notification in unread) notification.MarkRead(now);
        await db.SaveChangesAsync(cancellationToken);
    }
}
