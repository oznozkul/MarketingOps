using FluentValidation;

namespace MarketingOps.Application.Findings.Queries.GetFindingSummary;

public sealed class GetFindingSummaryQueryValidator : AbstractValidator<GetFindingSummaryQuery>
{
    public GetFindingSummaryQueryValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
    }
}
