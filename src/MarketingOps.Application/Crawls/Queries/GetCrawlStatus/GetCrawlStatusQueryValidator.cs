using FluentValidation;

namespace MarketingOps.Application.Crawls.Queries.GetCrawlStatus;

public sealed class GetCrawlStatusQueryValidator : AbstractValidator<GetCrawlStatusQuery>
{
    public GetCrawlStatusQueryValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
    }
}
