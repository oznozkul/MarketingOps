using MarketingOps.Domain.Scores;
using MediatR;

namespace MarketingOps.Application.Scores.Queries.GetScoreBreakdown;

public sealed record GetScoreBreakdownQuery(Guid SiteId, ScoreCategory Category) : IRequest<ScoreBreakdownResponse>;
