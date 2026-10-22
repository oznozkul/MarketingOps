namespace MarketingOps.Application.Scores;

public sealed record ScoreFacts(IReadOnlyDictionary<Guid, int> Pages, IReadOnlyList<ScoreFact> Findings);
