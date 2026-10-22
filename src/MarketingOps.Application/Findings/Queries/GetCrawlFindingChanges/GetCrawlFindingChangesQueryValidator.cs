using FluentValidation;

namespace MarketingOps.Application.Findings.Queries.GetCrawlFindingChanges;

public sealed class GetCrawlFindingChangesQueryValidator : AbstractValidator<GetCrawlFindingChangesQuery>
{
    public GetCrawlFindingChangesQueryValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.CrawlNumber).GreaterThan(0).WithMessage("Tarama numarası geçersiz.");
    }
}
