using FluentValidation;

namespace MarketingOps.Application.Findings.Commands.RevertFalsePositive;

public sealed class RevertFalsePositiveCommandValidator : AbstractValidator<RevertFalsePositiveCommand>
{
    public RevertFalsePositiveCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.FindingId).NotEmpty().WithMessage("Bulgu kimliği zorunludur.");

    }
}
