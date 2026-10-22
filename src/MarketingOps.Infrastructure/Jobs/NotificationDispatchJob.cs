using Hangfire;
using MarketingOps.Application.Notifications.Commands.DispatchNotification;
using MarketingOps.Domain.Notifications;
using MediatR;

namespace MarketingOps.Infrastructure.Jobs;

public sealed class NotificationDispatchJob(ISender sender)
{
    [AutomaticRetry(Attempts = 3)]
    public Task RunAsync(Guid siteId, string eventType, Guid? findingId, int? magnitude, string? detail, CancellationToken cancellationToken) =>
        sender.Send(new DispatchNotificationCommand(siteId, Enum.Parse<NotificationEventType>(eventType), findingId, magnitude, detail), cancellationToken);
}
