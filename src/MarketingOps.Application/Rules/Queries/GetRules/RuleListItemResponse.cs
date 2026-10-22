using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules;

namespace MarketingOps.Application.Rules.Queries.GetRules;

/// <summary><see cref="Severity"/> site geneli efektif önemdir; sayfa tipine özel önemler <see cref="PageTypeOverrideCount"/>'te sayılır.</summary>
public sealed record RuleListItemResponse(
    string Code,
    string Name,
    string Description,
    FindingCategory Category,
    RuleScope Scope,
    Severity DefaultSeverity,
    Severity Severity,
    bool IsEnabled,
    bool HasThresholds,
    int PageTypeOverrideCount,
    int ActiveFindingCount,
    int MutedPageTypeCount)
{
    public static RuleListItemResponse From(IRule rule, SiteRuleSetting? setting, int activeFindings, int mutedPageTypes) => new(
        rule.Code, rule.Name, rule.Description, rule.Category, rule.Scope, rule.DefaultSeverity,
        SiteRuleSetting.EffectiveSeverity(rule, setting, null),
        setting?.IsEnabled ?? true,
        rule.Thresholds.Count > 0,
        setting?.PageTypeSeverities.Count ?? 0,
        activeFindings,
        mutedPageTypes);
}
