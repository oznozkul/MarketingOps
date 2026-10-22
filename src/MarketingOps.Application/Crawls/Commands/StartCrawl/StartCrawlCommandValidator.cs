using FluentValidation;

namespace MarketingOps.Application.Crawls.Commands.StartCrawl;

public sealed class StartCrawlCommandValidator : AbstractValidator<StartCrawlCommand>
{
    public StartCrawlCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.TriggeredBy).IsInEnum().WithMessage("Geçersiz tetikleyici.");
    }
}
