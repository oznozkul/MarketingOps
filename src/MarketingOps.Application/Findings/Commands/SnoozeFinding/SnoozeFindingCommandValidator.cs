using FluentValidation;

namespace MarketingOps.Application.Findings.Commands.SnoozeFinding;

public sealed class SnoozeFindingCommandValidator : AbstractValidator<SnoozeFindingCommand>
{
    public SnoozeFindingCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.FindingId).NotEmpty().WithMessage("Bulgu kimliği zorunludur.");

    }
}
