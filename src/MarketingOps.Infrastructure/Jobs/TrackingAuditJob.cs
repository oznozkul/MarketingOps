using Hangfire;
using MarketingOps.Application.Tracking.Commands.RunTrackingAudit;
using MediatR;

namespace MarketingOps.Infrastructure.Jobs;

/// <summary>Denetim kendi içinde hata durumunu yazar. Tekrar denemek siteyi ikinci kez açar.</summary>
public sealed class TrackingAuditJob(ISender sender)
{
    [AutomaticRetry(Attempts = 0)]
    public Task RunAsync(Guid siteId, CancellationToken cancellationToken) =>
        sender.Send(new RunTrackingAuditCommand(siteId), cancellationToken);
}
