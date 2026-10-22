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
        var page = filtered
            .Skip((request.OccurrencePage - 1) * request.OccurrencePageSize)
            .Take(request.OccurrencePageSize)
            .Select(o => new FindingOccurrenceResponse(o.Url, o.CurrentValue, o.Detail))
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
