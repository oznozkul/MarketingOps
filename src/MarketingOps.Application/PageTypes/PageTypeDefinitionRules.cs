using System.Linq.Expressions;
using FluentValidation;
using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.PageTypes;

/// <summary>Ekleme, güncelleme ve önizleme sorgularının paylaştığı girdi kuralları.</summary>
public static class PageTypeDefinitionRules
{
    public static void AddConditionRules<T>(this AbstractValidator<T> validator,
        Expression<Func<T, MatchMode>> mode,
        Expression<Func<T, IEnumerable<MatchConditionInput>>> conditions)
    {
        validator.RuleFor(mode).IsInEnum().WithMessage("Geçersiz eşleşme modu.");
        validator.RuleFor(conditions).NotEmpty().WithMessage("En az bir eşleşme koşulu gerekli.")
            .Must(c => c is null || c.Count() <= PageType.MaxConditions).WithMessage($"En fazla {PageType.MaxConditions} koşul olabilir.");
        validator.RuleForEach(conditions).SetValidator(new MatchConditionInputValidator());
    }

    public static void AddNameRules<T>(this AbstractValidator<T> validator, Expression<Func<T, string>> name)
    {
        validator.RuleFor(name).NotEmpty().WithMessage("Sayfa tipi adı zorunludur.")
            .MaximumLength(PageType.NameMaxLength).WithMessage($"Sayfa tipi adı en fazla {PageType.NameMaxLength} karakter olabilir.");
    }
}
