using FluentValidation;
using MarketingOps.Application.Common.Validation;

namespace MarketingOps.Application.Crawls.Queries.GetPages;

public sealed class GetPagesQueryValidator : AbstractValidator<GetPagesQuery>
{
    public GetPagesQueryValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.CrawlId).NotEmpty().WithMessage("Tarama kimliği zorunludur.");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Geçersiz durum filtresi.");
        RuleFor(x => x.Search).MaximumLength(200).WithMessage("Arama metni en fazla 200 karakter olabilir.");
        this.AddPagingRules(x => x.Page, x => x.PageSize);
    }
}
