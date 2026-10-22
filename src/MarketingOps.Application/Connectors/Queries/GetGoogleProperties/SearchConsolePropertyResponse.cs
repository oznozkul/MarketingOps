namespace MarketingOps.Application.Connectors.Queries.GetGoogleProperties;

/// <summary><see cref="MatchesSite"/>: mülk bu sitenin domainine ait; doğrulanmışsa seçilince site sahipliği doğrulanır.</summary>
public sealed record SearchConsolePropertyResponse(string SiteUrl, string PermissionLevel, bool IsVerified, bool MatchesSite);
