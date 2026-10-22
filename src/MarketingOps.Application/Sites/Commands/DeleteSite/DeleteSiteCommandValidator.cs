using FluentValidation;

namespace MarketingOps.Application.Sites.Commands.DeleteSite;

public sealed class DeleteSiteCommandValidator : AbstractValidator<DeleteSiteCommand>
{
    public DeleteSiteCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
    }
}
