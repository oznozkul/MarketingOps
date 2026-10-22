using Hangfire;
using MarketingOps.Application.Tasks.Commands.EnrichTask;
using MediatR;

namespace MarketingOps.Infrastructure.Jobs;

/// <summary>Görev önerisinin AI zenginleştirmesi. Başarısızlıkta görev şablon önerisiyle kalır; iki kez daha denenir.</summary>
public sealed class EnrichTaskJob(ISender sender)
{
    [AutomaticRetry(Attempts = 2)]
    public Task RunAsync(Guid taskId, CancellationToken cancellationToken) => sender.Send(new EnrichTaskCommand(taskId), cancellationToken);
}
