using MarketingOps.Domain.Scores;

namespace MarketingOps.Application.Scores.Queries.GetScoreBreakdown;

public sealed record ScoreBreakdownResponse(ScoreCategory Category, int Score, int? Previous, IReadOnlyList<ScoreDeductionResponse> Deductions);
