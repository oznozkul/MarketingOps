using FluentValidation;

namespace MarketingOps.Application.PageTypes.Commands.UpdatePageType;

public sealed class UpdatePageTypeCommandValidator : AbstractValidator<UpdatePageTypeCommand>
{
    public UpdatePageTypeCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.PageTypeId).NotEmpty().WithMessage("Sayfa tipi kimliği zorunludur.");
        this.AddNameRules(x => x.Name);
        this.AddConditionRules(x => x.Mode, x => x.Conditions);
    }
}
