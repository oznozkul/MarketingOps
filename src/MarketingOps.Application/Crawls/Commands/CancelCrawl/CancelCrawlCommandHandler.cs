using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Crawls.Commands.CancelCrawl;

/// <summary>Çalışan tarama, durumu periyodik kontrol ettiği için iptali bir sonraki adımında fark eder ve durur.</summary>
public sealed class CancelCrawlCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<CancelCrawlCommand>
{
    public async Task Handle(CancelCrawlCommand request, CancellationToken cancellationToken)
    {
        var crawl = await db.Crawls.FirstOrDefaultAsync(c => c.Id == request.CrawlId && c.SiteId == request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Tarama bulunamadı.");
        crawl.Cancel(clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
