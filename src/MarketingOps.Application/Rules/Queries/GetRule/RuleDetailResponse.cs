using MarketingOps.Application.Rules.Queries.GetRules;
using MarketingOps.Domain.Findings;

namespace MarketingOps.Application.Rules.Queries.GetRule;

public sealed record RuleDetailResponse(
    RuleListItemResponse Rule,
    string Impact,
    string ReferenceKey,
    Severity? SeverityOverride,
    IReadOnlyList<RuleThresholdResponse> Thresholds,
    IReadOnlyList<RulePageTypeResponse> PageTypes,
    IReadOnlyList<RuleFindingResponse> Findings,
    IReadOnlyList<MutedPageTypeResponse> Muted);
