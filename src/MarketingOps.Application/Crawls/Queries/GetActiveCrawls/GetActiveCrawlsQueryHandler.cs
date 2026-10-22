using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Crawls;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Crawls.Queries.GetActiveCrawls;

public sealed class GetActiveCrawlsQueryHandler(IAppDbContext db) : IRequestHandler<GetActiveCrawlsQuery, IReadOnlyList<ActiveCrawlResponse>>
{
    public async Task<IReadOnlyList<ActiveCrawlResponse>> Handle(GetActiveCrawlsQuery request, CancellationToken cancellationToken)
    {
        var rows = await db.Crawls.AsNoTracking()
            .Where(c => c.Status == CrawlStatus.Queued || c.Status == CrawlStatus.Running)
            .Select(c => new { c.SiteId, c.Id, c.Number, c.Status, c.Stages, c.StartedAt, c.PagesDiscovered, c.PagesCrawled, c.PagesFailed })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new ActiveCrawlResponse(r.SiteId, r.Id, r.Number, r.Status,
                r.Stages.Count == 0 ? null : r.Stages[^1].Stage, r.StartedAt, r.PagesDiscovered, r.PagesCrawled, r.PagesFailed))
            .ToList();
    }
}
