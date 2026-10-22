using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Crawls;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Crawls.Queries.GetCrawlStatus;

public sealed class GetCrawlStatusQueryHandler(IAppDbContext db) : IRequestHandler<GetCrawlStatusQuery, CrawlStatusResponse>
{
    public async Task<CrawlStatusResponse> Handle(GetCrawlStatusQuery request, CancellationToken cancellationToken)
    {
        var crawls = db.Crawls.AsNoTracking().Where(c => c.SiteId == request.SiteId);
        var active = await Project(crawls.Where(c => c.Status == CrawlStatus.Queued || c.Status == CrawlStatus.Running))
            .FirstOrDefaultAsync(cancellationToken);
        var lastFinished = await Project(crawls.Where(c => c.Status == CrawlStatus.Completed || c.Status == CrawlStatus.PartiallyCompleted))
            .FirstOrDefaultAsync(cancellationToken);
        return new CrawlStatusResponse(active, lastFinished);
    }

    private static IQueryable<CrawlListItemResponse> Project(IQueryable<Crawl> crawls) => crawls
        .OrderByDescending(c => c.Number)
        .Select(c => new CrawlListItemResponse(c.Id, c.Number, c.Status, c.TriggeredBy, c.QueuedAt, c.StartedAt, c.FinishedAt,
            c.PagesDiscovered, c.PagesCrawled, c.PagesFailed, c.EvaluatedAt, c.NewFindings, c.ResolvedFindings, c.ReopenedFindings));
}
