using FluentValidation;

namespace MarketingOps.Application.PageTypes.Queries.GetPageTypeStats;

public sealed class GetPageTypeStatsQueryValidator : AbstractValidator<GetPageTypeStatsQuery>
{
    public GetPageTypeStatsQueryValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
    }
}
