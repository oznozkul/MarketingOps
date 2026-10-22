using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Scores;
using MarketingOps.Domain.Scores;
using MediatR;

namespace MarketingOps.Application.Tracking.Queries.GetTrackingScore;

public sealed class GetTrackingScoreQueryHandler(ScoreReader scores) : IRequestHandler<GetTrackingScoreQuery, TrackingScoreResponse>
{
    public async Task<TrackingScoreResponse> Handle(GetTrackingScoreQuery request, CancellationToken cancellationToken)
    {
        var facts = await scores.LoadAsync(request.SiteId, cancellationToken);
        var inputs = ScoreReader.Inputs(facts, ScoreCategory.Tracking);
        return new TrackingScoreResponse(ScoreCalculator.Score(inputs), inputs.Count);
    }
}
