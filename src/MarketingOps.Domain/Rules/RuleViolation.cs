namespace MarketingOps.Domain.Rules;

/// <summary>Bir kuralın bir sayfada (ya da statik dosyada) bulduğu ihlal.</summary>
/// <summary><see cref="Devices"/>: render kurallarında sorunun görüldüğü cihazlar; diğer kurallarda boştur.</summary>
public sealed record RuleViolation(string RuleCode, Guid? PageTypeId, string Url, string? CurrentValue, string? Detail = null, string? Devices = null);
