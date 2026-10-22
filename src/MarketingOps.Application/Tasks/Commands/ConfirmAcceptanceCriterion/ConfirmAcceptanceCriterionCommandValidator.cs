using FluentValidation;

namespace MarketingOps.Application.Tasks.Commands.ConfirmAcceptanceCriterion;

public sealed class ConfirmAcceptanceCriterionCommandValidator : AbstractValidator<ConfirmAcceptanceCriterionCommand>
{
    public ConfirmAcceptanceCriterionCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Görev kimliği zorunludur.");
        RuleFor(x => x.CriterionId).NotEmpty().WithMessage("Kriter kimliği zorunludur.");
    }
}
