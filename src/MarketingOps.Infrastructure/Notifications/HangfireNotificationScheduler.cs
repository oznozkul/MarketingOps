using Hangfire;
using MarketingOps.Application.Notifications;
using MarketingOps.Domain.Notifications;
using MarketingOps.Infrastructure.Jobs;

namespace MarketingOps.Infrastructure.Notifications;

public sealed class HangfireNotificationScheduler(IBackgroundJobClient jobs) : INotificationJobScheduler
{
    public void Enqueue(Guid siteId, NotificationEventType eventType, Guid? findingId, int? magnitude, string? detail) =>
        jobs.Enqueue<NotificationDispatchJob>(job => job.RunAsync(siteId, eventType.ToString(), findingId, magnitude, detail, CancellationToken.None));

    public void Schedule(Guid siteId, NotificationEventType eventType, Guid? findingId, int? magnitude, string? detail, DateTimeOffset at) =>
        jobs.Schedule<NotificationDispatchJob>(job => job.RunAsync(siteId, eventType.ToString(), findingId, magnitude, detail, CancellationToken.None), at);
}
