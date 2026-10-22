using FluentValidation;

namespace MarketingOps.Application.Rules.Queries.GetRule;

public sealed class GetRuleQueryValidator : AbstractValidator<GetRuleQuery>
{
    public GetRuleQueryValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.RuleCode).NotEmpty().WithMessage("Kural kodu zorunludur.");
    }
}
