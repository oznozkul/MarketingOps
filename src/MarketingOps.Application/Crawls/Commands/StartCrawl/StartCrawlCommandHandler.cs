using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Crawls;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Crawls.Commands.StartCrawl;

public sealed class StartCrawlCommandHandler(IAppDbContext db, ICrawlScheduler scheduler, TimeProvider clock) : IRequestHandler<StartCrawlCommand, Guid>
{
    private const string AlreadyActive = "Bu site için zaten devam eden bir tarama var.";

    public async Task<Guid> Handle(StartCrawlCommand request, CancellationToken cancellationToken)
    {
        var site = await db.Sites.AsNoTracking()
            .Where(s => s.Id == request.SiteId)
            .Select(s => new { s.IsOwnershipVerified })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Site bulunamadı.");

        var crawls = db.Crawls.Where(c => c.SiteId == request.SiteId);
        if (await crawls.AnyAsync(c => c.Status == CrawlStatus.Queued || c.Status == CrawlStatus.Running, cancellationToken))
            throw new ConflictException(AlreadyActive);

        var number = (await crawls.MaxAsync(c => (int?)c.Number, cancellationToken) ?? 0) + 1;
        var crawl = Crawl.Queue(request.SiteId, site.IsOwnershipVerified, number, request.TriggeredBy, clock.GetUtcNow());
        db.Crawls.Add(crawl);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Aynı anda gelen iki istekten ikincisi: veritabanındaki "site başına tek aktif tarama" indeksi reddeder.
            throw new ConflictException(AlreadyActive);
        }

        scheduler.EnqueueCrawl(crawl.Id);
        return crawl.Id;
    }
}
