using Hangfire;
using MarketingOps.Application.Notifications.Commands.SendDueReports;
using MediatR;

namespace MarketingOps.Infrastructure.Jobs;

public sealed class SendDueReportsJob(ISender sender)
{
    public const string RecurringId = "reports";

    [AutomaticRetry(Attempts = 0)]
    public Task RunAsync(CancellationToken cancellationToken) => sender.Send(new SendDueReportsCommand(), cancellationToken);
}
