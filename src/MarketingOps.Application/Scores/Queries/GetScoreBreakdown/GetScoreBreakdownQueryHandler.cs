using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Sites;
using MarketingOps.Domain.Scores;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Scores.Queries.GetScoreBreakdown;

public sealed class GetScoreBreakdownQueryHandler(IAppDbContext db, ScoreReader scores, TimeProvider clock) : IRequestHandler<GetScoreBreakdownQuery, ScoreBreakdownResponse>
{
    public async Task<ScoreBreakdownResponse> Handle(GetScoreBreakdownQuery request, CancellationToken cancellationToken)
    {
        var facts = await scores.LoadAsync(request.SiteId, cancellationToken);
        var inputs = ScoreReader.Inputs(facts, request.Category);
        var site = await SiteLoader.LoadAsync(db, request.SiteId, cancellationToken);
        var names = site.PageTypes.ToDictionary(p => p.Id, p => p.Name);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var previous = await db.ScoreSnapshots.AsNoTracking()
            .Where(s => s.SiteId == request.SiteId && s.Category == request.Category && s.Date < today)
            .OrderByDescending(s => s.Date)
            .Select(s => (int?)s.Score)
            .FirstOrDefaultAsync(cancellationToken);

        var deductions = ScoreCalculator.Breakdown(inputs)
            .Select(d => new ScoreDeductionResponse(d.FindingId, d.Title, d.Severity,
                d.PageTypeId is Guid id && names.TryGetValue(id, out var name) ? name : null,
                d.AffectedCount, d.PageTypePageCount, d.Points))
            .ToList();
        return new ScoreBreakdownResponse(request.Category, ScoreCalculator.Score(inputs), previous, deductions);
    }
}
