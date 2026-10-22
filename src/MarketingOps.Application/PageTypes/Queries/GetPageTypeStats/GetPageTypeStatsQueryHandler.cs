using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Rules;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.PageTypes.Queries.GetPageTypeStats;

public sealed class GetPageTypeStatsQueryHandler(IAppDbContext db) : IRequestHandler<GetPageTypeStatsQuery, PageTypeStatsResponse>
{
    public async Task<PageTypeStatsResponse> Handle(GetPageTypeStatsQuery request, CancellationToken cancellationToken)
    {
        var settings = await db.LoadAsync(request.SiteId, cancellationToken);
        var enabled = RuleCatalog.All.Where(settings.IsEnabled).Select(r => r.Code).ToList();
        var muted = await db.Findings.AsNoTracking()
            .Where(f => f.SiteId == request.SiteId && f.Status == FindingStatus.FalsePositive && f.PageTypeId != null && enabled.Contains(f.RuleCode))
            .GroupBy(f => f.PageTypeId!.Value)
            .Select(g => new { PageTypeId = g.Key, Count = g.Select(f => f.RuleCode).Distinct().Count() })
            .ToDictionaryAsync(g => g.PageTypeId, g => g.Count, cancellationToken);

        var latest = await db.Crawls.AsNoTracking()
            .Where(c => c.SiteId == request.SiteId && (c.Status == CrawlStatus.Completed || c.Status == CrawlStatus.PartiallyCompleted))
            .OrderByDescending(c => c.Number)
            .Select(c => new { c.Id, c.Number })
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is null) return new PageTypeStatsResponse(null, null, new Dictionary<Guid, int>(), 0, enabled.Count, muted);

        var counts = await db.PageSnapshots.AsNoTracking()
            .Where(p => p.CrawlId == latest.Id)
            .GroupBy(p => p.PageTypeId)
            .Select(g => new { PageTypeId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new PageTypeStatsResponse(
            latest.Number,
            latest.Id,
            counts.Where(c => c.PageTypeId != null).ToDictionary(c => c.PageTypeId!.Value, c => c.Count),
            counts.FirstOrDefault(c => c.PageTypeId == null)?.Count ?? 0,
            enabled.Count,
            muted);
    }
}
