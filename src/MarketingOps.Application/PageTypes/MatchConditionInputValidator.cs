using FluentValidation;
using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.PageTypes;

public sealed class MatchConditionInputValidator : AbstractValidator<MatchConditionInput>
{
    public MatchConditionInputValidator()
    {
        RuleFor(x => x.Type).IsInEnum().WithMessage("Geçersiz koşul türü.");
        RuleFor(x => x.Value).NotEmpty().WithMessage("Koşul değeri zorunludur.")
            .MaximumLength(MatchCondition.ValueMaxLength).WithMessage($"Koşul değeri en fazla {MatchCondition.ValueMaxLength} karakter olabilir.");
    }
}
