using FluentValidation;

namespace MarketingOps.Application.Crawls.Queries.GetCrawlById;

public sealed class GetCrawlByIdQueryValidator : AbstractValidator<GetCrawlByIdQuery>
{
    public GetCrawlByIdQueryValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.CrawlId).NotEmpty().WithMessage("Tarama kimliği zorunludur.");
    }
}
