using MarketingOps.Application.Rules.Queries.PreviewRuleThresholdImpact;

namespace MarketingOps.Web.ViewModels.Rules;

/// <summary><see cref="Error"/> doluysa girilen eşik geçersizdir ve önizleme yapılmadı.</summary>
public sealed record ThresholdImpactViewModel(ThresholdImpactResponse? Impact, bool Changed, string? Error);
