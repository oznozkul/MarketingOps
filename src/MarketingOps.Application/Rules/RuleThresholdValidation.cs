using FluentValidation;
using MarketingOps.Domain.Rules;

namespace MarketingOps.Application.Rules;

/// <summary>Eşik değerlerinin kuralın tanımına göre doğrulanması; ayar kaydı ve etki önizlemesi aynı kuralları kullanır.</summary>
public static class RuleThresholdValidation
{
    public static void ValidateThresholds<T>(this ValidationContext<T> context, IRule rule, IReadOnlyDictionary<string, decimal>? thresholds)
    {
        foreach (var (key, value) in thresholds ?? new Dictionary<string, decimal>())
        {
            var definition = rule.Thresholds.FirstOrDefault(t => t.Key == key);
            if (definition is null)
                context.AddFailure($"Thresholds[{key}]", $"'{key}' bu kural için geçerli bir eşik değil.");
            else if (value < definition.Min || value > definition.Max)
                context.AddFailure($"Thresholds[{key}]", $"{definition.Label} {definition.Min:0.##}–{definition.Max:0.##} {definition.Unit} arasında olmalı.");
        }
    }
}
