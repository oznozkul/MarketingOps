using FluentValidation;

namespace MarketingOps.Application.Rules.Queries.GetRules;

public sealed class GetRulesQueryValidator : AbstractValidator<GetRulesQuery>
{
    public GetRulesQueryValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
    }
}
