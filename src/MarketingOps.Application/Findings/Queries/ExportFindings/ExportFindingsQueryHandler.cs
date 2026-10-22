using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Findings.Queries.ExportFindings;

public sealed class ExportFindingsQueryHandler(IAppDbContext db) : IRequestHandler<ExportFindingsQuery, IReadOnlyList<FindingListItemResponse>>
{
    private const int MaxRows = 10_000;

    public async Task<IReadOnlyList<FindingListItemResponse>> Handle(ExportFindingsQuery request, CancellationToken cancellationToken)
    {
        var latestCrawlId = await db.LatestEvaluatedCrawlIdAsync(request.SiteId, cancellationToken);
        return await db.Findings.AsNoTracking()
            .Where(f => f.SiteId == request.SiteId)
            .Filter(request.Filter, latestCrawlId)
            .Sort(request.Sort, request.Descending)
            .Take(MaxRows)
            .ToListItems(db, request.SiteId, latestCrawlId)
            .ToListAsync(cancellationToken);
    }
}
