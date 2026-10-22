using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Findings.Commands.RequestCrawlEvaluation;

public sealed class RequestCrawlEvaluationCommandHandler(IAppDbContext db, ICrawlScheduler scheduler) : IRequestHandler<RequestCrawlEvaluationCommand>
{
    public async Task Handle(RequestCrawlEvaluationCommand request, CancellationToken cancellationToken)
    {
        var crawl = await db.Crawls.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CrawlId && c.SiteId == request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Tarama bulunamadı.");
        if (!crawl.CanBeEvaluated) throw new ConflictException("Bu tarama değerlendirilemez ya da zaten değerlendirildi.");

        // Bulguların doğru sırayla güncellenmesi için yalnızca sitenin en son bitmiş taraması değerlendirilebilir.
        var newerEvaluated = await db.Crawls.AnyAsync(c => c.SiteId == request.SiteId && c.Number > crawl.Number && c.EvaluatedAt != null, cancellationToken);
        if (newerEvaluated) throw new ConflictException("Daha yeni bir tarama zaten değerlendirildi; eski tarama bulguları geri alamaz.");

        scheduler.EnqueueEvaluation(crawl.Id);
    }
}
