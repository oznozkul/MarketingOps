using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Findings.Queries.GetFindings;

public sealed class GetFindingsQueryHandler(IAppDbContext db) : IRequestHandler<GetFindingsQuery, PagedResponse<FindingListItemResponse>>
{
    public async Task<PagedResponse<FindingListItemResponse>> Handle(GetFindingsQuery request, CancellationToken cancellationToken)
    {
        var latestCrawlId = await db.LatestEvaluatedCrawlIdAsync(request.SiteId, cancellationToken);
        var findings = db.Findings.AsNoTracking().Where(f => f.SiteId == request.SiteId).Filter(request.Filter, latestCrawlId);

        var total = await findings.CountAsync(cancellationToken);
        var items = await findings
            .Sort(request.Sort, request.Descending)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListItems(db, request.SiteId, latestCrawlId)
            .ToListAsync(cancellationToken);

        return new PagedResponse<FindingListItemResponse>(items, request.Page, request.PageSize, total);
    }
}
