using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Crawls;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Connectors.Commands.TriggerConnectorSync;

public sealed class TriggerConnectorSyncCommandHandler(IAppDbContext db, ICrawlScheduler scheduler) : IRequestHandler<TriggerConnectorSyncCommand>
{
    public async Task Handle(TriggerConnectorSyncCommand request, CancellationToken cancellationToken)
    {
        if (!await db.Sites.AnyAsync(s => s.Id == request.SiteId, cancellationToken)) throw new NotFoundException("Site bulunamadı.");
        if (request.IncludeWebVitals)
        {
            // Örnek sayfalar son taramadan seçilir.
            var hasCrawl = await db.Crawls.AnyAsync(c => c.SiteId == request.SiteId
                && (c.Status == CrawlStatus.Completed || c.Status == CrawlStatus.PartiallyCompleted), cancellationToken);
            if (!hasCrawl) throw new ConflictException("Web Vitals ölçümü için önce sitenin taranması gerekir.");
            scheduler.EnqueueWebVitals(request.SiteId);
            return;
        }
        scheduler.EnqueueConnectorSync(request.SiteId);
    }
}
