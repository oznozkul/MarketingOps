using MarketingOps.Application.Common.Events;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Findings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tasks.Events;

/// <summary>Bulgu geri geldi → mevcut görev Yapılacak'a döner; yeni görev açılmaz.</summary>
public sealed class FindingReopenedHandler(IAppDbContext db, TimeProvider clock) : INotificationHandler<DomainEventNotification<FindingReopened>>
{
    public async Task Handle(DomainEventNotification<FindingReopened> notification, CancellationToken cancellationToken)
    {
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.FindingId == notification.Event.FindingId, cancellationToken);
        if (task is null) return;
        task.FindingReopened(notification.Event.CrawlNumber, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
