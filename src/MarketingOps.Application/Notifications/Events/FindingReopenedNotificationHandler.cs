using MarketingOps.Application.Common.Events;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Notifications;
using MediatR;

namespace MarketingOps.Application.Notifications.Events;

public sealed class FindingReopenedNotificationHandler(INotificationJobScheduler jobs) : INotificationHandler<DomainEventNotification<FindingReopened>>
{
    public Task Handle(DomainEventNotification<FindingReopened> notification, CancellationToken cancellationToken)
    {
        var reopened = notification.Event;
        jobs.Enqueue(reopened.SiteId, NotificationEventType.FindingReopened, reopened.FindingId, null, null);
        return Task.CompletedTask;
    }
}
