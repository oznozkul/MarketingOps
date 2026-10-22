using MarketingOps.Application.Common.Events;
using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Notifications;
using MediatR;

namespace MarketingOps.Application.Notifications.Events;

public sealed class ConnectorExpiredNotificationHandler(INotificationJobScheduler jobs) : INotificationHandler<DomainEventNotification<ConnectorAuthorizationExpired>>
{
    public Task Handle(DomainEventNotification<ConnectorAuthorizationExpired> notification, CancellationToken cancellationToken)
    {
        var expired = notification.Event;
        jobs.Enqueue(expired.SiteId, NotificationEventType.ConnectorAuthorizationExpired, null, null, expired.Type.ToString());
        return Task.CompletedTask;
    }
}
