using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Findings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Findings.Queries.GetCrawlFindingChanges;

public sealed class GetCrawlFindingChangesQueryHandler(IAppDbContext db) : IRequestHandler<GetCrawlFindingChangesQuery, IReadOnlyList<FindingChangeResponse>>
{
    private static readonly FindingEventType[] Changes = [FindingEventType.Opened, FindingEventType.Resolved, FindingEventType.Reopened];

    public async Task<IReadOnlyList<FindingChangeResponse>> Handle(GetCrawlFindingChangesQuery request, CancellationToken cancellationToken)
    {
        // Geçmiş JSON içinde tutulur; bir sitenin bulgu sayısı küçük olduğundan filtre bellekte yapılır.
        var findings = await db.Findings.AsNoTracking()
            .Where(f => f.SiteId == request.SiteId)
            .Select(f => new { f.Id, f.Title, f.Severity, f.History })
            .ToListAsync(cancellationToken);

        return findings
            .SelectMany(f => f.History
                .Where(h => h.CrawlNumber == request.CrawlNumber && Changes.Contains(h.Type))
                .Select(h => new FindingChangeResponse(f.Id, f.Title, f.Severity, h.Type)))
            .OrderBy(c => c.Change).ThenBy(c => c.Severity)
            .ToList();
    }
}
