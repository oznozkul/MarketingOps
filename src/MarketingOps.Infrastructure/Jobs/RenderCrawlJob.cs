using Hangfire;
using MarketingOps.Application.Crawls.Commands.RenderCrawl;
using MediatR;

namespace MarketingOps.Infrastructure.Jobs;

/// <summary>
/// Biten taramanın render denetimi. Komut ölçülmüş sayfaları atlar; bu yüzden yeniden denenebilir. Aynı tarama iki kez
/// ölçülmesin (sayfa × cihaz benzersiz) diye eşzamanlı işler sıraya girer.
/// </summary>
public sealed class RenderCrawlJob(ISender sender)
{
    [AutomaticRetry(Attempts = 1)]
    [DisableConcurrentExecution("crawl-render:{0}", 1800)]
    [Queue("crawls")]
    public Task RunAsync(Guid crawlId, CancellationToken cancellationToken) => sender.Send(new RenderCrawlCommand(crawlId), cancellationToken);
}
