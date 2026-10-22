using MarketingOps.Application.Common.Events;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Findings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tasks.Events;

/// <summary>Bulgu çözüldü → görevin otomatik kriterleri sağlanır; ekip kriterleri de sağlanmışsa görev kapanır.</summary>
public sealed class FindingResolvedHandler(IAppDbContext db, TimeProvider clock) : INotificationHandler<DomainEventNotification<FindingResolved>>
{
    public async Task Handle(DomainEventNotification<FindingResolved> notification, CancellationToken cancellationToken)
    {
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.FindingId == notification.Event.FindingId, cancellationToken);
        if (task is null) return;
        task.FindingResolved(notification.Event.CrawlNumber, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
