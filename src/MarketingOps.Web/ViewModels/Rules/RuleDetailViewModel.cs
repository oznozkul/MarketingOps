using MarketingOps.Application.Rules.Queries.GetRule;

namespace MarketingOps.Web.ViewModels.Rules;

public sealed record RuleDetailViewModel(RuleDetailResponse Detail, RuleSettingsForm Form);
