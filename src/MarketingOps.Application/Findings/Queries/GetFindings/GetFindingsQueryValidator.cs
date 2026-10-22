using FluentValidation;
using MarketingOps.Application.Common.Validation;

namespace MarketingOps.Application.Findings.Queries.GetFindings;

public sealed class GetFindingsQueryValidator : AbstractValidator<GetFindingsQuery>
{
    public GetFindingsQueryValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.Sort).IsInEnum().WithMessage("Geçersiz sıralama.");
        RuleFor(x => x.Filter.Search).MaximumLength(200).WithMessage("Arama metni en fazla 200 karakter olabilir.");
        this.AddPagingRules(x => x.Page, x => x.PageSize);
    }
}
