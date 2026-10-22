using Hangfire;
using MarketingOps.Application.Findings.Commands.EvaluateCrawl;
using MediatR;

namespace MarketingOps.Infrastructure.Jobs;

/// <summary>Biten taramanın kural değerlendirmesi. Komut idempotenttir (değerlendirilmiş tarama atlanır); bu yüzden tekrar denenebilir.</summary>
public sealed class EvaluateCrawlJob(ISender sender)
{
    [AutomaticRetry(Attempts = 2)]
    public Task RunAsync(Guid crawlId, CancellationToken cancellationToken) => sender.Send(new EvaluateCrawlCommand(crawlId), cancellationToken);
}
