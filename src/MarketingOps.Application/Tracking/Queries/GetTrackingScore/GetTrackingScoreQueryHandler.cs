using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Findings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tracking.Queries.GetTrackingScore;

public sealed class GetTrackingScoreQueryHandler(IAppDbContext db) : IRequestHandler<GetTrackingScoreQuery, TrackingScoreResponse>
{
    public async Task<TrackingScoreResponse> Handle(GetTrackingScoreQuery request, CancellationToken cancellationToken)
    {
        var severities = await db.Findings.AsNoTracking()
            .Where(f => f.SiteId == request.SiteId && f.Category == FindingCategory.Tracking && (f.Status == FindingStatus.Open || f.Status == FindingStatus.Reopened))
            .Select(f => f.Severity)
            .ToListAsync(cancellationToken);
        var penalty = severities.Sum(Weight);
        return new TrackingScoreResponse(Math.Max(0, 100 - penalty), severities.Count);
    }

    /// <summary>Faz 7 ağırlıklarının önizlemesi. Her açık bulgu kendi ağırlığını bir kez düşer.</summary>
    private static int Weight(Severity severity) => severity switch
    {
        Severity.Critical => 15,
        Severity.High => 8,
        Severity.Medium => 3,
        _ => 1
    };
}
