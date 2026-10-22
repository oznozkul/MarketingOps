using FluentValidation;

namespace MarketingOps.Application.Crawls.Commands.CancelCrawl;

public sealed class CancelCrawlCommandValidator : AbstractValidator<CancelCrawlCommand>
{
    public CancelCrawlCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.CrawlId).NotEmpty().WithMessage("Tarama kimliği zorunludur.");
    }
}
