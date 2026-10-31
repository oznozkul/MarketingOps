using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Rendering;
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
                c.SkippedUrls,
                c.SnapshotsPrunedAt
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Tarama bulunamadı.");

        var snapshots = db.PageSnapshots.AsNoTracking().Where(p => p.CrawlId == request.CrawlId);
        var failed = snapshots.Where(p => p.StatusCode == null || p.StatusCode >= 400);

        var counts = await snapshots.GroupBy(p => p.PageTypeId).Select(g => new { PageTypeId = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        var names = await db.Sites.AsNoTracking().Where(s => s.Id == request.SiteId)
            .SelectMany(s => s.PageTypes).Select(p => new { p.Id, p.Name }).ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var renders = db.RenderSamples.AsNoTracking().Where(r => r.CrawlId == request.CrawlId);
        var renderCounts = await renders.GroupBy(r => r.Status).Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(r => r.Status, r => r.Count, cancellationToken);
        var renderFailures = await renders.Where(r => r.Status == RenderStatus.Failed).OrderBy(r => r.Url).Take(ListLimit)
            .Select(r => new FailedRenderResponse(r.Url, r.Device, r.Error)).ToListAsync(cancellationToken);

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
            crawl.SkippedUrls.Take(ListLimit).Select(s => new SkippedUrlResponse(s.Url, s.Reason)).ToList(),
            crawl.SnapshotsPrunedAt,
            new RenderSummaryResponse(renderCounts.GetValueOrDefault(RenderStatus.Measured), renderCounts.GetValueOrDefault(RenderStatus.Obstructed),
                renderCounts.GetValueOrDefault(RenderStatus.Failed), renderFailures));
    }
}
