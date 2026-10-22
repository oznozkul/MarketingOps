using FluentValidation;

namespace MarketingOps.Application.PageTypes.Commands.AddPageType;

public sealed class AddPageTypeCommandValidator : AbstractValidator<AddPageTypeCommand>
{
    public AddPageTypeCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        this.AddNameRules(x => x.Name);
        this.AddConditionRules(x => x.Mode, x => x.Conditions);
    }
}
