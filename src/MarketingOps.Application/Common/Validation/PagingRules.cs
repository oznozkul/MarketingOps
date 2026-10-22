using FluentValidation;

namespace MarketingOps.Application.Common.Validation;

/// <summary>Sayfalı tüm sorgularda ortak sınırlar.</summary>
public static class PagingRules
{
    public const int MaxPageSize = 100;

    public static void AddPagingRules<T>(this AbstractValidator<T> validator, Func<T, int> page, Func<T, int> pageSize)
    {
        validator.RuleFor(x => page(x)).GreaterThanOrEqualTo(1).OverridePropertyName("Page")
            .WithMessage("Sayfa numarası 1 veya daha büyük olmalı.");
        validator.RuleFor(x => pageSize(x)).InclusiveBetween(1, MaxPageSize).OverridePropertyName("PageSize")
            .WithMessage($"Sayfa boyutu 1 ile {MaxPageSize} arasında olmalı.");
    }
}
