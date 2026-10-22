using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Findings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Findings.Queries.GetFindingSummary;

public sealed class GetFindingSummaryQueryHandler(IAppDbContext db) : IRequestHandler<GetFindingSummaryQuery, FindingSummaryResponse>
{
    public async Task<FindingSummaryResponse> Handle(GetFindingSummaryQuery request, CancellationToken cancellationToken)
    {
        var latestCrawlId = await db.LatestEvaluatedCrawlIdAsync(request.SiteId, cancellationToken);
        var site = db.Findings.AsNoTracking().Where(f => f.SiteId == request.SiteId);
        var filtered = site.Filter(request.Filter, latestCrawlId);

        return new FindingSummaryResponse(
            await filtered.CountAsync(cancellationToken),
            await filtered.CountAsync(f => f.Severity == Severity.Critical, cancellationToken),
            latestCrawlId is { } id
                ? await filtered.CountAsync(f => f.LastOpenedCrawlId == id && (f.Status == FindingStatus.Open || f.Status == FindingStatus.Reopened), cancellationToken)
                : 0,
            await site.AnyAsync(cancellationToken),
            latestCrawlId is not null);
    }
}
