using FluentValidation;
using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.Sites.Commands.CreateSite;

public sealed class CreateSiteCommandValidator : AbstractValidator<CreateSiteCommand>
{
    public CreateSiteCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Site adı zorunludur.")
            .MaximumLength(Site.NameMaxLength).WithMessage($"Site adı en fazla {Site.NameMaxLength} karakter olabilir.");
        RuleFor(x => x.Domain).NotEmpty().WithMessage("Domain zorunludur.")
            .MaximumLength(Site.DomainMaxLength).WithMessage($"Domain en fazla {Site.DomainMaxLength} karakter olabilir.");
    }
}
