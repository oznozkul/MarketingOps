using FluentValidation;
using MarketingOps.Application.Common.Validation;

namespace MarketingOps.Application.Crawls.Queries.GetCrawls;

public sealed class GetCrawlsQueryValidator : AbstractValidator<GetCrawlsQuery>
{
    public GetCrawlsQueryValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        this.AddPagingRules(x => x.Page, x => x.PageSize);
    }
}
