using FluentValidation;
using MarketingOps.Domain.Findings;

namespace MarketingOps.Application.Findings.Commands.BulkUpdateFindings;

public sealed class BulkUpdateFindingsCommandValidator : AbstractValidator<BulkUpdateFindingsCommand>
{
    private const int MaxItems = 500;

    public BulkUpdateFindingsCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.FindingIds).NotEmpty().WithMessage("En az bir bulgu seçin.")
            .Must(ids => ids is null || ids.Count <= MaxItems).WithMessage($"Tek seferde en fazla {MaxItems} bulgu güncellenebilir.");
        RuleFor(x => x.Action).IsInEnum().WithMessage("Geçersiz işlem.");
        When(x => x.Action == BulkFindingAction.MarkFalsePositive, () =>
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Yanlış alarm için gerekçe zorunludur.")
                .MaximumLength(Finding.ReasonMaxLength).WithMessage($"Gerekçe en fazla {Finding.ReasonMaxLength} karakter olabilir."));
        When(x => x.Action == BulkFindingAction.Snooze, () =>
            RuleFor(x => x.Until).NotNull().WithMessage("Erteleme tarihi zorunludur."));
    }
}
