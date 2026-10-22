using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Crawls.Queries.GetCrawlById;

public sealed class GetCrawlByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetCrawlByIdQuery, CrawlDetailResponse>
{
    private const int ListLimit = 100;

    public async Task<CrawlDetailResponse> Handle(GetCrawlByIdQuery request, CancellationToken cancellationToken)
    {
        var crawl = await db.Crawls.AsNoTracking()
            .Where(c => c.Id == request.CrawlId && c.SiteId == request.SiteId)
            .Select(c => new
            {
                Item = new CrawlListItemResponse(c.Id, c.Number, c.Status, c.TriggeredBy, c.QueuedAt, c.StartedAt, c.FinishedAt,
                    c.PagesDiscovered, c.PagesCrawled, c.PagesFailed, c.EvaluatedAt, c.NewFindings, c.ResolvedFindings, c.ReopenedFindings),
                c.ErrorMessage,
                c.Stages,
                c.SkippedCount,
                c.SkippedUrls
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Tarama bulunamadı.");

        var snapshots = db.PageSnapshots.AsNoTracking().Where(p => p.CrawlId == request.CrawlId);
        var failed = snapshots.Where(p => p.StatusCode == null || p.StatusCode >= 400);

        var counts = await snapshots.GroupBy(p => p.PageTypeId).Select(g => new { PageTypeId = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        var names = await db.Sites.AsNoTracking().Where(s => s.Id == request.SiteId)
            .SelectMany(s => s.PageTypes).Select(p => new { p.Id, p.Name }).ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        return new CrawlDetailResponse(
            crawl.Item,
            crawl.ErrorMessage,
            crawl.Stages.Select(s => new CrawlStageResponse(s.Stage, s.StartedAt, s.FinishedAt)).ToList(),
            counts
                .Select(c => new PageTypeCountResponse(c.PageTypeId, c.PageTypeId is { } id ? names.GetValueOrDefault(id) : null, c.Count))
                .OrderByDescending(c => c.Count)
                .ToList(),
            await failed.CountAsync(cancellationToken),
            await failed.OrderBy(p => p.FetchedAt).Take(ListLimit)
                .Select(p => new FailedPageResponse(p.Url, p.StatusCode, p.FetchError)).ToListAsync(cancellationToken),
            crawl.SkippedCount,
            crawl.SkippedUrls.Take(ListLimit).Select(s => new SkippedUrlResponse(s.Url, s.Reason)).ToList());
    }
}
