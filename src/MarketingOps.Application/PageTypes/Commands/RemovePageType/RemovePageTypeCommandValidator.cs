using FluentValidation;

namespace MarketingOps.Application.PageTypes.Commands.RemovePageType;

public sealed class RemovePageTypeCommandValidator : AbstractValidator<RemovePageTypeCommand>
{
    public RemovePageTypeCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.PageTypeId).NotEmpty().WithMessage("Sayfa tipi kimliği zorunludur.");
    }
}
