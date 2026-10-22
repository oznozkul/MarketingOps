using Hangfire;
using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Crawls.Commands.StartCrawl;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Crawls;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Infrastructure.Jobs;

/// <summary>Sitenin tarama sıklığına göre çalışan tekrarlayan iş: taramayı kuyruğa ekler.</summary>
public sealed class ScheduledCrawlJob(ISender sender, ILogger<ScheduledCrawlJob> logger)
{
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(Guid siteId, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new StartCrawlCommand(siteId, CrawlTrigger.Schedule), cancellationToken);
        }
        catch (Exception ex) when (ex is DomainException or ConflictException or NotFoundException)
        {
            // Sahipliği doğrulanmamış site, zaten süren tarama ya da silinmiş site: beklenen durumlar, iş başarısız sayılmaz.
            logger.LogInformation("Zamanlanmış tarama atlandı ({SiteId}): {Reason}", siteId, ex.Message);
        }
    }
}
