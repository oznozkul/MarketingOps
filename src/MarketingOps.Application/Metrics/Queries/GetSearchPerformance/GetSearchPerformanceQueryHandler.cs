using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Metrics.Queries.GetSearchPerformance;

public sealed class GetSearchPerformanceQueryHandler(IAppDbContext db) : IRequestHandler<GetSearchPerformanceQuery, SearchPerformanceResponse?>
{
    private const int WindowDays = 28;

    public async Task<SearchPerformanceResponse?> Handle(GetSearchPerformanceQuery request, CancellationToken cancellationToken)
    {
        var to = await db.SearchPerformance.Where(r => r.SiteId == request.SiteId).MaxAsync(r => (DateOnly?)r.Date, cancellationToken);
        if (to is null) return null;
        var from = to.Value.AddDays(-(WindowDays - 1));

        var rows = db.SearchPerformance.AsNoTracking().Where(r => r.SiteId == request.SiteId && r.Date >= from && r.Date <= to);
        if (request.FindingId is { } findingId)
        {
            var urls = (await db.Findings.AsNoTracking().Where(f => f.Id == findingId && f.SiteId == request.SiteId).Select(f => f.Occurrences).FirstOrDefaultAsync(cancellationToken) ?? [])
                .Select(o => o.Url).ToList();
            // Search Console URL'leri sondaki "/" ile ya da onsuz gelebilir.
            var variants = urls.SelectMany(u => new[] { u, u.TrimEnd('/'), u.TrimEnd('/') + "/" }).Distinct().ToList();
            rows = rows.Where(r => variants.Contains(r.Url));
        }

        var total = await rows.GroupBy(_ => 1).Select(g => new
        {
            Pages = g.Select(r => r.Url).Distinct().Count(),
            Clicks = g.Sum(r => r.Clicks),
            Impressions = g.Sum(r => r.Impressions),
            Weighted = g.Sum(r => r.Position * r.Impressions)
        }).FirstOrDefaultAsync(cancellationToken);
        if (total is null) return new SearchPerformanceResponse(from, to.Value, 0, 0, 0, 0, null);

        return new SearchPerformanceResponse(from, to.Value, total.Pages, total.Clicks, total.Impressions,
            total.Impressions == 0 ? 0 : (double)total.Clicks / total.Impressions,
            total.Impressions == 0 ? null : total.Weighted / total.Impressions);
    }
}
