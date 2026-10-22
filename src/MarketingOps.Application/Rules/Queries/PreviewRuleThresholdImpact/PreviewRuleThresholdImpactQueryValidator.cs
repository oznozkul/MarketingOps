using FluentValidation;
using MarketingOps.Domain.Rules;

namespace MarketingOps.Application.Rules.Queries.PreviewRuleThresholdImpact;

public sealed class PreviewRuleThresholdImpactQueryValidator : AbstractValidator<PreviewRuleThresholdImpactQuery>
{
    public PreviewRuleThresholdImpactQueryValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.RuleCode).Must(code => RuleCatalog.Find(code) is not null).WithMessage("Kural bulunamadı.");
        RuleFor(x => x).Custom((query, context) =>
        {
            if (RuleCatalog.Find(query.RuleCode) is { } rule) context.ValidateThresholds(rule, query.Thresholds);
        });
    }
}
