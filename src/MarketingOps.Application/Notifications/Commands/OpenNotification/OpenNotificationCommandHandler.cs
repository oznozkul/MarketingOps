using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Notifications.Commands.OpenNotification;

public sealed class OpenNotificationCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<OpenNotificationCommand, string?>
{
    public async Task<string?> Handle(OpenNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await db.UserNotifications.FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.SiteId == request.SiteId, cancellationToken);
        if (notification is null) return null;
        notification.MarkRead(clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return notification.Href;
    }
}
