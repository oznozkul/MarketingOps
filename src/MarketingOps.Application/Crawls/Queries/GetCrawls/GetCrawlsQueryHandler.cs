using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Crawls.Queries.GetCrawls;

public sealed class GetCrawlsQueryHandler(IAppDbContext db) : IRequestHandler<GetCrawlsQuery, PagedResponse<CrawlListItemResponse>>
{
    public async Task<PagedResponse<CrawlListItemResponse>> Handle(GetCrawlsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Crawls.AsNoTracking().Where(c => c.SiteId == request.SiteId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.Number)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CrawlListItemResponse(c.Id, c.Number, c.Status, c.TriggeredBy, c.QueuedAt, c.StartedAt, c.FinishedAt,
                c.PagesDiscovered, c.PagesCrawled, c.PagesFailed, c.EvaluatedAt, c.NewFindings, c.ResolvedFindings, c.ReopenedFindings))
            .ToListAsync(cancellationToken);
        return new PagedResponse<CrawlListItemResponse>(items, request.Page, request.PageSize, total);
    }
}
