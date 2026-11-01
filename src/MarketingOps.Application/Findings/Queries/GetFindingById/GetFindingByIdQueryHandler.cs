using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Common.Models;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.WorkTasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Findings.Queries.GetFindingById;

public sealed class GetFindingByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetFindingByIdQuery, FindingDetailResponse>
{
    public async Task<FindingDetailResponse> Handle(GetFindingByIdQuery request, CancellationToken cancellationToken)
    {
        var latestCrawlId = await db.LatestEvaluatedCrawlIdAsync(request.SiteId, cancellationToken);
        var findings = db.Findings.AsNoTracking().Where(f => f.Id == request.FindingId && f.SiteId == request.SiteId);
        var item = await findings.ToListItems(db, request.SiteId, latestCrawlId).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Bulgu bulunamadı.");
        var finding = await findings.FirstAsync(cancellationToken);

        var rule = RuleCatalog.Find(finding.RuleCode);
        var occurrences = finding.Occurrences.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(request.OccurrenceSearch))
        {
            var term = request.OccurrenceSearch.Trim();
            occurrences = occurrences.Where(o => o.Url.Contains(term, StringComparison.OrdinalIgnoreCase)
                                                 || (o.CurrentValue?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        var filtered = occurrences.ToList();
        var pageItems = filtered
            .Skip((request.OccurrencePage - 1) * request.OccurrencePageSize)
            .Take(request.OccurrencePageSize)
            .ToList();

        // Render bulgularında her örnek URL için kanıt ekran görüntüleri; bulgunun gözlemlendiği taramaya ait olanlar.
        var screenshotsByUrl = new Dictionary<string, IReadOnlyList<FindingScreenshotResponse>>(StringComparer.Ordinal);
        if (rule?.DataSource == RuleDataSource.Render && pageItems.Count > 0)
        {
            // Örnekler farklı taramalarda görülmüş olabilir; her biri kendi son taramasının görüntüsüyle eşlenir.
            var crawlIds = pageItems.Select(o => o.LastSeenCrawlId).Distinct().ToList();
            var urls = pageItems.Select(o => o.Url).ToList();
            var lastSeen = pageItems.Select(o => (o.Url, o.LastSeenCrawlId)).ToHashSet();
            var shots = await db.RenderScreenshots.AsNoTracking()
                .Where(s => crawlIds.Contains(s.CrawlId) && urls.Contains(s.Url))
                .OrderBy(s => s.Device)
                .Select(s => new { s.Url, s.CrawlId, s.Device, s.Id })
                .ToListAsync(cancellationToken);
            screenshotsByUrl = shots.Where(s => lastSeen.Contains((s.Url, s.CrawlId)))
                .GroupBy(s => s.Url)
                .ToDictionary(g => g.Key,
                    g => (IReadOnlyList<FindingScreenshotResponse>)g.Select(s => new FindingScreenshotResponse(s.Id, s.Device)).ToList(),
                    StringComparer.Ordinal);
        }

        var page = pageItems
            .Select(o => new FindingOccurrenceResponse(o.Url, o.CurrentValue, o.Detail, o.Devices,
                screenshotsByUrl.TryGetValue(o.Url, out var shots) ? shots : []))
            .ToList();

        return new FindingDetailResponse(
            item,
            rule?.Name ?? finding.RuleCode,
            rule?.Description ?? "",
            TaskTemplate.Problem(item.AffectedCount, item.PageTypeName, finding.Occurrences.FirstOrDefault()),
            rule?.Impact ?? "",
            rule?.ReferenceKey ?? "",
            finding.Fingerprint,
            finding.ResolvedAt,
            finding.SnoozedUntil,
            finding.FalsePositiveReason,
            finding.History.OrderByDescending(h => h.At).ToList(),
            new PagedResponse<FindingOccurrenceResponse>(page, request.OccurrencePage, request.OccurrencePageSize, filtered.Count),
            finding.Occurrences.Count);
    }
}
