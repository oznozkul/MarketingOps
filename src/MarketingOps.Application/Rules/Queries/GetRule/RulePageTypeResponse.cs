using MarketingOps.Domain.Findings;

namespace MarketingOps.Application.Rules.Queries.GetRule;

/// <summary><see cref="SeverityOverride"/> null ise sayfa tipi site geneli önemi kullanır.</summary>
public sealed record RulePageTypeResponse(Guid Id, string Name, Severity? SeverityOverride, bool IsMuted);
