using MarketingOps.Domain.Rules;

namespace MarketingOps.Application.Scores;

public sealed record ScoreFacts(
    IReadOnlyDictionary<Guid, int> Pages,
    int SitePages,
    IReadOnlyDictionary<(RuleDataSource Source, Guid PageTypeId), int> Samples,
    IReadOnlyList<ScoreFact> Findings);
