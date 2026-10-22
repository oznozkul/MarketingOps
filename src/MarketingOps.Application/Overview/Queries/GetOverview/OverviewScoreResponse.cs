using MarketingOps.Domain.Scores;

namespace MarketingOps.Application.Overview.Queries.GetOverview;

public sealed record OverviewScoreResponse(ScoreCategory Category, int Score, int? Previous);
