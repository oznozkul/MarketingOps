using FluentValidation;
using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.Sites.Commands.UpdateSite;

public sealed class UpdateSiteCommandValidator : AbstractValidator<UpdateSiteCommand>
{
    public UpdateSiteCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Site adı zorunludur.")
            .MaximumLength(Site.NameMaxLength).WithMessage($"Site adı en fazla {Site.NameMaxLength} karakter olabilir.");
    }
}
