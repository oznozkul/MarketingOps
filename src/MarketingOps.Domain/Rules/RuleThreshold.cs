namespace MarketingOps.Domain.Rules;

/// <summary>Kuralın ayarlanabilir eşiği; site ayarı yoksa <see cref="Default"/> geçerlidir.</summary>
public sealed record RuleThreshold(string Key, string Label, string Unit, decimal Default, decimal Min, decimal Max);
