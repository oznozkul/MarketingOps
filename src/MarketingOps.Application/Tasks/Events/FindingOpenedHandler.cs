using MarketingOps.Application.Common.Events;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Findings;
using MediatR;

namespace MarketingOps.Application.Tasks.Events;

/// <summary>Yeni bulgu → görev. Öneri önce şablondan gelir; AI ayarlıysa zenginleştirme ayrı işte yapılır.</summary>
public sealed class FindingOpenedHandler(IAppDbContext db, WorkTaskFactory factory, ITaskJobScheduler jobs) : INotificationHandler<DomainEventNotification<FindingOpened>>
{
    public async Task Handle(DomainEventNotification<FindingOpened> notification, CancellationToken cancellationToken)
    {
        var finding = await db.Findings.FindAsync([notification.Event.FindingId], cancellationToken);
        if (finding is null) return;

        var task = await factory.CreateAsync(finding, cancellationToken);
        if (task is null) return;
        await db.SaveChangesAsync(cancellationToken);
        jobs.EnqueueEnrichment(task.Id);
    }
}
