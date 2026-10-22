using FluentValidation;

namespace MarketingOps.Application.Findings.Commands.MarkFindingFalsePositive;

public sealed class MarkFindingFalsePositiveCommandValidator : AbstractValidator<MarkFindingFalsePositiveCommand>
{
    public MarkFindingFalsePositiveCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.FindingId).NotEmpty().WithMessage("Bulgu kimliği zorunludur.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Yanlış alarm için gerekçe zorunludur.")
            .MaximumLength(Domain.Findings.Finding.ReasonMaxLength).WithMessage($"Gerekçe en fazla {Domain.Findings.Finding.ReasonMaxLength} karakter olabilir.");
    }
}
