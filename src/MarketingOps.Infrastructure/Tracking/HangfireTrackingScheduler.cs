using Hangfire;
using MarketingOps.Application.Tracking;
using MarketingOps.Infrastructure.Jobs;

namespace MarketingOps.Infrastructure.Tracking;

public sealed class HangfireTrackingScheduler(IBackgroundJobClient jobs) : ITrackingJobScheduler
{
    public void Enqueue(Guid siteId) => jobs.Enqueue<TrackingAuditJob>(job => job.RunAsync(siteId, CancellationToken.None));
}
