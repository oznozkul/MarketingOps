namespace MarketingOps.Application.Connectors.Queries.GetGoogleProperties;

/// <summary>
/// <see cref="LikelyMatch"/>: mülkün ya da hesabın adı site adını veya domain kökünü içeriyor (öneri; doğrulama değil).
/// <see cref="IsDemo"/>: Google'ın herkese açık demo hesabı; gerçek site verisi değildir.
/// </summary>
public sealed record Ga4PropertyResponse(string Id, string DisplayName, string AccountName, bool LikelyMatch, bool IsDemo);
