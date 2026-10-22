using FluentValidation;
using MarketingOps.Application.Common.Validation;

namespace MarketingOps.Application.Sites.Queries.GetSites;

public sealed class GetSitesQueryValidator : AbstractValidator<GetSitesQuery>
{
    public GetSitesQueryValidator()
    {
        this.AddPagingRules(x => x.Page, x => x.PageSize);
    }
}
