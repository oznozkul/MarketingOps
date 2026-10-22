using Hangfire;
using MarketingOps.Application.Common.Interfaces;

namespace MarketingOps.Infrastructure.Jobs;

public sealed class HangfireTaskJobScheduler(IBackgroundJobClient jobs) : ITaskJobScheduler
{
    public void EnqueueEnrichment(Guid taskId) => jobs.Enqueue<EnrichTaskJob>(job => job.RunAsync(taskId, CancellationToken.None));
}
