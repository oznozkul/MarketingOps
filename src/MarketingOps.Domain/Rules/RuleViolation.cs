namespace MarketingOps.Domain.Rules;

/// <summary>Bir kuralın bir sayfada (ya da statik dosyada) bulduğu ihlal.</summary>
public sealed record RuleViolation(string RuleCode, Guid? PageTypeId, string Url, string? CurrentValue, string? Detail = null);
