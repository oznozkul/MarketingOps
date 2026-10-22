using MarketingOps.Application.Common.Events;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Notifications;
using MediatR;

namespace MarketingOps.Application.Notifications.Events;

public sealed class FindingOpenedNotificationHandler(INotificationJobScheduler jobs) : INotificationHandler<DomainEventNotification<FindingOpened>>
{
    public Task Handle(DomainEventNotification<FindingOpened> notification, CancellationToken cancellationToken)
    {
        var opened = notification.Event;
        var type = NotificationEvents.ForOpened(opened.RuleCode, opened.Severity);
        if (type is not null)
            jobs.Enqueue(opened.SiteId, type.Value, opened.FindingId, null, null);
        return Task.CompletedTask;
    }
}
