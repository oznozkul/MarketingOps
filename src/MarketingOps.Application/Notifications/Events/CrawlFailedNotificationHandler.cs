using MarketingOps.Application.Common.Events;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Notifications;
using MediatR;

namespace MarketingOps.Application.Notifications.Events;

public sealed class CrawlFailedNotificationHandler(INotificationJobScheduler jobs) : INotificationHandler<DomainEventNotification<CrawlFailed>>
{
    public Task Handle(DomainEventNotification<CrawlFailed> notification, CancellationToken cancellationToken)
    {
        var failed = notification.Event;
        jobs.Enqueue(failed.SiteId, NotificationEventType.CrawlFailed, null, null, failed.Number.ToString());
        return Task.CompletedTask;
    }
}
