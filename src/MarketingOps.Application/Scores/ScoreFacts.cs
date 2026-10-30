namespace MarketingOps.Application.Scores;

public sealed record ScoreFacts(IReadOnlyDictionary<Guid, int> Pages, int SitePages, IReadOnlyList<ScoreFact> Findings);
