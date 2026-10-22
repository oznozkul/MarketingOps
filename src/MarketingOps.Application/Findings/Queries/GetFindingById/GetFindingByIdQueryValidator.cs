using FluentValidation;
using MarketingOps.Application.Common.Validation;

namespace MarketingOps.Application.Findings.Queries.GetFindingById;

public sealed class GetFindingByIdQueryValidator : AbstractValidator<GetFindingByIdQuery>
{
    public GetFindingByIdQueryValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.FindingId).NotEmpty().WithMessage("Bulgu kimliği zorunludur.");
        this.AddPagingRules(x => x.OccurrencePage, x => x.OccurrencePageSize);
    }
}
